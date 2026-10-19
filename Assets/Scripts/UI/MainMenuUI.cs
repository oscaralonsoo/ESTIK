using Unity.VisualScripting;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject levelsPanel;

    bool switching;

    void Start()
    {
//#if UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID
//        quitButton.SetActive(false);
//#endif  
        levelsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
    void Switch(GameObject target, System.Action open)
    {
        if (switching) return;

        GameObject current = mainPanel.activeSelf ? mainPanel
                           : levelsPanel.activeSelf ? levelsPanel
                           : null;

        if (current == target) return;
        if (current == null) { open(); return; }

        switching = true;
        void Next()
        {
            switching = false;
            open();
        }

        var anim = current.GetComponent<PanelAnimator>();
        if (anim != null) anim.Hide(Next);
        else
        {
            current.SetActive(false);
            Next();
        }
    }

    public void ShowMain() => Switch(mainPanel, () => mainPanel.SetActive(true));
    public void ShowLevels() => Switch(levelsPanel, () => levelsPanel.SetActive(true));
    public void Quit() => Application.Quit();
}