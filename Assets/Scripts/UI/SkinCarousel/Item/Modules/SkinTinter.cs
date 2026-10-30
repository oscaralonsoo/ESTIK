using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkinTinter
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    struct Tint
    {
        public Renderer renderer;
        public int colorId;
        public Color baseColor;
    }

    [Header("Lock")]
    [SerializeField, Range(0f, 1f)] float lockedBrightness = 0.15f;

    readonly List<Tint> _tints = new List<Tint>();
    MaterialPropertyBlock _mpb;
    float _lastApplied = -1f;
    public float Darkness { get; set; }

    public float Flash { get; set; }
    public float Brightness { get; set; } = 1f;

    public void Build(IEnumerable<Renderer> renderers)
    {
        _tints.Clear();
        _lastApplied = -1f;

        foreach (var r in renderers)
            Add(r);
    }
    public void ApplyIfChanged()
    {
        if (_tints.Count == 0 || Mathf.Approximately(Brightness, _lastApplied))
            return;

        _lastApplied = Brightness;
        Apply();
    }

    public void Refresh()
    {
        _lastApplied = -1f;
        Apply();
    }

    public void Apply()
    {
        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();

        float finalBrightness = Brightness * Mathf.Lerp(1f, lockedBrightness, Darkness);

        foreach (var tint in _tints)
        {
            if (tint.renderer == null) continue;

            Color c = tint.baseColor * finalBrightness;
            if (Flash > 0f)
                c = Color.Lerp(c, Color.white, Flash);
            c.a = tint.baseColor.a;

            tint.renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(tint.colorId, c);
            tint.renderer.SetPropertyBlock(_mpb);
        }
    }

    void Add(Renderer r)
    {
        if (r == null) return;

        Material m = r.sharedMaterial;
        if (m == null) return;

        int id;
        if (m.HasProperty(BaseColorId)) id = BaseColorId;
        else if (m.HasProperty(ColorId)) id = ColorId;
        else return;

        _tints.Add(new Tint
        {
            renderer = r,
            colorId = id,
            baseColor = m.GetColor(id)
        });
    }
}
