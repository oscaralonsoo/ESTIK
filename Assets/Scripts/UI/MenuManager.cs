using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class MainMenuManager : MonoBehaviour
{
    [System.Serializable]
    public class BiomeMap
    {
        public string name;
        public int firstLevel;
        public GameObject mapPrefab;
        public GameObject existingInScene;
        public Vector3 mapPosition;
        public Material skybox;

        [Header("Cámara orbital de este bioma")]
        public float orbitRadius = 110f;
        public float orbitHeight = 25f;
        public float orbitLookHeight = 2f;

        [HideInInspector] public GameObject instance;
        [HideInInspector] public Transform orbitTarget;
    }

    [Header("Camaras / juego")]
    public OrbitCamera orbitCamera;
    public CameraFollow cameraController;
    public GameObject menuCanvas;
    public GameObject ball;

    [Header("Paneles")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject customizationPanel;
    public GameObject LevelSelector;

    [Header("Buttons")]
    public GameObject levelSelectorButton;

    [Header("Skins")]
    public SkinCarousel skinCarousel;

    [Header("Selector de niveles")]
    public float closeSelectorDelay = 0.8f;

    [Header("Biomas")]
    public BiomeMap[] biomes;

    GameObject currentPanel;
    BiomeMap currentBiome;
    bool transitioning;
    bool closingSelector;

    void OnEnable()
    {
        LevelIcon.Selected += OnLevelSelected;
    }

    void OnDisable()
    {
        LevelIcon.Selected -= OnLevelSelected;
    }

    void Start()
    {
        orbitCamera.enabled = true;
        cameraController.enabled = false;
        if (ball) ball.SetActive(false);

        if (optionsPanel) optionsPanel.SetActive(false);
        if (customizationPanel) customizationPanel.SetActive(false);
        if (LevelSelector) LevelSelector.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);
        currentPanel = null;

        levelSelectorButton.SetActive(PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1);

        ShowBiomeForLevel(PlayerPrefs.GetInt("SelectedLevel", 0));
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (currentPanel != null && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            OnBackPressed();
    }

    void OnLevelSelected(LevelIcon icon)
    {
        ShowBiomeForLevel(icon.Level);

        if (currentPanel == LevelSelector && !closingSelector)
            StartCoroutine(CloseSelectorRoutine());
    }

    IEnumerator CloseSelectorRoutine()
    {
        closingSelector = true;
        yield return new WaitForSecondsRealtime(closeSelectorDelay);
        closingSelector = false;

        if (currentPanel == LevelSelector) OnBackPressed();
    }
    void ShowBiomeForLevel(int level)
    {
        if (biomes == null || biomes.Length == 0) return;

        BiomeMap best = null;
        foreach (var b in biomes)
        {
            if (level >= b.firstLevel && (best == null || b.firstLevel > best.firstLevel))
                best = b;
        }
        if (best == null) best = biomes[0];
        if (best == currentBiome) return;

        currentBiome = best;
        EnsureInstance(best);

        foreach (var b in biomes)
        {
            if (b == best) continue;
            if (b.existingInScene) b.existingInScene.SetActive(false);
            if (b.instance) b.instance.SetActive(false);
        }

        if (best.instance) best.instance.SetActive(true);

        if (best.orbitTarget)
            orbitCamera.SetTarget(best.orbitTarget, best.orbitRadius, best.orbitHeight, best.orbitLookHeight);

        if (best.skybox)
        {
            RenderSettings.skybox = best.skybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    void EnsureInstance(BiomeMap b)
    {
        if (b.instance != null) return;

        if (b.existingInScene != null)
            b.instance = b.existingInScene;
        else if (b.mapPrefab != null)
            b.instance = Instantiate(b.mapPrefab, b.mapPosition, Quaternion.identity);

        if (b.instance == null) return;

        Transform t = b.instance.transform.Find("OrbitTarget");
        b.orbitTarget = t != null ? t : b.instance.transform;
    }
    public void OnPlayPressed()
    {
        orbitCamera.enabled = false;
        cameraController.enabled = true;

        menuCanvas.SetActive(false);
        if (ball) ball.SetActive(true);
    }
    public void OnLevelSelectorPressed() => OpenPanel(LevelSelector);
    public void OnOptionsPressed() => OpenPanel(optionsPanel);
    public void OnCustomizationPressed() => OpenPanel(customizationPanel);
    public void OnBackPressed()
    {
        if (currentPanel == null || transitioning) return;

        if (skinCarousel != null && skinCarousel.IsUnlocking) return;

        transitioning = true;
        GameObject closing = currentPanel;
        currentPanel = null;

        HidePanel(closing, () =>
        {
            if (mainPanel) mainPanel.SetActive(true);
            transitioning = false;
        });
    }
    void OpenPanel(GameObject panel)
    {
        if (panel == null || transitioning || currentPanel == panel) return;

        transitioning = true;

        void Show()
        {
            panel.SetActive(true);
            currentPanel = panel;
            transitioning = false;
        }

        if (mainPanel && mainPanel.activeSelf)
            HidePanel(mainPanel, Show);
        else
            Show();
    }

    static void HidePanel(GameObject panel, System.Action onDone)
    {
        var animator = panel.GetComponent<PanelAnimator>();
        if (animator != null && panel.activeInHierarchy)
        {
            animator.Hide(onDone);
        }
        else
        {
            panel.SetActive(false);
            onDone?.Invoke();
        }
    }
}