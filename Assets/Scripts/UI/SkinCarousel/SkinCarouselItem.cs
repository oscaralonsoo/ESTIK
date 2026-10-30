using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SkinCarouselItem : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static Sprite _glowSprite;

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

    [Header("Recive Points Anim")]
    [SerializeField] float pulseScale = 1.3f;
    [SerializeField, Range(0f, 1f)] float pulseMinStrength = 0.45f;
    [SerializeField] float pulseDecay = 12f;
    [SerializeField] float pulseMinInterval = 0.07f;
    [SerializeField] float pulseMaxInterval = 0.22f;
    [SerializeField] float receiveHold = 0.3f;

    [Header("Unlock Anim")]
    [SerializeField] float shakeDuration = 0.35f;
    [SerializeField] float shakeAngle = 18f;
    [SerializeField] float lockPopDuration = 0.25f;
    [SerializeField] float lockPopScale = 1.6f;
    [SerializeField] float revealDuration = 0.55f;
    [SerializeField] float ballPunchScale = 1.35f;

    [Header("Glow Anim")]
    [SerializeField] Color glowColor = new Color(1f, 0.92f, 0.6f, 1f);
    [SerializeField] float glowSize = 2.5f;
    [SerializeField] float glowGrow = 0.35f;
    [SerializeField] float glowDuration = 1.2f;

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
    Quaternion _lockBaseRot = Quaternion.identity;
    float _lastBrightness = -1f;
    float _currentBrightness = 1f;

    GameObject _visualInstance;
    Renderer[] _visualRenderers;
    ParticleSystem[] _particles;
    SpriteRenderer _glowRenderer;

    bool _valid;
    bool _selected;
    bool _usePrefabVisual;
    bool _visualShown = true;

    bool _locked;
    bool _unlocking;
    float _darkness;
    float _flash;

    float _glow;
    float _glowK;
    float _glowTime = -1f;

    float _unlockPunch = 1f;
    float _lockAnim = 1f;
    float _lockRot;

    float _punch;
    float _receiveTimer;
    float _nextPulseIn;

    Coroutine _unlockCo;
    Action _onUnlockDone;

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
        {
            _lockBaseScale = lockIcon.transform.localScale;
            _lockBaseRot = lockIcon.transform.localRotation;
        }

        CreateGlow();
    }
    void CreateGlow()
    {
        if (_glowSprite == null)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Pow(1f - Mathf.Clamp01(d), 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();

            _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        var go = new GameObject("UnlockGlow");
        go.transform.SetParent(ball.parent, false);
        go.transform.localPosition = ball.localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.layer = ball.gameObject.layer;

        _glowRenderer = go.AddComponent<SpriteRenderer>();
        _glowRenderer.sprite = _glowSprite;
        _glowRenderer.sortingLayerID = ballRenderer.sortingLayerID;
        _glowRenderer.sortingOrder = ballRenderer.sortingOrder - 1;
        _glowRenderer.enabled = false;
    }

    void OnDisable()
    {
        if (_unlockCo != null) { StopCoroutine(_unlockCo); _unlockCo = null; }

        _punch = 0f;
        _receiveTimer = 0f;
        _unlockPunch = 1f;
        _lockAnim = 1f;
        _lockRot = 0f;
        _flash = 0f;
        _glow = 0f;
        _glowK = 0f;
        _glowTime = -1f;
        if (_glowRenderer != null)
            _glowRenderer.enabled = false;
        if (_unlocking)
            FinishUnlock();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (_receiveTimer > 0f)
        {
            _receiveTimer -= dt;
            _nextPulseIn -= dt;

            if (_nextPulseIn <= 0f)
            {
                float peak = Mathf.Max(0f, pulseScale - 1f);
                float strength = UnityEngine.Random.Range(pulseMinStrength, 1f);
                _punch = Mathf.Max(_punch, peak * strength);
                _nextPulseIn = UnityEngine.Random.Range(pulseMinInterval, pulseMaxInterval);
            }
        }

        if (_punch > 0.0001f)
            _punch = Mathf.Lerp(_punch, 0f, 1f - Mathf.Exp(-pulseDecay * dt));
        else
            _punch = 0f;

        if (_glowTime >= 0f)
        {
            _glowTime += dt;
            _glowK = Mathf.Clamp01(_glowTime / Mathf.Max(0.01f, glowDuration));
            _glow = Mathf.Clamp01(_glowK / 0.08f) * (1f - Mathf.SmoothStep(0f, 1f, _glowK));

            if (_glowK >= 1f)
            {
                _glowTime = -1f;
                _glow = 0f;
                _glowK = 0f;
            }
        }

        if (_valid && _selected)
            ball.Rotate(0f, spinSpeed * dt, 0f, Space.Self);
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
        _darkness = _locked ? 1f : 0f;
        _flash = 0f;

        if (lockIcon != null)
            lockIcon.SetActive((_locked || _unlocking) && _visualShown);

        SetParticlesActive(!_locked);

        _lastBrightness = -1f;
        ApplyBrightness(_currentBrightness);
    }
    public void PlayReceivePulse()
    {
        if (!_valid) return;

        if (_receiveTimer <= 0f)
            _nextPulseIn = 0f;

        _receiveTimer = receiveHold;
    }
    public void PlayUnlockAnimation(Action onDone = null)
    {
        if (!_valid) { onDone?.Invoke(); return; }

        if (!isActiveAndEnabled)
        {
            _unlocking = true;
            _onUnlockDone = onDone;
            FinishUnlock();
            return;
        }

        if (_unlockCo != null) StopCoroutine(_unlockCo);
        _onUnlockDone = onDone;
        _unlockCo = StartCoroutine(UnlockRoutine());
    }

    IEnumerator UnlockRoutine()
    {
        _unlocking = true;
        _receiveTimer = 0f;
        if (lockIcon != null) lockIcon.SetActive(_visualShown);

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / shakeDuration);

            _lockRot = Mathf.Sin(k * Mathf.PI * 6f) * shakeAngle * (1f - k * 0.3f);
            _lockAnim = 1f + 0.1f * Mathf.Sin(k * Mathf.PI);
            _unlockPunch = 1f - 0.1f * Mathf.Sin(k * Mathf.PI);
            yield return null;
        }
        _locked = false;
        SetParticlesActive(true);

        _glowTime = 0f;

        t = 0f;
        float total = Mathf.Max(revealDuration, lockPopDuration);
        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            float kp = Mathf.Clamp01(t / lockPopDuration);
            _lockAnim = Mathf.Lerp(1f, lockPopScale, kp) * (1f - kp * kp);
            _lockRot = Mathf.Lerp(0f, 25f, kp);

            float kr = Mathf.Clamp01(t / revealDuration);
            _darkness = 1f - Mathf.SmoothStep(0f, 1f, kr);
            _flash = Mathf.Sin(Mathf.Clamp01(kr * 2f) * Mathf.PI * 0.5f) * (1f - kr) * 0.8f;
            _unlockPunch = 1f + (ballPunchScale - 1f) * Mathf.Sin(kr * Mathf.PI);

            if (_visualShown) ApplyBrightness(_currentBrightness);
            yield return null;
        }

        _unlockCo = null;
        FinishUnlock();
    }

    void FinishUnlock()
    {
        _unlocking = false;
        _locked = false;
        _darkness = 0f;
        _flash = 0f;
        _unlockPunch = 1f;
        _lockAnim = 1f;
        _lockRot = 0f;
        _punch = 0f;
        _receiveTimer = 0f;

        if (lockIcon != null)
        {
            lockIcon.transform.localRotation = _lockBaseRot;
            lockIcon.SetActive(false);
        }

        SetParticlesActive(true);
        _lastBrightness = -1f;
        ApplyBrightness(_currentBrightness);

        Action cb = _onUnlockDone;
        _onUnlockDone = null;
        cb?.Invoke();
    }
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
        float finalBrightness = brightness * Mathf.Lerp(1f, lockedBrightness, _darkness);

        foreach (var tint in _tints)
        {
            if (tint.renderer == null) continue;

            Color c = tint.baseColor * finalBrightness;
            if (_flash > 0f)
                c = Color.Lerp(c, Color.white, _flash);
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
        ball.localScale = _baseBallScale * s * _unlockPunch;

        if (lockIcon != null)
        {
            lockIcon.transform.localScale = _lockBaseScale * s * (1f + _punch) * _lockAnim;
            lockIcon.transform.localRotation = _lockBaseRot * Quaternion.Euler(0f, 0f, _lockRot);
        }

        if (_glowRenderer != null)
        {
            bool showGlow = _glow > 0.001f && _visualShown;
            if (_glowRenderer.enabled != showGlow)
                _glowRenderer.enabled = showGlow;

            if (showGlow)
            {
                float diameter = _baseBallScale.x * s;
                _glowRenderer.transform.localScale = Vector3.one * diameter * glowSize * (1f + glowGrow * _glowK);

                Color c = glowColor;
                c.a = glowColor.a * _glow;
                _glowRenderer.color = c;
            }
        }
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
            bool lockShouldShow = (_locked || _unlocking) && visible;
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