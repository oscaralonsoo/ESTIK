using UnityEngine;
using UnityEngine.SceneManagement;

public class HideInTutorial : MonoBehaviour
{
    void Awake()
    {
        LevelLoader.SyncWithScene(SceneManager.GetActiveScene().name);

        if (LevelLoader.GetCurrentLevel() < 0)
            gameObject.SetActive(false);
    }
}