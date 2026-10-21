using UnityEngine;
using TMPro;

public class NumberLevelController : MonoBehaviour
{
    private TMP_Text level;

    void Start()
    {
        level = GetComponent<TMP_Text>();

        int nivel = PlayerPrefs.GetInt("Nivel", 1);

        level.text = nivel.ToString();
    }
}