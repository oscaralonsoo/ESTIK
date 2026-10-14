using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] TMP_Text scoreText;

    [Header("Paneles")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    [Header("Efecto de Victoria")]
    [SerializeField] UIStarRewardFX starRewardFX;

    bool gameEnded, paused, leaving;
    Coroutine scorePunch;

    void Start()
    {
        EnsureButtonFeedback();

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

        Open(winPanel);

        if (starRewardFX != null)
            starRewardFX.PlayRewardSequence(score, stars);
    }

    public void ShowLose()
    {
        if (gameEnded) return;
        gameEnded = true;
        Open(losePanel);
    }

    void EnsureButtonFeedback()
    {
        var allButtons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var b in allButtons)
        {
            if (b.GetComponent<ButtonFeedback>() == null)
                b.gameObject.AddComponent<ButtonFeedback>();
        }
    }

    System.Collections.IEnumerator Punch(Transform t, float peak)
    {
        const float duration = 0.25f;
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float k = time / duration;
            t.localScale = Vector3.one * Mathf.Lerp(peak, 1f, 1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    public void Restart() => Leave(LevelLoader.Reload);
    public void NextLevel() => Leave(LevelLoader.Next);
    public void GoToMenu() => Leave(LevelLoader.LoadMenu);
}