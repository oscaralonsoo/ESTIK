using UnityEngine;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] LevelButton buttonPrefab;
    [SerializeField] Transform container;   

    void OnEnable()
    {
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        for (int i = 1; i <= LevelLoader.TotalLevels; i++)
            Instantiate(buttonPrefab, container).Setup(i);
    }
}