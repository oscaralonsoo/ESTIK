using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] TMP_Text scoreText;

    [Header("Paneles")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    [Header("Victoria")]
    [SerializeField] TMP_Text winScoreText;
    [SerializeField] Image[] winStars;    
    [SerializeField] Sprite starOn;
    [SerializeField] Sprite starOff;

    [Header("Destello de las estrellas")]
    [SerializeField] Color flashColor = new Color(1f, 0.92f, 0.55f, 1f);
    [SerializeField] float flashSize = 2.6f;     
    [SerializeField] int sparkCount = 8;         
    [SerializeField] float sparkDistance = 1.1f; 

    bool gameEnded, paused, leaving;
    Coroutine scorePunch;
    Image[] flashes;
    readonly List<Image> sparkPool = new List<Image>();
    static Sprite glowSprite, sparkSprite;

    void Start()
    {
        var allButtons = GetComponentsInChildren<Button>(true);
        foreach (var b in allButtons)
        {
            if (b.GetComponent<ButtonFeedback>() == null)
                b.gameObject.AddComponent<ButtonFeedback>();
        }

        pausePanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!gameEnded && !leaving && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (paused) Resume();
            else Pause();
        }
    }

    void Open(GameObject panel) => panel.SetActive(true);

    void Close(GameObject panel, System.Action onDone = null)
    {
        var anim = panel.GetComponent<PanelAnimator>();
        if (anim != null && panel.activeSelf) anim.Hide(onDone);
        else
        {
            panel.SetActive(false);
            onDone?.Invoke();
        }
    }

    void Leave(System.Action action)
    {
        if (leaving) return;
        leaving = true;

        GameObject open = winPanel.activeSelf ? winPanel
                        : losePanel.activeSelf ? losePanel
                        : pausePanel.activeSelf ? pausePanel
                        : null;

        void Go()
        {
            Time.timeScale = 1f;
            action();
        }

        if (open == null) Go();
        else Close(open, Go);
    }

    public void SetScore(int score)
    {
        string text = score.ToString();
        if (scoreText.text == text) return;
        scoreText.text = text;

        if (scorePunch != null) StopCoroutine(scorePunch);
        scorePunch = StartCoroutine(Punch(scoreText.transform, 1.3f));
    }

    public void Pause()
    {
        if (gameEnded || paused) return;
        paused = true;
        Open(pausePanel);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        Close(pausePanel);
        Time.timeScale = 1f;
    }

    public void ShowWin(int score, int stars)
    {
        if (gameEnded) return;
        gameEnded = true;
        SaveLevelData.SetStars(LevelLoader.CurrentLevel, stars);

        for (int i = 0; i < winStars.Length; i++)
        {
            winStars[i].sprite = starOff;
            winStars[i].transform.localScale = Vector3.one;
            winStars[i].transform.localRotation = Quaternion.identity;
        }
        winScoreText.text = "0";

        Open(winPanel);
        StartCoroutine(CountUp(score));
        StartCoroutine(RevealStars(stars));
    }

    public void ShowLose()
    {
        if (gameEnded) return;
        gameEnded = true;
        Open(losePanel);
    }

    IEnumerator CountUp(int target)
    {
        yield return new WaitForSecondsRealtime(0.5f);

        const float duration = 0.9f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / duration, 3f);
            winScoreText.text = Mathf.RoundToInt(target * k).ToString();
            yield return null;
        }
        winScoreText.text = target.ToString();
    }

    IEnumerator RevealStars(int stars)
    {
        yield return new WaitForSecondsRealtime(0.8f);

        for (int i = 0; i < stars && i < winStars.Length; i++)
        {
            winStars[i].sprite = starOn;
            StartCoroutine(Flash(i));
            StartCoroutine(Sparkles(i));
            StartCoroutine(Pop(winStars[i].transform));
            yield return new WaitForSecondsRealtime(0.32f);
        }
    }

    IEnumerator Pop(Transform t)
    {
        const float duration = 0.4f;
        const float split = 0.55f;

        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float k = time / duration;
            float s = k < split
                ? Mathf.Lerp(0.3f, 1.35f, EaseOut(k / split))
                : Mathf.Lerp(1.35f, 1f, EaseOut((k - split) / (1f - split)));
            t.localScale = Vector3.one * s;
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-35f, 0f, EaseOut(k)));
            yield return null;
        }
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;
    }

    IEnumerator Flash(int index)
    {
        Image img = GetFlash(index);
        img.gameObject.SetActive(true);

        const float duration = 0.6f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            img.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.15f, EaseOut(k));
            img.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-15f, 25f, k));

            float a = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
            Color c = flashColor;
            c.a = flashColor.a * a * a;
            img.color = c;
            yield return null;
        }
        img.gameObject.SetActive(false);
    }

    IEnumerator Sparkles(int index)
    {
        RectTransform starRt = (RectTransform)winStars[index].transform;
        float starSize = starRt.rect.width;
        Vector2 center = starRt.anchoredPosition;

        int n = Mathf.Max(0, sparkCount);
        var items = new Image[n];
        var dirs = new Vector2[n];
        var dists = new float[n];
        var spins = new float[n];

        for (int i = 0; i < n; i++)
        {
            float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            dirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            dists[i] = starSize * sparkDistance * Random.Range(0.7f, 1.3f);
            spins[i] = Random.Range(-120f, 120f);

            Image img = GetSpark(starRt);
            float size = starSize * Random.Range(0.18f, 0.34f);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            img.rectTransform.anchoredPosition = center;
            img.color = Color.Lerp(flashColor, Color.white, 0.55f);
            img.gameObject.SetActive(true);
            items[i] = img;
        }

        const float duration = 0.65f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            float move = EaseOut(k);
            float s = Mathf.Sin(k * Mathf.PI);
            float a = 1f - k * k;

            for (int i = 0; i < n; i++)
            {
                var rt = items[i].rectTransform;
                rt.anchoredPosition = center + dirs[i] * dists[i] * move;
                rt.localScale = Vector3.one * s;
                rt.localRotation = Quaternion.Euler(0f, 0f, spins[i] * k);

                Color c = items[i].color;
                c.a = a;
                items[i].color = c;
            }
            yield return null;
        }

        for (int i = 0; i < n; i++) items[i].gameObject.SetActive(false);
    }

    Image CreateOverlay(string name, RectTransform like, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                                typeof(Image), typeof(LayoutElement));
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        var rt = (RectTransform)go.transform;
        rt.SetParent(like.parent, false);
        rt.anchorMin = like.anchorMin;
        rt.anchorMax = like.anchorMax;
        rt.pivot = like.pivot;
        rt.anchoredPosition = like.anchoredPosition;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    Image GetFlash(int index)
    {
        if (flashes == null || flashes.Length != winStars.Length)
            flashes = new Image[winStars.Length];

        if (flashes[index] != null) return flashes[index];

        RectTransform starRt = (RectTransform)winStars[index].transform;
        Image img = CreateOverlay("Flash", starRt, GetGlowSprite());
        img.rectTransform.SetAsFirstSibling();
        img.rectTransform.sizeDelta = starRt.sizeDelta * flashSize;

        flashes[index] = img;
        return img;
    }

    Image GetSpark(RectTransform like)
    {
        foreach (var s in sparkPool)
            if (s != null && !s.gameObject.activeSelf) return s;

        Image img = CreateOverlay("Spark", like, GetSparkSprite());
        img.rectTransform.SetAsLastSibling();
        sparkPool.Add(img);
        return img;
    }

    static Sprite GetGlowSprite()
    {
        if (glowSprite == null)
            glowSprite = MakeStarSprite(128, points: 5, sharpness: 5f, innerRadius: 0.28f, core: 0.4f);
        return glowSprite;
    }

    static Sprite GetSparkSprite()
    {
        if (sparkSprite == null)
            sparkSprite = MakeStarSprite(64, points: 4, sharpness: 6f, innerRadius: 0.1f, core: 0.25f);
        return sparkSprite;
    }

    static Sprite MakeStarSprite(int size, int points, float sharpness, float innerRadius, float core)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float ang = Mathf.Atan2(dy, dx) - Mathf.PI * 0.5f;
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * points * 0.5f)), sharpness);

                float radius = Mathf.Lerp(innerRadius, 1f, ray);
                float a = Mathf.Clamp01(1f - d / radius);
                a *= a;

                float c = Mathf.Clamp01(1f - d / core);
                a = Mathf.Max(a, c * c);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    IEnumerator Punch(Transform t, float peak)
    {
        const float duration = 0.25f;
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float k = time / duration;
            t.localScale = Vector3.one * Mathf.Lerp(peak, 1f, EaseOut(k));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    static float EaseOut(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);

    public void Restart() => Leave(LevelLoader.Reload);
    public void NextLevel() => Leave(LevelLoader.Next);
    public void GoToMenu() => Leave(LevelLoader.LoadMenu);
}