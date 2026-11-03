using UnityEngine;

public class Spark : MonoBehaviour
{
    [Header("AREA")]
    [SerializeField] private Vector3 areaSize = new Vector3(30f, 1f, 30f);
    [Tooltip("Si lo asignas, las partículas siguen a este objeto (ej. la cámara o el jugador).")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = Vector3.zero;

    [Header("ASH (CENIZA)")]
    [SerializeField] private float ashHeight = 8f;
    [SerializeField] private float ashRate = 15f;
    [SerializeField] private Vector2 ashSize = new Vector2(0.15f, 0.4f);
    [SerializeField] private Color ashColorA = new Color(0.25f, 0.22f, 0.22f);
    [SerializeField] private Color ashColorB = new Color(0.5f, 0.45f, 0.45f);
    [SerializeField] private Vector3 wind = new Vector3(0.6f, 0f, 0f);

    [Header("SPARKS (CHISPAS)")]
    [SerializeField] private float sparkHeight = 0f;
    [SerializeField] private float sparkRate = 12f;
    [SerializeField] private Vector2 sparkSize = new Vector2(0.08f, 0.22f);
    [SerializeField] private Color sparkStart = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color sparkMid = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color sparkEnd = new Color(0.8f, 0.1f, 0.05f);

    private ParticleSystem ash;
    private ParticleSystem sparks;
    private Material ashMat;
    private Material sparkMat;

    private void Awake()
    {
        ash = CreateAsh();
        sparks = CreateSparks();
        ash.Play();
        sparks.Play();
    }

    private void LateUpdate()
    {
        if (followTarget != null)
            transform.position = followTarget.position + followOffset;
    }

    // ????????????????????????? CENIZA ?????????????????????????

    private ParticleSystem CreateAsh()
    {
        ParticleSystem ps = CreateBase("Ash");
        ashMat = CreateMaterial(null); // sin textura = copos cuadrados
        ps.GetComponent<ParticleSystemRenderer>().material = ashMat;

        var main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(ashSize.x, ashSize.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ashColorA, ashColorB);
        main.gravityModifier = 0.03f;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = ashRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;
        shape.position = new Vector3(0f, ashHeight, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = wind.x;
        velocity.y = wind.y;
        velocity.z = wind.z;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.3f;
        noise.quality = ParticleSystemNoiseQuality.Low;

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.15f, 1f),
            new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)));

        return ps;
    }

    // ????????????????????????? CHISPAS ?????????????????????????

    private ParticleSystem CreateSparks()
    {
        ParticleSystem ps = CreateBase("Sparks");
        sparkMat = CreateMaterial(CreateCircleTexture(32));
        ps.GetComponent<ParticleSystemRenderer>().material = sparkMat;

        var main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(sparkSize.x, sparkSize.y);
        main.gravityModifier = -0.12f; // negativa = suben
        main.maxParticles = 150;

        var emission = ps.emission;
        emission.rateOverTime = sparkRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;
        shape.position = new Vector3(0f, sparkHeight, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.2f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.6f;
        noise.quality = ParticleSystemNoiseQuality.Low;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(sparkStart, 0f),
                new GradientColorKey(sparkMid, 0.4f),
                new GradientColorKey(sparkEnd, 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        return ps;
    }

    // ????????????????????????? UTILIDADES ?????????????????????????

    private ParticleSystem CreateBase(string objName)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // para poder cambiar duration

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    private Material CreateMaterial(Texture2D tex)
    {
        // Sprites/Default funciona en Built-in y en URP
        Material mat = new Material(Shader.Find("Sprites/Default"));
        if (tex != null) mat.mainTexture = tex;
        return mat;
    }

    private Texture2D CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01((r - 1f - d) + 1f); // borde duro, estilo cartoon
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (ashMat != null) Destroy(ashMat);
        if (sparkMat != null)
        {
            if (sparkMat.mainTexture != null) Destroy(sparkMat.mainTexture);
            Destroy(sparkMat);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * ashHeight, areaSize);
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * sparkHeight, areaSize);
    }
}