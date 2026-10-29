using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [Header("HUD")]
    public GameObject hudPanel;          // HUD con el boton de pausa

    [Header("Paneles")]
    public GameObject pausePanel;        // Resume / Options / Skins
    public GameObject optionsPanel;
    public GameObject customBallPanel;   // Se controla solo

    GameObject currentPanel;             // Panel secundario abierto (options / skins)
    bool paused;
    bool transitioning;

    public bool IsPaused => paused;

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
        // Evita dejar el juego congelado si se desactiva el canvas estando en pausa
        Time.timeScale = 1f;
        paused = false;
    }

    void ResetState()
    {
        Time.timeScale = 1f;
        paused = false;
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

        if (!paused) OnPausePressed();
        else if (currentPanel != null) OnBackPressed();
        else OnResumePressed();
    }

    // ---------- Botones ----------

    // Boton de pausa del HUD
    public void OnPausePressed()
    {
        if (paused || transitioning) return;

        paused = true;
        Time.timeScale = 0f;

        if (hudPanel) hudPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    // Boton Resume
    public void OnResumePressed()
    {
        if (!paused || transitioning) return;

        // Si hay un panel secundario abierto, se cierra sin animar
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }

        if (pausePanel) pausePanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);

        paused = false;
        Time.timeScale = 1f;
    }

    public void OnOptionsPressed() => OpenPanel(optionsPanel);

    public void OnSkinsPressed() => OpenPanel(customBallPanel);

    // Boton Back de Options / llamalo tambien desde el cierre de CustomBallPanel
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

    // Si CustomBallPanel se cierra solo con su propio boton, llama a esto desde ahi
    // (OnClick -> PauseMenuManager.OnSecondaryPanelClosed) para volver al menu de pausa
    public void OnSecondaryPanelClosed()
    {
        if (!paused) return;

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }
        transitioning = false;
        if (pausePanel) pausePanel.SetActive(true);
    }

    // ---------- Internos ----------

    void OpenPanel(GameObject panel)
    {
        if (panel == null || !paused || transitioning || currentPanel == panel) return;

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
