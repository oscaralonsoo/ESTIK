using UnityEngine;
using TMPro;
using Unity.VectorGraphics;

public class LevelIcon : MonoBehaviour
{
    [SerializeField] GameObject levelLocker;
    [SerializeField] int level;
    TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (text)
            text.text  = level.ToString();
    }

    public void SetLevel(int lvl)
    {
        level = lvl;
        if (text)
            text.text = level.ToString();
    }

    public void UnlockLevel()
    {
        levelLocker.gameObject.SetActive(false);
        
    }

    public void OnClick()
    {
        PlayerPrefs.SetInt("SelectedLevel", level);
        PlayerPrefs.Save();
    }
}
