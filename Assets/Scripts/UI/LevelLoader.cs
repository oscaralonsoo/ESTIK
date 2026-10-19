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
    const int MenuSceneIndex = 0; 
    const int FirstLevelSceneIndex = 1;  
    const int LevelCount = 10; 
    public static PlayMode Mode { get; private set; } = PlayMode.Normal;

    static readonly List<int> queue = new List<int>();
    static int position;
    static int lastLevelOfPreviousRun = -1;
    public static int CurrentLevel
    {
        get { EnsureRun(); return queue[position]; }
    }

    public static bool HasNext
    {
        get { EnsureRun(); return position + 1 < queue.Count; }
    }
    public static void StartRun(PlayMode mode)
    {
        Mode = mode;
        BuildQueue();
        position = 0;
        LoadCurrent();
    }

    public static void Next()
    {
        if (!HasNext)
        {
            LoadMenu();
            return;
        }

        position++;
        LoadCurrent();
    }

    public static void Reload()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void LoadMenu()
    {
        queue.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuSceneIndex);
    }

    static void LoadCurrent()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(FirstLevelSceneIndex + queue[position] - 1);
    }
    static void EnsureRun()
    {
        if (queue.Count > 0) return;

        Mode = PlayMode.Normal;
        BuildQueue();
        int current = SceneManager.GetActiveScene().buildIndex - FirstLevelSceneIndex;
        position = Mathf.Clamp(current, 0, queue.Count - 1);
    }

    static void BuildQueue()
    {
        queue.Clear();
        for (int level = 1; level <= LevelCount; level++)
            queue.Add(level);

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