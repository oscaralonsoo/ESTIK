
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public GameState currentState;

    private void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    private void Update()
    {
        switch (currentState)
        {
            case GameState.MainMenu:
                // Main menu logic
                break;

            case GameState.Playing:
                // Gameplay logic
                break;

            case GameState.Paused:
                // Pause logic
                break;

            case GameState.GameOver:
                // Game over logic
                break;
        }
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log("Game State: " + currentState);

        switch (currentState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }
    }
}
