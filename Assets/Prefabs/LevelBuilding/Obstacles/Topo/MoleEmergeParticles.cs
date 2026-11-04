using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MoleEmergeParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem dirtParticles;
    [SerializeField] private Transform holePoint;

    [Header("Sonido")]
    [SerializeField] private AudioClip[] emergeSounds;
    [SerializeField] private AudioMixerGroup mixerGroup;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;
    [SerializeField] private float hearDistance = 12f;
    [Tooltip("Si es true, el volumen baja con la distancia a la bola. Si es false, suena a volumen fijo dentro del rango.")]
    [SerializeField] private bool fadeWithDistance = true;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [Tooltip("Si es true, evita repetir el mismo clip dos veces seguidas.")]
    [SerializeField] private bool avoidRepeat = true;

    [Header("Sonido de golpe (Bounce)")]
    [SerializeField] private bool playBounceOnBodyHit = true;
    [SerializeField] private float bounceReferenceSpeed = 30f;

    [Header("Ajustes del Animator")]
    private Animator moleAnimator;
    [SerializeField] private float animatorDelay = 0f;

    [Header("Ajustes de Partículas")]
    [SerializeField] private Color dirtColor = new Color(0.40f, 0.27f, 0.15f, 1f);
    [SerializeField] private int burstCount = 40;
    [SerializeField] private float coneAngle = 50f;
    [SerializeField] private float coneRadius = 0.2f;
    [SerializeField] private Vector2 speedRange = new Vector2(4f, 8f);
    [SerializeField] private Vector2 sizeRange = new Vector2(0.2f, 0.5f);
    [SerializeField] private Vector2 lifetimeRange = new Vector2(0.9f, 1.5f);
    [SerializeField] private float gravity = 0.8f;
    [SerializeField] private float drag = 1.5f;

    [Header("Onda de repulsión (esfera)")]
    [SerializeField] private string ballTag = "Ball";
    [SerializeField] private LayerMask ballLayers = ~0;
    [SerializeField] private float startRadius = 0.2f;
    [SerializeField] private float maxColliderRadius = 1.5f;
    [SerializeField] private float colliderExpandDuration = 0.5f;
    [SerializeField] private AnimationCurve expandCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float repelForce = 10f;
    [SerializeField] private float upwardLift = 2f;
    [Tooltip("Si es true, cada bola solo es empujada una vez por emergencia.")]
    [SerializeField] private bool hitEachBallOnce = true;
    [Tooltip("Adelanta la onda: empieza con este tiempo ya transcurrido, así llega antes a la bola (segundos).")]
    [SerializeField] private float burstLeadTime = 0.15f;

    [Header("Collider del cuerpo (sigue la animación)")]
    [SerializeField] private bool enableBodyCollider = true;
    [Tooltip("Si es true, la colisión de la bola con el cuerpo la repele igual que la onda.")]
    [SerializeField] private bool bodyRepelsBall = true;
    [Tooltip("Transform que se mueve con la animación (hueso/malla del topo). Si está vacío usa este transform.")]
    [SerializeField] private Transform followTarget;
    [Tooltip("Distancia desde el pivote de followTarget hasta lo más alto del topo (cabeza).")]
    [SerializeField] private float topOffset = 0.5f;
    [SerializeField] private float bodyRadius = 0.35f;
    [SerializeField] private float minBodyHeight = 0.05f;
    [SerializeField] private float maxBodyHeight = 2f;

    private bool burstActive;
    private float burstTimer;
    private readonly HashSet<Rigidbody> alreadyHit = new HashSet<Rigidbody>();
    private readonly Collider[] overlapBuffer = new Collider[32];

    private GameObject bodyObj;
    private Rigidbody bodyRb;
    private CapsuleCollider bodyCollider;

    private AudioSource audioSource;
    private Transform ballTransform;
    private int lastSoundIndex = -1;

    private Vector3 Holeposition => holePoint != null ? holePoint.position : transform.position;

    private void Awake()
    {
        if (dirtParticles == null)
            dirtParticles = CreateDirtParticles();

        if (moleAnimator == null)
            moleAnimator = GetComponent<Animator>();

        if (followTarget == null)
            followTarget = transform;

        if (moleAnimator != null && animatorDelay > 0f)
        {
            moleAnimator.enabled = false;
            StartCoroutine(DelayedAnimatorStart());
        }

        if (enableBodyCollider)
            CreateBodyCollider();

        CreateAudioSource();
    }

    private void OnDestroy()
    {
        if (bodyObj != null) Destroy(bodyObj);
    }

    private void OnDisable()
    {
        burstActive = false;
        if (bodyCollider != null) bodyCollider.enabled = false;
    }

    private IEnumerator DelayedAnimatorStart()
    {
        yield return new WaitForSeconds(animatorDelay);
        if (moleAnimator != null)
            moleAnimator.enabled = true;
    }

    public void PlayEmerge()
    {
        Transform t = dirtParticles.transform;
        t.position = Holeposition;

        if (!dirtParticles.isPlaying)
            dirtParticles.Play();

        dirtParticles.Emit(burstCount);

        PlayEmergeSound();

        alreadyHit.Clear();
        burstTimer = Mathf.Clamp(burstLeadTime, 0f, colliderExpandDuration);
        burstActive = true;
    }

    private void CreateAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.outputAudioMixerGroup = mixerGroup;
    }

    private Transform FindBall()
    {
        if (ballTransform == null)
        {
            GameObject ball = GameObject.FindGameObjectWithTag(ballTag);
            if (ball != null) ballTransform = ball.transform;
        }
        return ballTransform;
    }

    private AudioClip PickRandomClip()
    {
        if (emergeSounds == null || emergeSounds.Length == 0) return null;

        int index = Random.Range(0, emergeSounds.Length);

        if (avoidRepeat && emergeSounds.Length > 1 && index == lastSoundIndex)
            index = (index + Random.Range(1, emergeSounds.Length)) % emergeSounds.Length;

        lastSoundIndex = index;
        return emergeSounds[index];
    }

    private void PlayEmergeSound()
    {
        if (audioSource == null) return;

        Transform ball = FindBall();
        if (ball == null) return;

        float distance = Vector3.Distance(ball.position, Holeposition);
        if (distance > hearDistance) return;

        AudioClip clip = PickRandomClip();
        if (clip == null) return;

        float volume = soundVolume;
        if (fadeWithDistance)
            volume *= 1f - Mathf.Clamp01(distance / Mathf.Max(0.0001f, hearDistance));

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }

    private void FixedUpdate()
    {
        UpdateRepelBurst();
        UpdateBodyCollider();
    }

    private void UpdateRepelBurst()
    {
        if (!burstActive) return;

        burstTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(burstTimer / Mathf.Max(0.0001f, colliderExpandDuration));
        float radius = Mathf.Lerp(startRadius, maxColliderRadius, expandCurve.Evaluate(t));
        Vector3 center = Holeposition;

        int count = Physics.OverlapSphereNonAlloc(
            center, radius, overlapBuffer, ballLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (!IsBall(col, out Rigidbody rb)) continue;
            if (hitEachBallOnce && alreadyHit.Contains(rb)) continue;

            alreadyHit.Add(rb);
            ApplyRepel(rb, center);
        }

        if (t >= 1f)
            burstActive = false;
    }

    private void ApplyRepel(Rigidbody rb, Vector3 center)
    {
        Vector3 pushDir = rb.worldCenterOfMass - center;
        if (pushDir.sqrMagnitude < 0.0001f) pushDir = Vector3.up;
        pushDir.Normalize();
        if (pushDir.y < 0.2f) pushDir.y = 0.2f;
        pushDir.Normalize();

        Vector3 targetVelocity = pushDir * repelForce;
        targetVelocity.y += upwardLift;

        rb.WakeUp();
        rb.linearVelocity = targetVelocity;
    }

    private bool IsBall(Collider col, out Rigidbody rb)
    {
        rb = col.attachedRigidbody;
        if (rb == null || rb.isKinematic) return false;
        return col.CompareTag(ballTag) || rb.CompareTag(ballTag);
    }

    private void CreateBodyCollider()
    {
        bodyObj = new GameObject("MoleBodyCollider");
        bodyObj.layer = gameObject.layer;
        bodyObj.transform.position = Holeposition;

        bodyRb = bodyObj.AddComponent<Rigidbody>();
        bodyRb.isKinematic = true;
        bodyRb.interpolation = RigidbodyInterpolation.Interpolate;

        bodyCollider = bodyObj.AddComponent<CapsuleCollider>();
        bodyCollider.direction = 1;
        bodyCollider.radius = bodyRadius;
        bodyCollider.enabled = false;

        if (bodyRepelsBall || playBounceOnBodyHit)
        {
            var relay = bodyObj.AddComponent<MoleBodyCollisionRelay>();
            relay.onCollision = OnBodyCollision;
        }
    }

    private void OnBodyCollision(Collision collision)
    {
        Collider col = collision.collider;
        if (!IsBall(col, out Rigidbody rb)) return;

        if (playBounceOnBodyHit)
        {
            if (rb.TryGetComponent(out BallSoundController ballSound) ||
                col.TryGetComponent(out ballSound))
            {
                float speed01 = collision.relativeVelocity.magnitude / Mathf.Max(0.0001f, bounceReferenceSpeed);
                ballSound.PlayMoleSound(speed01);
            }
        }

        if (!bodyRepelsBall) return;

        Vector3 hole = Holeposition;
        Vector3 center = new Vector3(hole.x, rb.worldCenterOfMass.y, hole.z);

        ApplyRepel(rb, center);
    }

    private void UpdateBodyCollider()
    {
        if (bodyCollider == null) return;

        Vector3 basePos = Holeposition;
        float topY = followTarget.position.y + topOffset;
        float visibleHeight = Mathf.Min(topY - basePos.y, maxBodyHeight);

        if (visibleHeight < minBodyHeight)
        {
            if (bodyCollider.enabled) bodyCollider.enabled = false;
            return;
        }

        float capsuleHeight = Mathf.Max(visibleHeight, bodyRadius * 2f);
        bodyCollider.radius = bodyRadius;
        bodyCollider.height = capsuleHeight;
        bodyCollider.center = new Vector3(0f, visibleHeight - capsuleHeight * 0.5f, 0f);

        if (!bodyCollider.enabled) bodyCollider.enabled = true;
        bodyRb.MovePosition(basePos);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 hole = holePoint != null ? holePoint.position : transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(hole, maxColliderRadius);

        Gizmos.color = new Color(0f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(hole, hearDistance);

        Transform ft = followTarget != null ? followTarget : transform;
        Gizmos.color = Color.green;
        Vector3 top = new Vector3(hole.x, ft.position.y + topOffset, hole.z);
        Gizmos.DrawLine(hole, top);
        Gizmos.DrawWireSphere(top, bodyRadius);
    }

    private ParticleSystem CreateDirtParticles()
    {
        GameObject go = new GameObject("MoleDirtParticles");
        go.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
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
            dirtColor, Color.Lerp(dirtColor, Color.black, 0.35f));

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
        limit.drag = drag;

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
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        mat.mainTexture = CreateBlobAtlas();
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return ps;
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

        for (int ty = 0; ty < tiles; ty++)
        {
            for (int tx = 0; tx < tiles; tx++)
            {
                float[] amp = new float[3];
                float[] phase = new float[3];
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
        tex.Apply();
        return tex;
    }
}

public class MoleBodyCollisionRelay : MonoBehaviour
{
    public System.Action<Collision> onCollision;

    private void OnCollisionEnter(Collision collision)
    {
        onCollision?.Invoke(collision);
    }
}