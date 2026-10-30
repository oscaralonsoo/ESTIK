using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GamePaused { get; private set; }

    [Header("HUD")]
    public GameObject hudPanel;

    [Header("Paneles")]
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject customBallPanel;

    GameObject currentPanel;
    bool transitioning;

    public bool IsPaused => GamePaused;

    void Start()
    {
        ResetState();
    }

    void OnEnable()
    {
        ResetState();
    }

    void OnDisable()
    {
        GamePaused = false;
        Time.timeScale = 1f;
    }

    void ResetState()
    {
        GamePaused = false;
        Time.timeScale = 1f;
        transitioning = false;
        currentPanel = null;

        if (hudPanel) hudPanel.SetActive(true);
        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (customBallPanel) customBallPanel.SetActive(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (!GamePaused) OnPausePressed();
        else if (currentPanel != null) OnBackPressed();
        else OnResumePressed();
    }
    public void OnPausePressed()
    {
        if (GamePaused || transitioning) return;

        GamePaused = true;
        Time.timeScale = 0f;

        if (hudPanel) hudPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    // Boton Resume
    public void OnResumePressed()
    {
        if (!GamePaused || transitioning) return;

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }

        if (pausePanel) pausePanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);

        GamePaused = false;
        Time.timeScale = 1f;
    }

    public void OnOptionsPressed() => OpenPanel(optionsPanel);

    public void OnSkinsPressed() => OpenPanel(customBallPanel);

    public void OnBackPressed()
    {
        if (currentPanel == null || transitioning) return;

        transitioning = true;
        GameObject closing = currentPanel;
        currentPanel = null;

        HidePanel(closing, () =>
        {
            if (pausePanel) pausePanel.SetActive(true);
            transitioning = false;
        });
    }
    public void OnSecondaryPanelClosed()
    {
        if (!GamePaused) return;

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }
        transitioning = false;
        if (pausePanel) pausePanel.SetActive(true);
    }

    void OpenPanel(GameObject panel)
    {
        if (panel == null || !GamePaused || transitioning || currentPanel == panel) return;

        transitioning = true;

        void Show()
        {
            panel.SetActive(true);
            currentPanel = panel;
            transitioning = false;
        }

        if (pausePanel && pausePanel.activeSelf)
            HidePanel(pausePanel, Show);
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