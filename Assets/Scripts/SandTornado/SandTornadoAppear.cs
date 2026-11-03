using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(100)]
public class SandTornadoAppear : MonoBehaviour
{
    [Header("Inicio")]
    public bool appearOnStart = true;
    public float startDelay = 0f;

    [Header("Escalonado entre tornados")]
    public bool staggerBetweenTornadoes = true;
    public float minGap = 0.3f;
    public float maxGap = 1.5f;

    [Header("Animación")]
    public float appearDuration = 3f;
    public float disappearDuration = 2.5f;
    [Range(0f, 0.9f)] public float stagger = 0.6f;
    [Range(0.01f, 1f)] public float startWidth = 0.04f;
    [Range(0f, 1f)] public float gustiness = 0.3f;
    public float gustSpeed = 2.5f;

    [Header("Vida")]
    public float lifetime = 0f;
    public bool repeat = false;
    public float hiddenTime = 5f;
    public bool destroyWhenHidden = false;

    [Header("Eventos")]
    public UnityEvent onAppeared;
    public UnityEvent onDisappeared;

    static readonly string[] ColorProps = { "_BaseColor", "_Color", "_TintColor" };

    static float nextSlotTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { nextSlotTime = 0f; }

    Transform[] tiers;
    float[] tierU;
    float[] tierA;
    bool hasMotion;

    ParticleSystem[] systems;
    int[] sysTier;
    float[] sysBaseRate;
    float[] sysOrigAlpha;
    float[] sysLastAlpha;
    ParticleSystem.Particle[] buffer;

    Renderer[] meshRenderers;
    int[] meshTier;
    int[] meshPropId;
    Color[] meshColor;
    MaterialPropertyBlock mpb;

    float p;
    float target;
    float delay;
    bool pendingAppear;
    float shownTime;
    float hiddenTimer;
    bool autoHidden;

    bool settledApplied;
    float settledP = -1f;

    public bool IsVisible { get { return target >= 1f; } }

    public void Appear() { target = 1f; shownTime = 0f; autoHidden = false; }
    public void Disappear() { target = 0f; shownTime = 0f; autoHidden = false; }
    public void Toggle() { if (target >= 1f) Disappear(); else Appear(); }

    [ContextMenu("Aparecer")] void CtxAppear() { Appear(); }
    [ContextMenu("Desaparecer")] void CtxDisappear() { Disappear(); }

    float ReserveSlot(float wait)
    {
        float now = Time.time;
        float slot = now + Mathf.Max(0f, wait);

        if (staggerBetweenTornadoes)
        {
            slot = Mathf.Max(slot, nextSlotTime);
            float lo = Mathf.Max(0f, minGap);
            float hi = Mathf.Max(lo, maxGap);
            nextSlotTime = slot + Random.Range(lo, hi);
        }

        return slot - now;
    }

    int TierOf(Transform body, Transform t)
    {
        if (body == null) return -1;
        while (t != null && t.parent != body) t = t.parent;
        return t != null ? t.GetSiblingIndex() : -1;
    }

    static float StartAlpha(ParticleSystem.MinMaxGradient g)
    {
        switch (g.mode)
        {
            case ParticleSystemGradientMode.Color: return g.color.a;
            case ParticleSystemGradientMode.TwoColors: return Mathf.Max(g.colorMin.a, g.colorMax.a);
            case ParticleSystemGradientMode.Gradient:
            case ParticleSystemGradientMode.RandomColor: return g.gradient.Evaluate(1f).a;
            case ParticleSystemGradientMode.TwoGradients: return g.gradientMax.Evaluate(1f).a;
            default: return 1f;
        }
    }

    void Awake()
    {
        Transform body = transform.Find("Body");
        if (body != null)
        {
            tiers = new Transform[body.childCount];
            for (int i = 0; i < tiers.Length; i++) tiers[i] = body.GetChild(i);
        }
        else tiers = new Transform[0];
        tierU = new float[tiers.Length];
        tierA = new float[tiers.Length];

        systems = GetComponentsInChildren<ParticleSystem>(true);
        int ns = systems.Length;
        sysTier = new int[ns];
        sysBaseRate = new float[ns];
        sysOrigAlpha = new float[ns];
        sysLastAlpha = new float[ns];
        int maxP = 1;
        for (int i = 0; i < ns; i++)
        {
            sysTier[i] = TierOf(body, systems[i].transform);
            sysBaseRate[i] = systems[i].emission.rateOverTimeMultiplier;
            sysOrigAlpha[i] = StartAlpha(systems[i].main.startColor);
            sysLastAlpha[i] = 1f;
            maxP = Mathf.Max(maxP, systems[i].main.maxParticles);
        }
        buffer = new ParticleSystem.Particle[maxP];

        mpb = new MaterialPropertyBlock();
        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        int count = 0;
        foreach (var r in all) if (!(r is ParticleSystemRenderer)) count++;
        meshRenderers = new Renderer[count];
        meshTier = new int[count];
        meshPropId = new int[count];
        meshColor = new Color[count];
        int k = 0;
        foreach (var r in all)
        {
            if (r is ParticleSystemRenderer) continue;
            meshRenderers[k] = r;
            meshTier[k] = TierOf(body, r.transform);
            meshPropId[k] = -1;
            Material m = r.sharedMaterial;
            if (m != null)
            {
                foreach (string prop in ColorProps)
                {
                    if (m.HasProperty(prop))
                    {
                        meshPropId[k] = Shader.PropertyToID(prop);
                        meshColor[k] = m.GetColor(prop);
                        break;
                    }
                }
            }
            k++;
        }
        hasMotion = GetComponent("SandTornadoMotion") != null;

        p = appearOnStart ? 0f : 1f;
        target = p;
        pendingAppear = appearOnStart;
        delay = appearOnStart ? ReserveSlot(startDelay) : startDelay;
    }

    void Start()
    {
        if (appearOnStart)
            foreach (var ps in systems) ps.Clear();
    }

    void Update()
    {
        if (pendingAppear)
        {
            delay -= Time.deltaTime;
            if (delay <= 0f) { pendingAppear = false; Appear(); }
        }

        if (p != target)
        {
            float dur = Mathf.Max(0.01f, target > p ? appearDuration : disappearDuration);
            p = Mathf.MoveTowards(p, target, Time.deltaTime / dur);
            if (p == target) Reached();
        }
        else if (target >= 1f && lifetime > 0f)
        {
            shownTime += Time.deltaTime;
            if (shownTime >= lifetime)
            {
                Disappear();
                autoHidden = true;
                hiddenTimer = 0f;
            }
        }
        else if (target <= 0f && repeat && autoHidden && !pendingAppear)
        {
            hiddenTimer += Time.deltaTime;
            if (hiddenTimer >= hiddenTime)
            {
                pendingAppear = true;
                delay = ReserveSlot(0f);
            }
        }
    }

    void Reached()
    {
        if (target >= 1f)
        {
            onAppeared.Invoke();
        }
        else
        {
            onDisappeared.Invoke();
            if (destroyWhenHidden && !repeat) Destroy(gameObject);
        }
    }

    float Alpha(float u, int seed)
    {
        float a = Mathf.SmoothStep(0f, 1f, u);
        if (gustiness > 0f)
        {
            float gust = Mathf.PerlinNoise(Time.time * gustSpeed, seed * 3.71f);
            float mid = 4f * a * (1f - a);
            a *= Mathf.Lerp(1f, gust, gustiness * mid);
        }
        return a;
    }

    void LateUpdate()
    {
        bool settled = p == target && (p >= 1f || p <= 0f);
        if (settled && settledApplied && settledP == p) return;

        int n = tiers.Length;
        float span = Mathf.Max(0.05f, 1f - stagger);

        for (int i = 0; i < n; i++)
        {
            float start = n > 1 ? stagger * i / (n - 1f) : 0f;
            float u = Mathf.Clamp01((p - start) / span);
            tierU[i] = u;
            tierA[i] = Alpha(u, i);

            if (tiers[i] == null) continue;

            float w = Mathf.Lerp(startWidth, 1f, Mathf.SmoothStep(0f, 1f, u));
            Vector3 baseScale = hasMotion ? tiers[i].localScale : Vector3.one;
            tiers[i].localScale = new Vector3(baseScale.x * w, baseScale.y, baseScale.z * w);
        }

        float globalMeshA = meshRenderers.Length > 0 ? Alpha(p, 99) : 0f;

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            Renderer r = meshRenderers[i];
            if (r == null) continue;

            int t = meshTier[i];
            float a = t >= 0 && t < n ? tierA[t] : globalMeshA;
            r.enabled = a > 0.001f;

            if (meshPropId[i] != -1)
            {
                Color c = meshColor[i];
                c.a *= a;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(meshPropId[i], c);
                r.SetPropertyBlock(mpb);
            }
        }

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null) continue;

            int t = sysTier[i];
            float a = t >= 0 && t < n ? tierA[t] : Alpha(p, 50 + i);

            var em = ps.emission;
            em.enabled = a > 0.001f || p > 0.001f;
            em.rateOverTimeMultiplier = sysBaseRate[i] * Mathf.Clamp01(a * 1.5f);

            if (a >= 0.999f && sysLastAlpha[i] >= 0.999f) continue;

            int cnt = ps.GetParticles(buffer);
            byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(sysOrigAlpha[i] * a * 255f), 0, 255);
            for (int j = 0; j < cnt; j++)
            {
                Color32 c = buffer[j].startColor;
                c.a = alpha;
                buffer[j].startColor = c;
            }
            ps.SetParticles(buffer, cnt);
            sysLastAlpha[i] = a;
        }

        settledApplied = settled;
        settledP = p;
    }
}