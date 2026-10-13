using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject levelsPanel;
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject quitButton;
    [SerializeField] OptionsUI options;

    void Start()
    {
#if UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID
        quitButton.SetActive(false);
#endif
        options.ApplySaved();
        ShowMain();
    }

    void Show(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        levelsPanel.SetActive(panel == levelsPanel);
        optionsPanel.SetActive(panel == optionsPanel);
    }

    public void ShowMain() => Show(mainPanel);
    public void ShowLevels() => Show(levelsPanel);
    public void ShowOptions() => Show(optionsPanel);
    public void Quit() => Application.Quit();
}