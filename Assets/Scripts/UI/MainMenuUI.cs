using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
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

    GameObject currentPanel;
    bool transitioning;

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
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (currentPanel != null && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            OnBackPressed();
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