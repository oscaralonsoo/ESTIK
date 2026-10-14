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

    bool gameEnded;

    void Start()
    {
        pausePanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!gameEnded && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pausePanel.activeSelf) Resume();
            else Pause();
        }
    }

    public void SetScore(int score) => scoreText.text = score.ToString();

    public void Pause()
    {
        if (gameEnded) return;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowWin(int score, int stars)
    {
        gameEnded = true;
        SaveLevelData.SetStars(LevelLoader.CurrentLevel, stars);

        winScoreText.text = score.ToString();
        for (int i = 0; i < winStars.Length; i++)
            winStars[i].sprite = i < stars ? starOn : starOff;

        winPanel.SetActive(true);
    }

    public void ShowLose()
    {
        gameEnded = true;
        losePanel.SetActive(true);
    }

    public void Restart() => LevelLoader.Reload();
    public void NextLevel() => LevelLoader.Next();
    public void GoToMenu() => LevelLoader.LoadMenu();
}