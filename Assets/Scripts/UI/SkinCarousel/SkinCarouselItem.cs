using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SkinCarouselItem : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    struct Tint
    {
        public Renderer renderer;
        public int colorId;
        public Color baseColor;
    }

    [Header("Refs")]
    [SerializeField] Transform ball;
    [SerializeField] Renderer ballRenderer;

    [Header("Lock")]
    [SerializeField] GameObject lockIcon;
    [SerializeField, Range(0f, 1f)] float lockedBrightness = 0.15f;

    [Header("Feel")]
    [SerializeField] float spinSpeed = 40f;

    [Header("Prefab visual")]
    [SerializeField] float prefabScaleMultiplier = 1f;
    [SerializeField] string[] ignoreChildPrefixes = { "Trail" };

    [Header("Sorting")]
    [SerializeField] int particlesSortingOrder = 10;
    [SerializeField] int shellSortingOrder = 11;

    public RectTransform Rect { get; private set; }
    public int Index { get; private set; }

    readonly List<Tint> _tints = new List<Tint>();
    MaterialPropertyBlock _mpb;
    Vector3 _baseBallScale = Vector3.one;
    Vector3 _lockBaseScale = Vector3.one;
    float _lastBrightness = -1f;
    float _currentBrightness = 1f;

    GameObject _visualInstance;
    Renderer[] _visualRenderers;
    ParticleSystem[] _particles;

    bool _valid;
    bool _selected;
    bool _usePrefabVisual;
    bool _visualShown = true;
    bool _locked;

    void Awake()
    {
        Rect = (RectTransform)transform;
        _mpb = new MaterialPropertyBlock();

        if (ballRenderer == null)
            ballRenderer = GetComponentInChildren<Renderer>(true);
        if (ball == null && ballRenderer != null)
            ball = ballRenderer.transform;

        _valid = ball != null && ballRenderer != null;
        if (!_valid)
        {
            Debug.LogError("SkinCarouselItem: el prefab necesita una bola 3D hija con MeshRenderer", this);
            return;
        }

        _baseBallScale = ball.localScale;

        if (lockIcon != null)
            _lockBaseScale = lockIcon.transform.localScale;
    }

    void Update()
    {
        if (_valid && _selected)
            ball.Rotate(0f, spinSpeed * Time.unscaledDeltaTime, 0f, Space.Self);
    }

    public void Init(SkinData data, int index)
    {
        Index = index;
        if (!_valid) return;

        ApplyBallMaterial(data);
        SpawnPrefabVisual(data);
        BuildTints();
        UpdateLockState(data);
    }

    public void UpdateLockState(SkinData data)
    {
        _locked = !SkinService.IsUnlocked(data);

        if (lockIcon != null)
            lockIcon.SetActive(_locked && _visualShown);

        // Las particulas de una skin bloqueada se pausan para que no "brille" sin comprarla
        SetParticlesActive(!_locked);

        // Forzar que se vuelva a aplicar el tinte (oscuro si esta bloqueada)
        _lastBrightness = -1f;
        ApplyBrightness(_currentBrightness);
    }

    /// <summary>
    /// Devuelve la posición mundial de la bola para que el PurchaseCollector sepa hacia dónde volar.
    /// </summary>
    public Vector3 GetBallPosition()
    {
        if (ball != null)
            return ball.position;
        return transform.position;
    }

    void ApplyBallMaterial(SkinData data)
    {
        if (data.ballMaterial != null)
            ballRenderer.sharedMaterial = data.ballMaterial;
        else
            Debug.LogWarning($"Skin '{data.id}' no tiene Ball Material asignado", data);
    }

    void SpawnPrefabVisual(SkinData data)
    {
        DestroyVisual();

        GameObject source = data.ballPrefab != null ? data.ballPrefab : data.effectPrefab;
        if (source == null) return;

        bool wholePrefab = data.ballPrefab != null;

        _visualInstance = Instantiate(source, ball);
        _visualInstance.name = "Visual_" + data.id;

        Transform t = _visualInstance.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        DisableGameplay(_visualInstance);
        HideIgnoredChildren(t);
        SetLayerRecursive(t, ball.gameObject.layer);

        if (wholePrefab)
        {
            t.localScale = Vector3.one * FitScaleToBall(t);
            _usePrefabVisual = true;
            ballRenderer.enabled = false;
        }
        else
        {
            t.localScale = Vector3.one * prefabScaleMultiplier;
        }

        SetupParticles(_visualInstance);
        _particles = _visualInstance.GetComponentsInChildren<ParticleSystem>(true);

        _visualRenderers = _visualInstance.GetComponentsInChildren<Renderer>(true);
        SetupSorting(_visualRenderers);
    }

    void DestroyVisual()
    {
        if (_visualInstance != null)
            Destroy(_visualInstance);

        _visualInstance = null;
        _visualRenderers = null;
        _particles = null;
        _usePrefabVisual = false;
    }

    static void DisableGameplay(GameObject root)
    {
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            mb.enabled = false;

        foreach (var c in root.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        foreach (var tr in root.GetComponentsInChildren<TrailRenderer>(true))
            tr.gameObject.SetActive(false);
    }

    void HideIgnoredChildren(Transform root)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && IsIgnored(child.name))
                child.gameObject.SetActive(false);
        }
    }

    bool IsIgnored(string childName)
    {
        foreach (var prefix in ignoreChildPrefixes)
        {
            if (!string.IsNullOrEmpty(prefix) && childName.StartsWith(prefix))
                return true;
        }
        return false;
    }

    static void SetLayerRecursive(Transform root, int layer)
    {
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            tr.gameObject.layer = layer;
    }

    float FitScaleToBall(Transform visualRoot)
    {
        Bounds? meshBounds = null;

        foreach (var mr in visualRoot.GetComponentsInChildren<MeshRenderer>(false))
        {
            if (meshBounds == null)
            {
                meshBounds = mr.bounds;
            }
            else
            {
                var b = meshBounds.Value;
                b.Encapsulate(mr.bounds);
                meshBounds = b;
            }
        }

        if (meshBounds == null || meshBounds.Value.size.x <= 0.0001f)
            return prefabScaleMultiplier;

        float ratio = ballRenderer.bounds.size.x / meshBounds.Value.size.x;
        return ratio * prefabScaleMultiplier;
    }

    static void SetupParticles(GameObject root)
    {
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ps.Clear(true);
            ps.Play(true);
        }
    }

    void SetParticlesActive(bool active)
    {
        if (_particles == null) return;

        foreach (var ps in _particles)
        {
            if (ps == null) continue;

            if (active)
            {
                if (!ps.isPlaying) ps.Play(true);
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    void SetupSorting(Renderer[] renderers)
    {
        foreach (var r in renderers)
        {
            r.sortingLayerID = ballRenderer.sortingLayerID;

            if (r is ParticleSystemRenderer psr)
            {
                psr.sortingOrder = particlesSortingOrder;
                psr.sortingFudge = 0f;
            }
            else if (r is MeshRenderer)
            {
                r.sortingOrder = shellSortingOrder;
            }
        }
    }

    void BuildTints()
    {
        _tints.Clear();
        _lastBrightness = -1f;

        if (!_usePrefabVisual)
        {
            AddTint(ballRenderer);
            return;
        }

        foreach (var mr in _visualInstance.GetComponentsInChildren<MeshRenderer>(false))
            AddTint(mr);
    }

    void AddTint(Renderer r)
    {
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

    void ApplyBrightness(float brightness)
    {
        float finalBrightness = _locked ? brightness * lockedBrightness : brightness;

        foreach (var tint in _tints)
        {
            if (tint.renderer == null) continue;

            Color c = tint.baseColor * finalBrightness;
            c.a = tint.baseColor.a;

            tint.renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(tint.colorId, c);
            tint.renderer.SetPropertyBlock(_mpb);
        }
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (!selected && _valid)
            ball.localRotation = Quaternion.identity;
    }

    public void SetScale(float s)
    {
        if (!_valid) return;
        ball.localScale = _baseBallScale * s;

        if (lockIcon != null)
            lockIcon.transform.localScale = _lockBaseScale * s;
    }

    public void SetVisual(float brightness, bool visible)
    {
        if (!_valid) return;

        _currentBrightness = brightness;

        bool ballShouldShow = visible && !_usePrefabVisual;
        if (ballRenderer.enabled != ballShouldShow)
            ballRenderer.enabled = ballShouldShow;

        SetVisualShown(visible);

        if (lockIcon != null)
        {
            bool lockShouldShow = _locked && visible;
            if (lockIcon.activeSelf != lockShouldShow)
                lockIcon.SetActive(lockShouldShow);
        }

        if (!visible || _tints.Count == 0 || Mathf.Approximately(brightness, _lastBrightness))
            return;

        _lastBrightness = brightness;
        ApplyBrightness(brightness);
    }

    void SetVisualShown(bool visible)
    {
        if (_visualRenderers == null || _visualShown == visible) return;
        _visualShown = visible;

        foreach (var r in _visualRenderers)
            if (r != null) r.enabled = visible;
    }
}