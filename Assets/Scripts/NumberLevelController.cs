using UnityEngine;
using TMPro;

public class NumberLevelController : MonoBehaviour
{
    private TMP_Text level;

    void Start()
    {
        level = GetComponent<TMP_Text>();

        level.text = LevelLoader.CurrentLevel.ToString();
    }
}