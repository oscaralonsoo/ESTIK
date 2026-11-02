using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LivesText : MonoBehaviour
{
    [Tooltip("{0} = vidas actuales, {1} = vidas maximas. Ejemplos: \"{0}\", \"x{0}\", \"{0}/{1}\"")]
    [SerializeField] private string format = "{0}";

    private TMP_Text label;
    private LivesManager manager;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        manager = LivesManager.Instance;
        manager.OnLivesChanged += OnLivesChanged;
        Refresh();
    }

    void Start()
    {
        Refresh();
    }

    void OnDisable()
    {
        if (manager != null)
        {
            manager.OnLivesChanged -= OnLivesChanged;
            manager = null;
        }
    }

    void OnLivesChanged(int current, int max)
    {
        label.text = string.Format(format, current, max);
    }

    void Refresh()
    {
        if (manager == null || label == null) return;
        label.text = string.Format(format, manager.CurrentLives, manager.MaxLives);
    }
}
