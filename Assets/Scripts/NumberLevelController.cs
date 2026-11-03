using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class NumberLevelController : MonoBehaviour
{
    private TMP_Text level;

    void Start()
    {
        level = GetComponent<TMP_Text>();

        LevelLoader.SyncWithScene(SceneManager.GetActiveScene().name);

        int levelNum = LevelLoader.GetCurrentLevel();

        if (levelNum < 0) level.text = "Tutorial";
        else level.text = levelNum.ToString();
    }
}