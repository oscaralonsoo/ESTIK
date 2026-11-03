using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelLoader
{
    const string MenuSceneName = "MainMenu";

    static readonly List<string> tutorialScenes = new List<string>
    {
        "Tuto1",
        "Tuto2",
        "Tuto3"
    };

    static readonly List<string> mainLevelScenes = new List<string>
    {
        "Level0",
        "Level1",
        "Level2",
        "Level3",
        "Level4",
        "Level5",
        "Level6",
        "Level7",
        "Level8",
        "Level9",
        "Level10",
        "Level11",
        "Level12",
        "Level13",
        "Level14",
        "Level15",
        "Level16",
        "Level17",
        "Level18",


    };

    static int currentLevel = 0;
    static int recordAtRunStart = int.MaxValue;

    public static bool IsNewRecord => currentLevel > 0 && currentLevel > recordAtRunStart;

    static void SnapshotRecord()
    {
        recordAtRunStart = PlayerPrefs.GetInt("Record", 1);
    }

    static int consecutiveLevels = 0;

    public static AsyncOperation StartGame()
    {
        bool completedTutorial = PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;

        currentLevel = completedTutorial ? PlayerPrefs.GetInt("SelectedLevel", 0) : -tutorialScenes.Count;

        SnapshotRecord();
        return LoadCurrent();
    }
    public static AsyncOperation RestartFromFirstLevel()
    {
        SnapshotRecord();
        return LoadSpecificLevel(0);
    }

    public static AsyncOperation LoadNext()
    {

        if (currentLevel >= 0)
        {
            consecutiveLevels++;
        }

        currentLevel++;

        if (currentLevel == 0)
        {
            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();
        }

        if (currentLevel >= mainLevelScenes.Count)
        {
            currentLevel = 0;

            Time.timeScale = 1f;
            return LoadSceneWithMusicFade(MenuSceneName);
        }

        if (currentLevel > 0)
        {
            int record = PlayerPrefs.GetInt("Record", 1);
            if (currentLevel > record)
            {
                if (currentLevel < mainLevelScenes.Count)
                {
                    PlayerPrefs.SetInt("Record", currentLevel);
                    PlayerPrefs.Save();
                }
            }
        }

        return LoadCurrent();
    }

    public static AsyncOperation LoadMenu()
    {
        consecutiveLevels = 0;
        return LoadSceneWithMusicFade(MenuSceneName);
    }

    static AsyncOperation LoadCurrent()
    {
        return LoadSpecificLevel(currentLevel);
    }

    public static AsyncOperation Reload()
    {
        consecutiveLevels = 0;
        Time.timeScale = 1f;
        return LoadSceneWithMusicFade(SceneManager.GetActiveScene().name);
    }

    public static AsyncOperation LoadSpecificLevel(int sceneNum)
    {
        Time.timeScale = 1f;
        string sceneName = "";
        if (sceneNum < 0) sceneName = GetTutorialLevelName(sceneNum);
        else sceneName = GetLevelName(sceneNum);

        currentLevel = sceneNum;
        return LoadSceneWithMusicFade(sceneName);
    }

    public static void SyncWithScene(string sceneName)
    {
        int i = mainLevelScenes.IndexOf(sceneName);
        if (i >= 0) { currentLevel = i; return; }

        i = tutorialScenes.IndexOf(sceneName);
        if (i >= 0) currentLevel = i - tutorialScenes.Count;
    }

    public static bool IsTutorialScene(string sceneName)
    {
        return tutorialScenes.Contains(sceneName);
    }

    public static string GetTutorialLevelName(int num)
    {
        if (num >= 0) return "";
        else if (num < -tutorialScenes.Count) return tutorialScenes[0];
        else return tutorialScenes[tutorialScenes.Count + num];
    }

    public static string GetLevelName(int num)
    {
        if (num < 0) return "";
        else if (num >= mainLevelScenes.Count) return mainLevelScenes[mainLevelScenes.Count - 1];
        else return mainLevelScenes[num];
    }

    public static int GetCurrentLevel()
    {
        return currentLevel;
    }

    public static int GetConsecutiveLevels()
    {
        return consecutiveLevels;
    }

    static AsyncOperation LoadSceneWithMusicFade(string sceneName)
    {
        AmbientMusicManager.FadeOut();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        float fadeTime = AmbientMusicManager.GetFadeOutDuration();

        if (fadeTime > 0f)
        {
            op.allowSceneActivation = false;
            AmbientMusicManager.RunAfterDelay(fadeTime, () => op.allowSceneActivation = true);
        }

        return op;
    }
}