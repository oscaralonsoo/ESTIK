using UnityEngine;
using UnityEngine.UI;

public class ModeSelectUI : MonoBehaviour
{
    [SerializeField] MainMenuUI mainMenu;
    [SerializeField] Button normalModeButton;
    [SerializeField] Button randomModeButton;
    [SerializeField] Button backButton;

    void Awake()
    {
        if (normalModeButton) normalModeButton.onClick.AddListener(() => LevelLoader.StartRun(PlayMode.Normal));
        if (randomModeButton) randomModeButton.onClick.AddListener(() => LevelLoader.StartRun(PlayMode.Random));
        if (backButton && mainMenu) backButton.onClick.AddListener(mainMenu.ShowMain);
    }
}