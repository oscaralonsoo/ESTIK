using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ---------------------------------------------------------------------------
//  Ajustes guardados. Léelos desde cualquier script con GameSettings.Load()
// ---------------------------------------------------------------------------
[System.Serializable]
public struct GameSettings
{
    public int quality;
    public bool fullscreen;
    public bool vsync;
    public float master, music, sfx;

    public static GameSettings Default => new GameSettings
    {
        quality = Mathf.Min(2, QualitySettings.names.Length - 1),
        fullscreen = true,
        vsync = true,
        master = 1f,
        music = 0.8f,
        sfx = 1f
    };

    public static GameSettings Load()
    {
        var d = Default;
        return new GameSettings
        {
            quality = PlayerPrefs.GetInt("opt_quality", d.quality),
            fullscreen = PlayerPrefs.GetInt("opt_fullscreen", d.fullscreen ? 1 : 0) == 1,
            vsync = PlayerPrefs.GetInt("opt_vsync", d.vsync ? 1 : 0) == 1,
            master = PlayerPrefs.GetFloat("opt_master", d.master),
            music = PlayerPrefs.GetFloat("opt_music", d.music),
            sfx = PlayerPrefs.GetFloat("opt_sfx", d.sfx)
        };
    }

    public void Save()
    {
        PlayerPrefs.SetInt("opt_quality", quality);
        PlayerPrefs.SetInt("opt_fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("opt_vsync", vsync ? 1 : 0);
        PlayerPrefs.SetFloat("opt_master", master);
        PlayerPrefs.SetFloat("opt_music", music);
        PlayerPrefs.SetFloat("opt_sfx", sfx);
        PlayerPrefs.Save();
    }

    // Aplica los ajustes al juego (se usa también para previsualizar)
    public void ApplyToGame(AudioMixer mixer)
    {
        QualitySettings.SetQualityLevel(Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1), true);
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        if (!Application.isMobilePlatform) Screen.fullScreen = fullscreen;

        if (mixer != null)
        {
            SetDb(mixer, "MasterVolume", master);
            SetDb(mixer, "MusicVolume", music);
            SetDb(mixer, "SfxVolume", sfx);
        }
        else
        {
            AudioListener.volume = master;
        }
    }

    static void SetDb(AudioMixer mixer, string param, float v) =>
        mixer.SetFloat(param, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
}

// ---------------------------------------------------------------------------
//  Panel de opciones. Ponlo en un objeto SIEMPRE ACTIVO (no en el propio panel).
// ---------------------------------------------------------------------------
public class OptionsUI : MonoBehaviour
{
    [System.Serializable]
    class Tab
    {
        public string name;
        public Button button;
        public Image background;
        public CanvasGroup page;
    }

    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] AudioMixer mixer;

    [Header("Pestañas")]
    [SerializeField] Tab[] tabs;
    [SerializeField] Color tabOn = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] Color tabOff = new Color(0.98f, 0.93f, 0.7f, 1f);
    [SerializeField] float pageSlide = 40f;
    [SerializeField] float pageDuration = 0.2f;

    [Header("Flechas (solo móvil)")]
    [SerializeField] Button leftArrow;
    [SerializeField] Button rightArrow;
    [SerializeField] bool forceArrows;

    [Header("Botones")]
    [SerializeField] Button applyButton;
    [SerializeField] Button cancelButton;
    [SerializeField] Button resetButton;

    [Header("Graphics")]
    [SerializeField] TMP_Dropdown qualityDropdown;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Toggle vsyncToggle;
    [SerializeField] GameObject[] desktopOnlyRows;

    [Header("Sound")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    public event System.Action Closed;

    GameSettings saved, pending;
    int current;
    Vector2[] pageRest;
    Coroutine switching;

    bool Mobile => Application.isMobilePlatform || forceArrows;

    void Awake()
    {
        pageRest = new Vector2[tabs.Length];
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            pageRest[i] = ((RectTransform)tabs[i].page.transform).anchoredPosition;
            tabs[i].page.gameObject.SetActive(false);
            tabs[i].button.onClick.AddListener(() => SelectTab(idx));
        }

        if (leftArrow != null) { leftArrow.gameObject.SetActive(Mobile); leftArrow.onClick.AddListener(() => Step(-1)); }
        if (rightArrow != null) { rightArrow.gameObject.SetActive(Mobile); rightArrow.onClick.AddListener(() => Step(1)); }

        foreach (var row in desktopOnlyRows) if (row != null) row.SetActive(!Application.isMobilePlatform);

        applyButton.onClick.AddListener(Apply);
        cancelButton.onClick.AddListener(Cancel);
        resetButton.onClick.AddListener(ResetToDefault);

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.onValueChanged.AddListener(v => { pending.quality = v; Preview(); });
        fullscreenToggle.onValueChanged.AddListener(v => { pending.fullscreen = v; Preview(); });
        vsyncToggle.onValueChanged.AddListener(v => { pending.vsync = v; Preview(); });
        masterSlider.onValueChanged.AddListener(v => { pending.master = v; Preview(); });
        musicSlider.onValueChanged.AddListener(v => { pending.music = v; Preview(); });
        sfxSlider.onValueChanged.AddListener(v => { pending.sfx = v; Preview(); });
    }

    void Start() => ApplySaved();

    public void ApplySaved() => GameSettings.Load().ApplyToGame(mixer);

    public void Open()
    {
        saved = GameSettings.Load();
        pending = saved;
        RefreshUI();

        if (switching != null) StopCoroutine(switching);
        current = 0;
        SnapPages(0);

        panel.SetActive(true);
    }

    void Close()
    {
        var anim = panel.GetComponent<PanelAnimator>();
        if (anim != null) anim.Hide(() => Closed?.Invoke());
        else
        {
            panel.SetActive(false);
            Closed?.Invoke();
        }
    }
    void Apply()
    {
        pending.Save();
        pending.ApplyToGame(mixer);
        Close();
    }
    void Cancel()
    {
        saved.ApplyToGame(mixer); 
        Close();
    }

    void ResetToDefault()
    {
        pending = GameSettings.Default;
        RefreshUI();
        Preview();
    }
    void Preview() => pending.ApplyToGame(mixer);

    void RefreshUI()
    {
        qualityDropdown.SetValueWithoutNotify(pending.quality);
        fullscreenToggle.SetIsOnWithoutNotify(pending.fullscreen);
        vsyncToggle.SetIsOnWithoutNotify(pending.vsync);
        masterSlider.SetValueWithoutNotify(pending.master);
        musicSlider.SetValueWithoutNotify(pending.music);
        sfxSlider.SetValueWithoutNotify(pending.sfx);
    }

    void Step(int dir) => SelectTab((current + dir + tabs.Length) % tabs.Length);

    void SelectTab(int index)
    {
        if (index == current) return;

        int from = current;
        current = index;

        if (switching != null)
        {
            StopCoroutine(switching);
            SnapPages(from);
        }
        switching = StartCoroutine(SwitchRoutine(from, index));
    }

    IEnumerator SwitchRoutine(int from, int to)
    {
        float dir = to > from ? 1f : -1f;

        var a = tabs[from].page;
        var b = tabs[to].page;
        var ra = (RectTransform)a.transform;
        var rb = (RectTransform)b.transform;

        b.gameObject.SetActive(true);

        for (float t = 0f; t < pageDuration; t += Time.unscaledDeltaTime)
        {
            float k = t / pageDuration;
            float e = 1f - Mathf.Pow(1f - k, 3f);

            a.alpha = 1f - e;
            ra.anchoredPosition = pageRest[from] + Vector2.left * dir * pageSlide * e;

            b.alpha = e;
            rb.anchoredPosition = pageRest[to] + Vector2.right * dir * pageSlide * (1f - e);
            yield return null;
        }

        SnapPages(to);
        switching = null;
    }
    void SnapPages(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            var p = tabs[i].page;
            p.gameObject.SetActive(i == index);
            p.alpha = 1f;
            ((RectTransform)p.transform).anchoredPosition = pageRest[i];
        }
    }

    void Update()
    {
        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        for (int i = 0; i < tabs.Length; i++)
        {
            bool on = i == current;
            var t = tabs[i];
            t.button.transform.localScale = Vector3.Lerp(
                t.button.transform.localScale, Vector3.one * (on ? 1.06f : 1f), k);
            if (t.background != null)
                t.background.color = Color.Lerp(t.background.color, on ? tabOn : tabOff, k);
        }
        if (panel.activeSelf && Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame) Step(-1);
            if (Keyboard.current.eKey.wasPressedThisFrame) Step(1);
        }
    }
}