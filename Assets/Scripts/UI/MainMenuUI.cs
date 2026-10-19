using UnityEngine;
using UnityEngine.Serialization;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject modePanel;

    bool switching;

    void Start()
    {
        modePanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    void Switch(GameObject target, System.Action open)
    {
        if (switching) return;

        GameObject current = mainPanel.activeSelf ? mainPanel
                           : modePanel.activeSelf ? modePanel
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

    public void ShowModes() => Switch(modePanel, () => modePanel.SetActive(true));

    public void Quit() => Application.Quit();
}