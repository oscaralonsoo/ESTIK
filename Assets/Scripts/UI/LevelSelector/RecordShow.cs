using TMPro;
using UnityEngine;

public class RecordShow : MonoBehaviour
{
    private RectTransform rectTransform;
    private TextMeshProUGUI textRecord;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textRecord = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetTextX(float x)
    {
        RectTransform rectTransform = textRecord.GetComponent<RectTransform>();
        if (!rectTransform) return;
        rectTransform.localPosition = new Vector3(x, rectTransform.localPosition.y, rectTransform.localPosition.z);
    }
    
    public void SetY(float y)
    {
        if (!rectTransform) return;
        rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, y, rectTransform.localPosition.z);
    }

    public void SetRecord(int record)
    {
        if (!textRecord) return;
        textRecord.text = "Record: "+record.ToString();
    }
}
