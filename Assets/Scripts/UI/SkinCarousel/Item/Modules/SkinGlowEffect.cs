using System;
using UnityEngine;

[Serializable]
public class SkinGlowEffect
{
    static Sprite _sprite;

    [Header("Glow Anim")]
    [SerializeField] Color color = new Color(1f, 0.92f, 0.6f, 1f);
    [SerializeField] float size = 2.5f;
    [SerializeField] float grow = 0.35f;
    [SerializeField] float duration = 1.2f;

    SpriteRenderer _renderer;
    float _time = -1f;
    float _k;
    float _intensity;

    public void Init(Transform ball, Renderer ballRenderer)
    {
        var go = new GameObject("UnlockGlow");
        go.transform.SetParent(ball.parent, false);
        go.transform.localPosition = ball.localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.layer = ball.gameObject.layer;

        _renderer = go.AddComponent<SpriteRenderer>();
        _renderer.sprite = GetSprite();
        _renderer.sortingLayerID = ballRenderer.sortingLayerID;
        _renderer.sortingOrder = ballRenderer.sortingOrder - 1;
        _renderer.enabled = false;
    }

    public void Play() => _time = 0f;

    public void Reset()
    {
        _time = -1f;
        _k = 0f;
        _intensity = 0f;

        if (_renderer != null)
            _renderer.enabled = false;
    }

    public void Tick(float dt)
    {
        if (_time < 0f) return;

        _time += dt;
        _k = Mathf.Clamp01(_time / Mathf.Max(0.01f, duration));
        _intensity = Mathf.Clamp01(_k / 0.08f) * (1f - Mathf.SmoothStep(0f, 1f, _k));

        if (_k >= 1f)
        {
            _time = -1f;
            _intensity = 0f;
            _k = 0f;
        }
    }

    /// <param name="baseDiameter">Diámetro actual de la bola (escala base * escala del carrusel).</param>
    public void Apply(float baseDiameter, bool visible)
    {
        if (_renderer == null) return;

        bool show = _intensity > 0.001f && visible;
        if (_renderer.enabled != show)
            _renderer.enabled = show;

        if (!show) return;

        _renderer.transform.localScale = Vector3.one * baseDiameter * size * (1f + grow * _k);

        Color c = color;
        c.a = color.a * _intensity;
        _renderer.color = c;
    }

    static Sprite GetSprite()
    {
        if (_sprite != null) return _sprite;

        const int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float r = res * 0.5f;
        var center = new Vector2(r, r);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / r;
                float a = Mathf.Pow(1f - Mathf.Clamp01(d), 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        _sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        return _sprite;
    }
}
