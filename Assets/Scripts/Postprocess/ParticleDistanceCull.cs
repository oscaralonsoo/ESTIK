using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleDistanceCull : MonoBehaviour
{
    [Header("Camara")]
    [SerializeField] private Camera targetCamera;

    [Header("Distancia")]
    [SerializeField] private Transform distanceReference;
    [SerializeField] private float hideDistance = 30f;
    [SerializeField] private float hysteresis = 3f;

    [Header("Modo")]
    [SerializeField] private bool instant = false;

    [Header("DEBUG")]
    [SerializeField] private bool hidden;
    [SerializeField] private float currentDistance;

    private ParticleSystem ps;
    private ParticleSystemRenderer rend;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        rend = GetComponent<ParticleSystemRenderer>();
        if (distanceReference == null) distanceReference = transform;
    }

    void Start()
    {
        Camera cam = GetCamera();
        if (cam == null) return;

        if (Distance(cam) > hideDistance)
        {
            hidden = true;
            ps.Clear(true);
            SetEmission(false);
            rend.enabled = false;
        }
    }

    void Update()
    {
        Camera cam = GetCamera();
        if (cam == null) return;

        currentDistance = Distance(cam);

        if (!hidden && currentDistance > hideDistance)
            SetHidden(true);
        else if (hidden && currentDistance < hideDistance - hysteresis)
            SetHidden(false);

        if (hidden && rend.enabled && ps.particleCount == 0)
            rend.enabled = false;
    }

    void SetHidden(bool value)
    {
        hidden = value;

        if (value)
        {
            SetEmission(false);

            if (instant)
            {
                ps.Clear(true);
                rend.enabled = false;
            }
        }
        else
        {
            rend.enabled = true;
            SetEmission(true);
            if (!ps.isPlaying) ps.Play(true);
        }
    }

    void SetEmission(bool enabled)
    {
        var emission = ps.emission;
        emission.enabled = enabled;
    }

    Camera GetCamera()
    {
        return targetCamera != null ? targetCamera : Camera.main;
    }

    float Distance(Camera cam)
    {
        return Vector3.Distance(cam.transform.position, distanceReference.position);
    }

    void OnDrawGizmosSelected()
    {
        Transform r = distanceReference != null ? distanceReference : transform;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
        Gizmos.DrawWireSphere(r.position, hideDistance);
    }
}
