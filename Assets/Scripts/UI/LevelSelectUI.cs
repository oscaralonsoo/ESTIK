using UnityEngine;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] LevelButton buttonPrefab;
    [SerializeField] Transform container;  
    [SerializeField] float stagger = 0.04f;

    void OnEnable()
    {
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        for (int i = 1; i <= LevelLoader.TotalLevels; i++)
        {
            LevelButton btn = Instantiate(buttonPrefab, container);
            btn.Setup(i);

            if (btn.TryGetComponent(out ButtonFeedback fb))
                fb.Appear((i - 1) * stagger);
        }
    }
}