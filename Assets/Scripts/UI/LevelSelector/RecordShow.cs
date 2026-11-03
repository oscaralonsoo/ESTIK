using TMPro;
using UnityEngine;

public class RecordShow : MonoBehaviour
{
    [Header("Pulsación")]
    [SerializeField] private RectTransform pulseTarget;
    [Tooltip("Pulsaciones por segundo")]
    [SerializeField] private float pulseSpeed = 0.75f;
    [SerializeField] private float pulseAmount = 0.05f;

    private RectTransform rectTransform;
    private TextMeshProUGUI textRecord;
    private Vector3 baseScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textRecord = GetComponentInChildren<TextMeshProUGUI>();

        if (pulseTarget == null) pulseTarget = rectTransform;
        if (pulseTarget != null) baseScale = pulseTarget.localScale;
    }

    void Update()
    {
        if (pulseTarget == null) return;

        float wave = Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
        pulseTarget.localScale = baseScale * (1f + pulseAmount * wave);
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
        textRecord.text = "Record: " + record.ToString();
    }
}