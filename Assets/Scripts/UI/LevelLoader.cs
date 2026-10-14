using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelLoader
{
    public const string MenuScene = "UI";
    public const int TotalLevels = 12;
    public static int CurrentLevel = 1;

    public static void Load(int level)
    {
        CurrentLevel = level;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level" + level);
    }

    public static void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScene);
    }

    public static void Reload() => Load(CurrentLevel);

    public static void Next()
    {
        if (CurrentLevel < TotalLevels) Load(CurrentLevel + 1);
        else LoadMenu();
    }
}