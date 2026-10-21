using UnityEngine;
using TMPro;

public class NumberLevelController : MonoBehaviour
{
    private TMP_Text level;

    void Start()
    {
        level = GetComponent<TMP_Text>();

        int levelNum = LevelLoader.CurrentLevel;
        if (levelNum == 0) level.text = "Tutorial";
        else level.text = LevelLoader.CurrentLevel.ToString();
    }
}