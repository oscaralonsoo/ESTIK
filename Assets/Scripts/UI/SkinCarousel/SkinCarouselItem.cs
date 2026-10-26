using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SkinCarouselItem : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
    static readonly int ColorId = Shader.PropertyToID("_Color");     // Built-in

    [Header("Refs")]
    [SerializeField] Transform ball;
    [SerializeField] Renderer ballRenderer;
    [SerializeField] GameObject lockIcon;

    [Header("Feel")]
    [SerializeField] float spinSpeed = 40f;

    public RectTransform Rect { get; private set; }
    public int Index { get; private set; }

    MaterialPropertyBlock _mpb;
    int _colorId;
    Color _baseColor = Color.white;
    Vector3 _baseBallScale = Vector3.one;
    bool _selected;
    bool _valid;
    float _lastBrightness = -1f;

    void Awake()
    {
        Rect = (RectTransform)transform;
        _mpb = new MaterialPropertyBlock();

        if (ballRenderer == null) ballRenderer = GetComponentInChildren<Renderer>(true);
        if (ball == null && ballRenderer != null) ball = ballRenderer.transform;

        _valid = ball != null && ballRenderer != null;
        if (!_valid)
        {
            Debug.LogError("SkinCarouselItem: el prefab necesita una bola 3D hija con MeshRenderer", this);
            return;
        }
        _baseBallScale = ball.localScale;
    }

    public void Init(SkinData data, int index)
    {
        Index = index;
        if (!_valid) return;

        if (data.ballMaterial != null)
        {
            ballRenderer.sharedMaterial = data.ballMaterial;

            var m = data.ballMaterial;
            if (m.HasProperty(BaseColorId)) { _colorId = BaseColorId; _baseColor = m.GetColor(BaseColorId); }
            else if (m.HasProperty(ColorId)) { _colorId = ColorId; _baseColor = m.GetColor(ColorId); }
            else _colorId = 0;
        }
        else
            Debug.LogWarning($"Skin '{data.id}' no tiene Ball Material asignado", data);

        if (lockIcon != null)
            lockIcon.SetActive(!SkinService.IsUnlocked(data));
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (!selected && _valid)
            ball.localRotation = Quaternion.identity;
    }

    /// Escala la bola 3D directamente (1 = tamaño del prefab)
    public void SetScale(float s)
    {
        if (!_valid) return;
        ball.localScale = _baseBallScale * s;
    }

    /// brightness: 1 = color original, 0 = negro
    public void SetVisual(float brightness, bool visible)
    {
        if (!_valid) return;

        if (ballRenderer.enabled != visible) ballRenderer.enabled = visible;
        if (!visible || _colorId == 0 || Mathf.Approximately(brightness, _lastBrightness)) return;
        _lastBrightness = brightness;

        var c = _baseColor * brightness;
        c.a = _baseColor.a;
        ballRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorId, c);
        ballRenderer.SetPropertyBlock(_mpb);
    }

    void Update()
    {
        if (_valid && _selected)
            ball.Rotate(0f, spinSpeed * Time.unscaledDeltaTime, 0f, Space.Self);
    }
}