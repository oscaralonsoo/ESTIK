using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayMode
{
    Normal,
    Random
}

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
        "Level1",
        "Level2",
        "Level3",
        "Level4",
        "Level5",
        "Level6",
        "Level7",
        "Level8",
        "Level9",
        "Level10"
    };

    public static PlayMode Mode { get; private set; } = PlayMode.Normal;
    public static bool IsInTutorial { get; private set; } = true;

    static readonly List<int> queue = new List<int>();
    static int position;
    static int lastLevelOfPreviousRun = -1;

    public static int CurrentLevel
    {
        get
        {
            if (IsInTutorial) return 0;
            EnsureRun();
            return queue[position] + 1;
        }
    }

    public static bool HasNext
    {
        get
        {
            if (IsInTutorial) return position + 1 < tutorialScenes.Count;
            EnsureRun();
            return position + 1 < queue.Count;
        }
    }

    public static AsyncOperation StartGame(PlayMode mode)
    {
        Mode = mode;
        bool completedTutorial = PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;

        if (!completedTutorial && tutorialScenes.Count > 0)
        {
            IsInTutorial = true;
            position = 0;
            Time.timeScale = 1f;
            return SceneManager.LoadSceneAsync(tutorialScenes[position]);
        }

        return StartMainRun(mode);
    }

    public static AsyncOperation StartMainRun(PlayMode mode)
    {
        Mode = mode;
        IsInTutorial = false;
        BuildQueue();
        position = 0;
        return LoadCurrent();
    }

    public static AsyncOperation Next()
    {
        if (IsInTutorial)
        {
            position++;
            if (position < tutorialScenes.Count)
            {
                Time.timeScale = 1f;
                return SceneManager.LoadSceneAsync(tutorialScenes[position]);
            }

            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();
            return StartMainRun(Mode);
        }

        if (!HasNext)
        {
            return LoadMenu();
        }

        position++;
        return LoadCurrent();
    }

    public static AsyncOperation Reload()
    {
        Time.timeScale = 1f;
        return SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    public static AsyncOperation LoadMenu()
    {
        queue.Clear();
        IsInTutorial = false;
        Time.timeScale = 1f;
        return SceneManager.LoadSceneAsync(MenuSceneName);
    }

    static AsyncOperation LoadCurrent()
    {
        Time.timeScale = 1f;
        int levelIndex = queue[position];
        return SceneManager.LoadSceneAsync(mainLevelScenes[levelIndex]);
    }

    static void EnsureRun()
    {
        if (queue.Count > 0) return;

        Mode = PlayMode.Normal;
        IsInTutorial = false;
        BuildQueue();

        string currentScene = SceneManager.GetActiveScene().name;
        int indexInMain = mainLevelScenes.IndexOf(currentScene);

        if (indexInMain >= 0)
        {
            position = queue.IndexOf(indexInMain);
            if (position < 0) position = 0;
        }
        else
        {
            position = 0;
        }
    }

    static void BuildQueue()
    {
        queue.Clear();
        for (int i = 0; i < mainLevelScenes.Count; i++)
            queue.Add(i);

        if (Mode != PlayMode.Random) return;

        for (int i = queue.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (queue[i], queue[j]) = (queue[j], queue[i]);
        }

        if (queue.Count > 1 && queue[0] == lastLevelOfPreviousRun)
            (queue[0], queue[queue.Count - 1]) = (queue[queue.Count - 1], queue[0]);

        lastLevelOfPreviousRun = queue[queue.Count - 1];
    }
}