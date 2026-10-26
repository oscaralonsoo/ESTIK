// SkinCarouselItem.cs
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class SkinCarouselItem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform ball;            // la bola 3D hija del prefab
    [SerializeField] Renderer ballRenderer;     // el MeshRenderer de esa bola
    [SerializeField] GameObject lockIcon;       // opcional

    [Header("Feel")]
    [SerializeField] float spinSpeed = 40f;

    public RectTransform Rect { get; private set; }
    public int Index { get; private set; }

    CanvasGroup _group;
    bool _selected;

    void Awake()
    {
        Rect = (RectTransform)transform;
        _group = GetComponent<CanvasGroup>();
    }

    public void Init(SkinData data, int index)
    {
        Index = index;

        if (ball == null || ballRenderer == null)
        {
            Debug.LogError("SkinCarouselItem: faltan Ball o Ball Renderer en el prefab", this);
            return;
        }

        if (data.ballMaterial != null)
            ballRenderer.sharedMaterial = data.ballMaterial;
        else
            Debug.LogWarning($"Skin '{data.id}' no tiene Ball Material asignado", data);

        if (lockIcon != null)
            lockIcon.SetActive(!SkinService.IsUnlocked(data));
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (!selected && ball != null)
            ball.localRotation = Quaternion.identity;
    }

    public void SetAlpha(float a)
    {
        if (_group != null) _group.alpha = a;
    }

    void Update()
    {
        if (_selected && ball != null)
            ball.Rotate(0f, spinSpeed * Time.unscaledDeltaTime, 0f, Space.Self);
    }
}