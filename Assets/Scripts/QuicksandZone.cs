using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Arenas movedizas basadas en MudZone.
/// - Frena igual que MudZone (drag, angularDrag, maxSpeed) y solo actua cuando el CENTRO de la
///   bola esta dentro de la zona (en horizontal), asi coincide con lo que se dibuja.
/// - NO toca la altura, la gravedad ni las colisiones de la bola: rueda por el suelo normal y
///   al salir de la zona simplemente deja de frenarla (sin saltos).
/// - Si la bola se mueve, no se hunde (y se va recuperando). Si esta quieta, el progreso de
///   hundimiento sube hasta el 100% y salta onBallSwallowed (respawn, game over, etc.).
/// - Mientras mas hundida, mas atascada: el rozamiento aumenta con el progreso.
/// - SinkProgress (0..1) esta disponible para UI, tintes, sonidos, etc.
///
/// MONTAJE:
///  Un collider (Box/Sphere/Capsule/Mesh convex) con este script. Se fuerza a Trigger.
///  Colocalo cubriendo la zona de arena y lo bastante alto para tocar la bola.
///  La cara SUPERIOR del collider se usa como superficie para las particulas.
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuicksandZone : MonoBehaviour
{
    [SerializeField] private string ballTag = "Ball";

    [Header("BORDE")]
    [Tooltip("Margen (m) que el centro de la bola debe salirse del borde para desactivar la arena. Evita parpadeos en el limite.")]
    [SerializeField, Min(0f)] private float edgeHysteresis = 0.1f;

    [Header("RALENTIZACION")]
    [Tooltip("Rozamiento extra mientras la bola esta dentro (1/s). Igual que MudZone.")]
    [SerializeField, Min(0f)] private float drag = 4f;
    [Tooltip("Rozamiento ADICIONAL cuando esta 100% hundida (1/s). Se aplica de forma proporcional al progreso.")]
    [SerializeField, Min(0f)] private float extraDragWhenSunk = 6f;
    [Tooltip("Tambien frena el giro.")]
    [SerializeField, Min(0f)] private float angularDrag = 6f;

    [Header("LIMITE DE VELOCIDAD (= menos fuerza de disparo)")]
    [Tooltip("Velocidad maxima horizontal dentro de la arena. Igual que MudZone.")]
    [SerializeField, Min(0.1f)] private float maxSpeed = 15f;

    [Header("HUNDIMIENTO")]
    [Tooltip("Por debajo de esta velocidad horizontal se considera 'quieta'. Por encima NO se hunde.")]
    [SerializeField, Min(0f)] private float stillSpeed = 1.0f;
    [Tooltip("Segundos quieta antes de empezar a hundirse.")]
    [SerializeField, Min(0f)] private float sinkDelay = 0.4f;
    [Tooltip("Segundos quieta (despues del delay) hasta quedar tragada.")]
    [SerializeField, Min(0.1f)] private float swallowTime = 4f;
    [Tooltip("Cuanto progreso se recupera por segundo mientras la bola se mueve (1 = tarda 1 s en recuperarse del todo).")]
    [SerializeField, Min(0f)] private float recoverSpeed = 0.5f;

    [Header("EVENTOS")]
    public UnityEvent onBallSwallowed;

    [Header("AJUSTES DE PARTICULAS (ARENA)")]
    [SerializeField] private Color sandColor = new Color(0.76f, 0.62f, 0.38f, 1f);
    [SerializeField] private int burstCount = 30;
    [Tooltip("Nubes pequenas por segundo mientras la bola se mueve dentro de la arena.")]
    [SerializeField, Min(0f)] private float struggleEmitRate = 6f;
    [SerializeField] private int struggleBurstCount = 3;
    [SerializeField] private float coneAngle = 50f;
    [SerializeField] private float coneRadius = 0.2f;
    [SerializeField] private Vector2 speedRange = new Vector2(2f, 5f);
    [SerializeField] private Vector2 sizeRange = new Vector2(0.15f, 0.4f);
    [SerializeField] private Vector2 lifetimeRange = new Vector2(0.9f, 1.5f);
    [SerializeField] private float gravity = 0.8f;
    [SerializeField] private float particleDrag = 1.5f;

    // Recursos compartidos entre todas las arenas (se crean una sola vez)
    private static Texture2D sharedAtlas;
    private static Material sharedMaterial;

    private const float CenterEpsilonSqr = 0.0001f;

    private Collider zoneCol;

    // Bola que toca el trigger (aun puede no estar "dentro")
    private Rigidbody candidateRb;
    private int touchCount;

    // Bola realmente afectada (centro dentro)
    private Rigidbody ballRb;

    // Valores cacheados (la zona se asume estatica)
    private float surfaceY;
    private float centerY;
    private float angularDragFactor;
    private float stillSpeedSqr;
    private float maxSpeedSqr;

    private float previousSpeed;
    private float sinkProgress;
    private float stillTimer;
    private float puffAccum;
    private bool swallowed;
    private ParticleSystem sandParticles;

    /// <summary>Progreso de hundimiento: 0 = libre, 1 = tragada.</summary>
    public float SinkProgress => ballRb != null ? sinkProgress : 0f;

    private void Awake()
    {
        zoneCol = GetComponent<Collider>();
        zoneCol.isTrigger = true;
    }

    private void OnDisable()
    {
        Deactivate();
        candidateRb = null;
        touchCount = 0;
    }

    // ---------- Trigger: solo registra la bola como candidata ----------
    private void OnTriggerEnter(Collider other)
    {
        if (!IsBall(other, out Rigidbody rb)) return;
        if (candidateRb != null && rb != candidateRb) return;

        if (candidateRb == null)
        {
            Bounds b = zoneCol.bounds;
            centerY = b.center.y;
            surfaceY = b.max.y;
        }

        candidateRb = rb;
        touchCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (candidateRb == null) return;
        if (!IsBall(other, out Rigidbody rb) || rb != candidateRb) return;

        // Una bola puede tener varios colliders: solo se libera al salir el ultimo
        if (--touchCount > 0) return;

        Deactivate();
        candidateRb = null;
        touchCount = 0;
    }

    private bool IsBall(Collider other, out Rigidbody rb)
    {
        rb = other.attachedRigidbody;
        if (rb == null) return false;
        return other.CompareTag(ballTag) || rb.CompareTag(ballTag);
    }

    // Distancia horizontal del centro de la bola al collider (0 = dentro)
    private float DistanceOutside(Vector3 worldPos)
    {
        worldPos.y = centerY;
        Vector3 closest = zoneCol.ClosestPoint(worldPos);
        float sqr = (closest - worldPos).sqrMagnitude;
        return sqr < CenterEpsilonSqr ? 0f : Mathf.Sqrt(sqr);
    }

    // ---------- Activar / desactivar el efecto sobre la bola ----------
    private void Activate(Rigidbody rb)
    {
        ballRb = rb;

        angularDragFactor = Mathf.Exp(-angularDrag * Time.fixedDeltaTime);
        stillSpeedSqr = stillSpeed * stillSpeed;
        maxSpeedSqr = maxSpeed * maxSpeed;

        Vector3 v = rb.linearVelocity;
        previousSpeed = Mathf.Sqrt(v.x * v.x + v.z * v.z);
        sinkProgress = 0f;
        stillTimer = 0f;
        puffAccum = 0f;
        swallowed = false;

        PlaySplash(rb.position, burstCount);
    }

    private void Deactivate()
    {
        ballRb = null;
        sinkProgress = 0f;
        stillTimer = 0f;
        previousSpeed = 0f;
        swallowed = false;
    }

    // ---------- Simulacion ----------
    private void FixedUpdate()
    {
        // La bola fue destruida mientras estaba dentro
        if (candidateRb == null)
        {
            if (ballRb != null) Deactivate();
            return;
        }

        // Solo actua cuando el CENTRO de la bola esta dentro de la zona
        float outside = DistanceOutside(candidateRb.position);
        if (ballRb == null)
        {
            if (outside > 0f) return; // toca el borde pero el centro aun esta fuera
            Activate(candidateRb);
        }
        else if (outside > edgeHysteresis)
        {
            Deactivate(); // el centro salio de la zona: se deja de frenar y ya
            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector3 v = ballRb.linearVelocity;
        float hSqr = v.x * v.x + v.z * v.z;
        float hSpeed = Mathf.Sqrt(hSqr);

        // Chute dentro de la arena -> salpicadura
        if (hSpeed - previousSpeed > 5f)
            PlaySplash(ballRb.position, burstCount);

        // ---------- 1) Hundimiento: quieta = se hunde, en movimiento = se recupera ----------
        if (hSqr < stillSpeedSqr)
        {
            stillTimer += dt;
            if (stillTimer >= sinkDelay)
                sinkProgress = Mathf.Min(1f, sinkProgress + dt / swallowTime);
        }
        else
        {
            stillTimer = 0f;
            sinkProgress = Mathf.Max(0f, sinkProgress - recoverSpeed * dt);
        }

        // ---------- 2) Rozamiento horizontal (mas fuerte cuanto mas hundida) ----------
        float k = Mathf.Exp(-(drag + extraDragWhenSunk * sinkProgress) * dt);
        v.x *= k;
        v.z *= k;

        // ---------- 3) Limite de velocidad horizontal (igual que MudZone) ----------
        float hMagSqr = v.x * v.x + v.z * v.z;
        if (hMagSqr > maxSpeedSqr)
        {
            float s = maxSpeed / Mathf.Sqrt(hMagSqr);
            v.x *= s;
            v.z *= s;
            hMagSqr = maxSpeedSqr;
        }

        // La velocidad vertical NO se toca: la fisica normal se encarga del suelo
        ballRb.linearVelocity = v;
        ballRb.angularVelocity *= angularDragFactor;

        // ---------- 4) Nubes de arena al forcejear ----------
        if (struggleEmitRate > 0f && hSpeed > stillSpeed * 1.5f)
        {
            puffAccum += struggleEmitRate * dt;
            while (puffAccum >= 1f)
            {
                puffAccum -= 1f;
                PlaySplash(ballRb.position, struggleBurstCount);
            }
        }

        // ---------- 5) Tragada ----------
        if (!swallowed && sinkProgress >= 1f)
        {
            swallowed = true;
            onBallSwallowed?.Invoke();
        }

        previousSpeed = Mathf.Sqrt(hMagSqr);
    }

    // ---------- Particulas ----------
    private void PlaySplash(Vector3 position, int count)
    {
        if (count <= 0) return;

        // Se crea la primera vez que hace falta (las arenas sin uso no gastan nada)
        if (sandParticles == null)
            sandParticles = CreateSandParticles();
        if (sandParticles == null) return;

        // La salpicadura sale en la superficie de la arena
        position.y = Mathf.Min(position.y, surfaceY);
        sandParticles.transform.position = position;
        sandParticles.Emit(count);
    }

    private ParticleSystem CreateSandParticles()
    {
        GameObject go = new GameObject("QuicksandSplashParticles");
        go.transform.SetParent(transform, false);
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = gravity;
        main.maxParticles = 300;
        main.startColor = new ParticleSystem.MinMaxGradient(
            sandColor, Color.Lerp(sandColor, Color.black, 0.3f));

        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startSizeY = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle;
        shape.radius = coneRadius;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 100f;
        limit.dampen = 0f;
        limit.drag = particleDrag;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(
            1f, new AnimationCurve(
                new Keyframe(0f, 0.7f), new Keyframe(0.1f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = fade;

        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = 2;
        sheet.numTilesY = 2;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.99f);
        sheet.cycleCount = 1;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetSharedMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Play(); // loop desactivado y emision a 0: solo emite con Emit()
        return ps;
    }

    // Material y textura compartidos por todas las arenas de la escena
    private static Material GetSharedMaterial()
    {
        if (sharedMaterial != null) return sharedMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) return null;

        if (sharedAtlas == null) sharedAtlas = CreateBlobAtlas();
        sharedMaterial = new Material(shader) { mainTexture = sharedAtlas };
        return sharedMaterial;
    }

    private static Texture2D CreateBlobAtlas()
    {
        const int tile = 64;
        const int tiles = 2;
        const int size = tile * tiles;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        System.Random rng = new System.Random(12345);
        float[] amp = new float[3];
        float[] phase = new float[3];

        for (int ty = 0; ty < tiles; ty++)
        {
            for (int tx = 0; tx < tiles; tx++)
            {
                for (int k = 0; k < 3; k++)
                {
                    amp[k] = 0.01f + (float)rng.NextDouble() * 0.02f;
                    phase[k] = (float)rng.NextDouble() * Mathf.PI * 2f;
                }

                for (int y = 0; y < tile; y++)
                {
                    for (int x = 0; x < tile; x++)
                    {
                        float dx = (x + 0.5f) / tile * 2f - 1f;
                        float dy = (y + 0.5f) / tile * 2f - 1f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(dy, dx);

                        float r = 0.78f;
                        for (int k = 0; k < 3; k++)
                            r += amp[k] * Mathf.Sin((k + 2) * ang + phase[k]);

                        float t = Mathf.Clamp01((dist - (r - 0.08f)) / 0.08f);
                        float alpha = 1f - t * t * (3f - 2f * t);

                        float shade = 1f - 0.35f * Mathf.Clamp01(dist / r);
                        byte c = (byte)(shade * 255f);

                        int index = (ty * tile + y) * size + tx * tile + x;
                        pixels[index] = new Color32(c, c, c, (byte)(alpha * 255f));
                    }
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true); // sin mipmaps y libera la copia en CPU
        return tex;
    }
}