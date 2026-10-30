using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona el visual 3D de la skin: instancia el prefab, desactiva su gameplay,
/// ajusta escala / layers / sorting y controla partículas y visibilidad.
/// </summary>
[Serializable]
public class SkinVisual
{
    [Header("Prefab visual")]
    [SerializeField] float prefabScaleMultiplier = 1f;
    [SerializeField] string[] ignoreChildPrefixes = { "Trail" };

    [Header("Sorting")]
    [SerializeField] int particlesSortingOrder = 10;
    [SerializeField] int shellSortingOrder = 11;

    Transform _ball;
    Renderer _ballRenderer;

    GameObject _instance;
    Renderer[] _renderers;
    ParticleSystem[] _particles;

    /// <summary>True si se usa un prefab completo en lugar de la malla de la bola.</summary>
    public bool UsesPrefab { get; private set; }

    /// <summary>True si el visual está visible (lo dicta el carrusel).</summary>
    public bool Shown { get; private set; } = true;

    public void Init(Transform ball, Renderer ballRenderer)
    {
        _ball = ball;
        _ballRenderer = ballRenderer;
    }

    // ───────────── Spawn / Clear ─────────────

    public void Spawn(SkinData data)
    {
        Clear();

        GameObject source = data.ballPrefab != null ? data.ballPrefab : data.effectPrefab;
        if (source == null) return;

        bool wholePrefab = data.ballPrefab != null;

        _instance = UnityEngine.Object.Instantiate(source, _ball);
        _instance.name = "Visual_" + data.id;

        Transform t = _instance.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        DisableGameplay(_instance);
        HideIgnoredChildren(t);
        SetLayerRecursive(t, _ball.gameObject.layer);

        if (wholePrefab)
        {
            t.localScale = Vector3.one * FitScaleToBall(t);
            UsesPrefab = true;
            _ballRenderer.enabled = false;
        }
        else
        {
            t.localScale = Vector3.one * prefabScaleMultiplier;
        }

        SetupParticles(_instance);
        _particles = _instance.GetComponentsInChildren<ParticleSystem>(true);

        _renderers = _instance.GetComponentsInChildren<Renderer>(true);
        SetupSorting(_renderers);
    }

    public void Clear()
    {
        if (_instance != null)
            UnityEngine.Object.Destroy(_instance);

        _instance = null;
        _renderers = null;
        _particles = null;
        UsesPrefab = false;
    }

    // ───────────── Tint ─────────────

    /// <summary>Renderers sobre los que se aplica el brillo.</summary>
    public IEnumerable<Renderer> GetTintRenderers()
    {
        if (!UsesPrefab || _instance == null)
        {
            yield return _ballRenderer;
            yield break;
        }

        foreach (var mr in _instance.GetComponentsInChildren<MeshRenderer>(false))
            yield return mr;
    }

    // ───────────── Runtime ─────────────

    public void SetParticlesActive(bool active)
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

    public void SetShown(bool visible)
    {
        if (_renderers == null || Shown == visible) return;
        Shown = visible;

        foreach (var r in _renderers)
            if (r != null) r.enabled = visible;
    }

    // ───────────── Helpers ─────────────

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
        if (ignoreChildPrefixes == null) return false;

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

        float ratio = _ballRenderer.bounds.size.x / meshBounds.Value.size.x;
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

    void SetupSorting(Renderer[] renderers)
    {
        foreach (var r in renderers)
        {
            r.sortingLayerID = _ballRenderer.sortingLayerID;

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
}
