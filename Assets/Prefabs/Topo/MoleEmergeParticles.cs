using System.Collections;
using UnityEngine;

public class MoleEmergeParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem dirtParticles;
    [SerializeField] private Transform holePoint;

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

    [Header("Ajustes del Trigger Collider")]
    [SerializeField] private float maxColliderRadius = 1.5f;
    [SerializeField] private float colliderExpandDuration = 0.5f;
    [SerializeField] private float repelForce = 10f;

    private void Awake()
    {
        if (dirtParticles == null)
            dirtParticles = CreateDirtParticles();
    }

    public void PlayEmerge()
    {
        Transform t = dirtParticles.transform;
        t.position = holePoint != null ? holePoint.position : transform.position;

        if (!dirtParticles.isPlaying)
            dirtParticles.Play();

        dirtParticles.Emit(burstCount);

        StartCoroutine(ExpandTriggerCollider(t));
    }

    private IEnumerator ExpandTriggerCollider(Transform particleTransform)
    {
        GameObject triggerObj = new GameObject("EmergeTrigger");
        triggerObj.transform.SetParent(particleTransform, false);
        triggerObj.transform.localPosition = Vector3.zero;

        SphereCollider sphereCollider = triggerObj.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = 0.05f;

        MoleTriggerHandler triggerHandler = triggerObj.AddComponent<MoleTriggerHandler>();
        triggerHandler.repelForce = repelForce;
        triggerHandler.originTransform = particleTransform;

        float elapsed = 0f;

        while (elapsed < colliderExpandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / colliderExpandDuration);

            float currentRadius = Mathf.Lerp(0.05f, maxColliderRadius, Mathf.Sin(t * Mathf.PI * 0.5f));
            sphereCollider.radius = currentRadius;

            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        if (triggerObj != null)
        {
            Destroy(triggerObj);
        }
    }

    private class MoleTriggerHandler : MonoBehaviour
    {
        public float repelForce;
        public Transform originTransform;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Ball"))
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 pushDir = (other.transform.position - originTransform.position).normalized;
                    if (pushDir.y < 0.2f) pushDir.y = 0.2f;
                    pushDir.Normalize();

                    rb.AddForce(pushDir * repelForce, ForceMode.Impulse);
                }
            }
        }
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