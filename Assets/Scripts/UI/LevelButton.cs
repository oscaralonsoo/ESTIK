using TMPro;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text numberText;
    [SerializeField] GameObject lockIcon;
    [SerializeField] Image[] stars;        
    [SerializeField] Sprite starOn;
    [SerializeField] Sprite starOff;

    public void Setup(int level)
    {
        bool unlocked = SaveData.IsUnlocked(level);
        int earned = SaveData.GetStars(level);

        numberText.text = level.ToString();
        numberText.gameObject.SetActive(unlocked);
        lockIcon.SetActive(!unlocked);
        button.interactable = unlocked;

        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].gameObject.SetActive(unlocked);
            stars[i].sprite = i < earned ? starOn : starOff;
        }

        button.onClick.AddListener(() => LevelLoader.Load(level));
    }
}