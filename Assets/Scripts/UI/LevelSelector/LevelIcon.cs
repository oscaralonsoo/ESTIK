using System;
using UnityEngine;
using TMPro;

public class LevelIcon : MonoBehaviour
{
    public static event Action<LevelIcon> Selected;

    [SerializeField] GameObject levelLocker;
    [SerializeField] int level;
    TextMeshProUGUI text;
    UISFXManager sfx;

    public int Level => level;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        RefreshText();
    }

    public void SetSfx(UISFXManager manager)
    {
        sfx = manager;
    }

    public void SetLevel(int lvl)
    {
        level = lvl;
        RefreshText();
    }

    void RefreshText()
    {
        if (text)
            text.text = (level + 1).ToString();
    }

    public void UnlockLevel()
    {
        levelLocker.gameObject.SetActive(false);
    }

    public void OnClick()
    {
        bool locked = levelLocker && levelLocker.activeSelf;
        if (locked) return;

        if (sfx != null)
            sfx.PlayNormal();

        PlayerPrefs.SetInt("SelectedLevel", level);
        PlayerPrefs.Save();
        Selected?.Invoke(this);
    }
}