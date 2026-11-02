using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MMC = UnityEngine.ParticleSystem.MinMaxCurve;
using MMG = UnityEngine.ParticleSystem.MinMaxGradient;

/// <summary>
/// Tormenta de arena estilo toon, creada 100% por código.
/// Añade este script a cualquier GameObject. Sigue a "followTarget" (o a la cámara principal).
///
/// 3 sistemas de partículas:
///  - Clouds : nubes de arena grandes, borde duro y 2 tonos (cel shading) con atlas procedural.
///  - Streaks: rayas de viento estiradas.
///  - Grit   : granos pequeños y rápidos.
///
/// Los materiales y texturas se generan por código (sin assets externos).
/// Funciona en URP (Particles/Unlit) y con fallback a Sprites/Default.
/// </summary>
[DisallowMultipleComponent]
public class ToonSandstorm : MonoBehaviour
{
    [Header("VIENTO")]
    [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0.15f);
    [SerializeField] private float windSpeed = 22f;
    [SerializeField] private float turbulence = 3f;

    [Header("AREA")]
    [Tooltip("Si está vacío usa la cámara principal.")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float width = 45f;
    [SerializeField] private float height = 14f;
    [SerializeField] private float travelDistance = 50f;
    [SerializeField] private float heightOffset = -1f;

    [Header("INTENSIDAD")]
    [SerializeField, Range(0f, 2f)] private float intensity = 1f;
    [SerializeField] private float cloudRate = 7f;
    [SerializeField] private float streakRate = 45f;
    [SerializeField] private float gritRate = 90f;

    [Header("TAMAÑO")]
    [Tooltip("Multiplica el tamaño de nubes, rayas y granos. 0.3-0.5 suele ir bien para un juego pequeño.")]
    [SerializeField, Range(0.05f, 3f)] private float sizeMultiplier = 0.4f;

    [Header("COLORES")]
    [SerializeField] private Color sandLight = new Color(0.96f, 0.78f, 0.45f, 0.85f);
    [SerializeField] private Color sandMid = new Color(0.88f, 0.64f, 0.33f, 0.85f);
    [SerializeField] private Color streakColor = new Color(1f, 0.93f, 0.72f, 0.7f);
    [SerializeField] private Color gritColor = new Color(0.78f, 0.55f, 0.27f, 1f);

    private Transform root;
    private ParticleSystem cloudPS, streakPS, gritPS;
    private readonly List<Object> created = new List<Object>();
    private Transform resolvedTarget;
    private bool started;

    private void Awake() => Build();
    private void OnDestroy() => Cleanup();

    private void LateUpdate()
    {
        if (root == null) return;

        if (resolvedTarget == null) resolvedTarget = ResolveTarget();
        if (resolvedTarget == null) return;

        // El emisor está "a barlovento" del objetivo y las partículas lo cruzan.
        root.SetPositionAndRotation(resolvedTarget.position, Quaternion.LookRotation(GetWind()));

        // Empezamos a emitir SOLO cuando el emisor ya está colocado
        // (si no, el prewarm se haría en el origen del mundo y no verías nada).
        if (!started)
        {
            started = true;
            cloudPS.Play();
            streakPS.Play();
            gritPS.Play();
            Debug.Log($"[ToonSandstorm] Siguiendo a '{resolvedTarget.name}' en {resolvedTarget.position} | shader: {cloudPS.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name}");
        }
    }

    private Transform ResolveTarget()
    {
        if (followTarget != null) return followTarget;

        var ball = FindAnyObjectByType<BallController>();
        if (ball != null) return ball.transform;

        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    // ????????????????????????? API pública ?????????????????????????

    /// <summary>Cambia la densidad en caliente (0 = sin tormenta, 2 = tormenta fuerte).</summary>
    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp(value, 0f, 2f);
        SetRate(cloudPS, cloudRate);
        SetRate(streakPS, streakRate);
        SetRate(gritPS, gritRate);
    }

    /// <summary>Reconstruye todo (úsalo si cambias viento, tamaños o colores en runtime).</summary>
    [ContextMenu("Rebuild")]
    public void Rebuild() => Build();

    // ????????????????????????? Construcción ?????????????????????????

    private void Build()
    {
        Cleanup();

        root = new GameObject("ToonSandstorm_Root").transform;
        root.SetParent(transform, false);
        created.Add(root.gameObject);

        BuildClouds();
        BuildStreaks();
        BuildGrit();

        started = false; // el Play se hace en LateUpdate cuando hay objetivo
    }

    private void BuildClouds()
    {
        Texture2D tex = MakeBlobAtlas(128);
        Material mat = MakeMaterial(tex, "ToonSand_Cloud");
        cloudPS = CreateSystem("Clouds", mat, out ParticleSystemRenderer r);
        r.sortMode = ParticleSystemSortMode.Distance;

        float speed = windSpeed * 0.85f;
        float life = travelDistance / speed;

        var main = cloudPS.main;
        ApplyCommon(ref main, 300);
        main.startLifetime = new MMC(life * 0.95f, life * 1.05f);
        main.startSpeed = new MMC(speed * 0.9f, speed * 1.1f);
        main.startSize = new MMC(5f * sizeMultiplier, 11f * sizeMultiplier);
        main.startRotation = new MMC(-0.25f, 0.25f); // poco giro: así el sombreado siempre queda abajo
        main.startColor = new MMG(sandLight, sandMid);

        SetupEmission(cloudPS, cloudRate);
        SetupShape(cloudPS);
        SetupNoise(cloudPS, turbulence * 0.6f, 0.12f);
        SetupSizePop(cloudPS);

        // Variedad: elige al azar uno de los 4 blobs del atlas
        var tsa = cloudPS.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Grid;
        tsa.numTilesX = 2;
        tsa.numTilesY = 2;
        tsa.animation = ParticleSystemAnimationType.WholeSheet;
        tsa.frameOverTime = new MMC(0f);
        tsa.startFrame = new MMC(0f, 0.99f);
        tsa.cycleCount = 1;
    }

    private void BuildStreaks()
    {
        Texture2D tex = MakeShapeTexture(64, 3f);
        Material mat = MakeMaterial(tex, "ToonSand_Streak");
        streakPS = CreateSystem("Streaks", mat, out ParticleSystemRenderer r);
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 4f;
        r.velocityScale = 0.06f * sizeMultiplier;
        r.cameraVelocityScale = 0f;

        float speed = windSpeed * 1.5f;
        float life = travelDistance / speed;

        var main = streakPS.main;
        ApplyCommon(ref main, 400);
        main.startLifetime = new MMC(life * 0.95f, life * 1.05f);
        main.startSpeed = new MMC(speed * 0.9f, speed * 1.1f);
        main.startSize = new MMC(0.06f * sizeMultiplier, 0.16f * sizeMultiplier);
        main.startColor = new MMG(streakColor);

        SetupEmission(streakPS, streakRate);
        SetupShape(streakPS);
        SetupNoise(streakPS, turbulence * 0.4f, 0.25f);
        SetupSizePop(streakPS);
    }

    private void BuildGrit()
    {
        Texture2D tex = MakeShapeTexture(32, 2.2f);
        Material mat = MakeMaterial(tex, "ToonSand_Grit");
        gritPS = CreateSystem("Grit", mat, out ParticleSystemRenderer r);

        float speed = windSpeed * 1.2f;
        float life = travelDistance / speed;

        var main = gritPS.main;
        ApplyCommon(ref main, 600);
        main.startLifetime = new MMC(life * 0.95f, life * 1.05f);
        main.startSpeed = new MMC(speed * 0.8f, speed * 1.2f);
        main.startSize = new MMC(0.06f * sizeMultiplier, 0.18f * sizeMultiplier);
        main.startRotation = new MMC(0f, Mathf.PI * 2f);
        main.startColor = new MMG(gritColor, sandMid);

        SetupEmission(gritPS, gritRate);
        SetupShape(gritPS);
        SetupNoise(gritPS, turbulence, 0.4f);
        SetupSizePop(gritPS);
    }

    // ????????????????????????? Helpers de partículas ?????????????????????????

    private ParticleSystem CreateSystem(string name, Material mat, out ParticleSystemRenderer renderer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // hay que parar antes de configurar

        renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return ps;
    }

    private void ApplyCommon(ref ParticleSystem.MainModule main, int maxParticles)
    {
        main.loop = true;
        main.playOnAwake = false;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.maxParticles = maxParticles;
    }

    private void SetupEmission(ParticleSystem ps, float baseRate)
    {
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = baseRate * intensity;
    }

    private void SetRate(ParticleSystem ps, float baseRate)
    {
        if (ps == null) return;
        var em = ps.emission;
        em.rateOverTime = baseRate * intensity;
    }

    private void SetupShape(ParticleSystem ps)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Box;
        // Plano fino a barlovento; las partículas viajan hacia +Z (la dirección del viento)
        sh.position = new Vector3(0f, height * 0.5f + heightOffset, -travelDistance * 0.5f);
        sh.scale = new Vector3(width, height, 0.01f);
        sh.randomDirectionAmount = 0.03f;
    }

    private void SetupNoise(ParticleSystem ps, float strength, float frequency)
    {
        var n = ps.noise;
        n.enabled = strength > 0f;
        n.strength = new MMC(strength);
        n.frequency = frequency;
        n.scrollSpeed = 0.6f;
        n.damping = true;
        n.octaveCount = 1;
        n.quality = ParticleSystemNoiseQuality.Low;
    }

    // Aparece / desaparece escalando (más "toon" que un fade de alpha).
    private void SetupSizePop(ParticleSystem ps)
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.85f, 1f),
            new Keyframe(1f, 0f));

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new MMC(1f, curve);
    }

    private Vector3 GetWind()
    {
        Vector3 w = windDirection;
        if (w.sqrMagnitude < 0.0001f) w = Vector3.right;
        return w.normalized;
    }

    // ????????????????????????? Texturas procedurales ?????????????????????????

    /// <summary>Atlas 2x2 de nubes con borde duro y 2 tonos (luz arriba / sombra abajo).</summary>
    private Texture2D MakeBlobAtlas(int cell)
    {
        const int tiles = 2;
        int size = cell * tiles;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "ToonSand_BlobAtlas" };
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];

        for (int ty = 0; ty < tiles; ty++)
        {
            for (int tx = 0; tx < tiles; tx++)
            {
                float seed = (ty * tiles + tx) * 2.37f + 1f;

                for (int y = 0; y < cell; y++)
                {
                    for (int x = 0; x < cell; x++)
                    {
                        float u = (x + 0.5f) / cell * 2f - 1f;
                        float v = (y + 0.5f) / cell * 2f - 1f;
                        float d = Mathf.Sqrt(u * u + v * v);
                        float a = Mathf.Atan2(v, u);

                        // Contorno con lóbulos (silueta de nube)
                        float r = 0.58f
                                + 0.13f * Mathf.Sin(3f * a + seed * 1.7f)
                                + 0.08f * Mathf.Sin(5f * a + seed * 3.1f)
                                + 0.05f * Mathf.Sin(8f * a + seed * 5.3f);

                        float alpha = Mathf.Clamp01((r - d) * cell * 0.5f); // ~1px de antialias

                        // Cel shading: corte duro ondulado. Abajo = tono oscuro.
                        float cut = -0.10f + 0.12f * Mathf.Sin(u * 4f + seed);
                        float c = v < cut ? 0.72f : 1f;

                        byte cb = (byte)(c * 255f);
                        px[(ty * cell + y) * size + (tx * cell + x)] = new Color32(cb, cb, cb, (byte)(alpha * 255f));
                    }
                }
            }
        }

        tex.SetPixels32(px);
        tex.Apply(true);
        created.Add(tex);
        return tex;
    }

    /// <summary>Forma rellena de borde duro. n=2 círculo, n&gt;2 cápsula/cuadrado redondeado.</summary>
    private Texture2D MakeShapeTexture(int size, float n)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "ToonSand_Shape" };
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), n) + Mathf.Pow(Mathf.Abs(v), n), 1f / n);
                float alpha = Mathf.Clamp01((0.95f - d) * size * 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(true);
        created.Add(tex);
        return tex;
    }

    // ????????????????????????? Material ?????????????????????????

    private Material MakeMaterial(Texture2D tex, string name)
    {
        // OJO en build: añade "Universal Render Pipeline/Particles/Unlit" a
        // Project Settings > Graphics > Always Included Shaders, o Shader.Find devolverá null.
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        var mat = new Material(sh) { name = name };

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

        if (mat.HasProperty("_Surface")) // URP: forzar superficie transparente
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        created.Add(mat);
        return mat;
    }

    // ????????????????????????? Limpieza ?????????????????????????

    // Selecciona el objeto en Play y verás el área de emisión (amarillo) y el recorrido (cian).
    private void OnDrawGizmosSelected()
    {
        if (root == null) return;
        Vector3 c = new Vector3(0f, height * 0.5f + heightOffset, -travelDistance * 0.5f);
        Gizmos.matrix = root.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(c, new Vector3(width, height, 0.1f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(c, c + new Vector3(0f, 0f, travelDistance));
    }

    private void Cleanup()
    {
        started = false;
        foreach (var o in created)
        {
            if (o == null) continue;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
        created.Clear();
        root = null;
        cloudPS = streakPS = gritPS = null;
    }
}