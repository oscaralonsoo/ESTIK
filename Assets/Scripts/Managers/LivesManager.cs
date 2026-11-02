using System;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    static LivesManager instance;

    public static LivesManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<LivesManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("LivesManager");
                    instance = go.AddComponent<LivesManager>();
                }
            }
            return instance;
        }
    }

    [Header("VIDAS")]
    [SerializeField, Min(1)] private int maxLives = 5;

    [Header("DEBUG")]
    [SerializeField] private int currentLives;

    public int MaxLives => maxLives;
    public int CurrentLives => currentLives;
    public bool HasLives => currentLives > 0;

    public event Action<int, int> OnLivesChanged;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        currentLives = maxLives;
    }

    public bool LoseLife()
    {
        currentLives = Mathf.Max(0, currentLives - 1);

        ComboManager.Instance.OnLifeLost();

        OnLivesChanged?.Invoke(currentLives, maxLives);
        return currentLives > 0;
    }

    public void ResetLives()
    {
        currentLives = maxLives;

        ComboManager.Instance.ResetAll();

        OnLivesChanged?.Invoke(currentLives, maxLives);
    }
}