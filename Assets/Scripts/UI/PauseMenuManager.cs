using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GamePaused { get; private set; }

    [Header("HUD")]
    public GameObject hudPanel;

    [Header("Paneles")]
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject customBallPanel;
    public GameObject gameOverPanel;
    public GameObject youWinPanel;

    [Header("HUD Buttons Anim")]
    [SerializeField] float hudButtonStagger = 0.05f;
    [SerializeField] float hudHideTime = 0.3f;
    [SerializeField] Vector2 hudButtonRise = new Vector2(0f, -30f);

    GameObject currentPanel;
    bool transitioning;
    bool gameOver;
    Button[] hudButtons;
    Coroutine hudRoutine;

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
        gameOver = false;
        currentPanel = null;

        if (hudPanel) hudPanel.SetActive(true);
        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (customBallPanel) customBallPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (youWinPanel) youWinPanel.SetActive(false);
    }

    void Update()
    {
        if (gameOver) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (!GamePaused) OnPausePressed();
        else if (currentPanel != null) OnBackPressed();
        else OnResumePressed();
    }

    // Lo llama FastSceneLoader cuando te quedas sin vidas
    public void ShowGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        transitioning = false;

        GamePaused = true;
        Time.timeScale = 0f;

        // Cierra cualquier panel de pausa que estuviera abierto
        if (currentPanel != null) { currentPanel.SetActive(false); currentPanel = null; }
        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (customBallPanel) customBallPanel.SetActive(false);

        HideHud();
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // Lo llama FastSceneLoader al completar el ultimo nivel (no hay mas niveles que cargar).
    // Devuelve false si no hay youWinPanel asignado, para que el loader use su comportamiento normal.
    public bool ShowYouWin()
    {
        if (youWinPanel == null) return false;
        if (gameOver) return true;

        gameOver = true;   // bloquea Escape / pausa mientras el panel esta abierto
        transitioning = false;

        GamePaused = true;
        Time.timeScale = 0f;

        if (currentPanel != null) { currentPanel.SetActive(false); currentPanel = null; }
        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (customBallPanel) customBallPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        HideHud();
        youWinPanel.SetActive(true);
        return true;
    }

    // Botones del YouWinPanel (hacen lo mismo que los del GameOverPanel)
    public void OnYouWinRetryPressed() => OnGameOverRetryPressed();
    public void OnYouWinMenuPressed() => OnGameOverMenuPressed();

    // Boton "Jugar de nuevo" del GameOverPanel
    public void OnGameOverRetryPressed()
    {
        if (!gameOver || transitioning) return;
        transitioning = true;

        GamePaused = false;
        Time.timeScale = 1f;

        if (FastSceneLoader.Instance != null) FastSceneLoader.Instance.RetryAfterGameOver();
        else
        {
            LivesManager.Instance.ResetLives();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Boton "Menu" del GameOverPanel
    public void OnGameOverMenuPressed()
    {
        if (!gameOver || transitioning) return;
        transitioning = true;

        GamePaused = false;
        Time.timeScale = 1f;

        if (FastSceneLoader.Instance != null) FastSceneLoader.Instance.MenuAfterGameOver();
        else
        {
            LivesManager.Instance.ResetLives();
            SceneManager.LoadScene(0);
        }
    }

    public void OnPausePressed()
    {
        if (GamePaused || transitioning || gameOver) return;

        GamePaused = true;
        Time.timeScale = 0f;

        HideHud();
        if (pausePanel) pausePanel.SetActive(true);
    }
    void CacheHudButtons()
    {
        if (hudButtons != null || hudPanel == null) return;
        hudButtons = hudPanel.GetComponentsInChildren<Button>(true);
    }

    static ButtonFeedback GetFeedback(Button b)
    {
        var fb = b.GetComponent<ButtonFeedback>();
        if (fb == null) fb = b.gameObject.AddComponent<ButtonFeedback>();
        return fb;
    }

    void HideHud()
    {
        if (!hudPanel) return;
        CacheHudButtons();

        if (hudRoutine != null) StopCoroutine(hudRoutine);

        for (int i = 0; i < hudButtons.Length; i++)
        {
            if (hudButtons[i] == null || !hudButtons[i].gameObject.activeInHierarchy) continue;
            GetFeedback(hudButtons[i]).Disappear(i * hudButtonStagger);
        }

        hudRoutine = StartCoroutine(DeactivateHudAfter(hudHideTime));
    }

    IEnumerator DeactivateHudAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (hudPanel) hudPanel.SetActive(false);
        hudRoutine = null;
    }

    void ShowHud()
    {
        if (!hudPanel) return;

        if (hudRoutine != null)
        {
            StopCoroutine(hudRoutine);
            hudRoutine = null;
        }

        hudPanel.SetActive(true);
        CacheHudButtons();

        for (int i = 0; i < hudButtons.Length; i++)
        {
            if (hudButtons[i] == null || !hudButtons[i].gameObject.activeInHierarchy) continue;
            GetFeedback(hudButtons[i]).Appear(i * hudButtonStagger, hudButtonRise);
        }
    }
    public void OnResumePressed()
    {
        if (!GamePaused || transitioning || gameOver) return;

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }

        if (pausePanel) pausePanel.SetActive(false);
        ShowHud();

        GamePaused = false;
        Time.timeScale = 1f;
    }
    public void OnMainMenuPressed()
    {
        if (transitioning) return;

        transitioning = true;

        if (FastSceneLoader.Instance != null)
        {
            FastSceneLoader.Instance.GoToMenu();
        }
        else
        {
            GamePaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    public void OnRestartPressed()
    {
        if (transitioning) return;
        transitioning = true;

        if (FastSceneLoader.Instance != null)
        {
            FastSceneLoader.Instance.RestartLevel();
        }
        else
        {
            LivesManager.Instance.LoseLife();
            GamePaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
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