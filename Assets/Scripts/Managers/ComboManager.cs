using System;
using UnityEngine;
public class ComboManager : MonoBehaviour
{
    static ComboManager instance;

    public static ComboManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<ComboManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ComboManager");
                    instance = go.AddComponent<ComboManager>();
                }
            }
            return instance;
        }
    }

    [Header("MULTIPLICADOR")]
    [SerializeField, Min(1)] private int bonusPerLevel = 1;
    [SerializeField, Min(1)] private int maxMultiplier = 5;

    [Header("DEBUG")]
    [SerializeField] private int currentCombo;
    [SerializeField] private bool levelClean = true;

    public int CurrentCombo => currentCombo;

    public int CurrentMultiplier => Mathf.Min(1 + currentCombo * bonusPerLevel, maxMultiplier);

    public event Action<int, int> OnComboChanged;

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
    }

    public void OnLifeLost()
    {
        levelClean = false;
        SetCombo(0);
    }

    public void RegisterLevelCompleted()
    {
        if (levelClean) SetCombo(currentCombo + 1);
        levelClean = true;
    }

    public void ResetAll()
    {
        levelClean = true;
        SetCombo(0);
    }

    void SetCombo(int value)
    {
        currentCombo = Mathf.Max(0, value);
        OnComboChanged?.Invoke(currentCombo, CurrentMultiplier);
    }
}
