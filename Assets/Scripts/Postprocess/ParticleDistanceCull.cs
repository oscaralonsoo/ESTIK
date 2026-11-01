using UnityEngine;

/// <summary>
/// Oculta un ParticleSystem cuando la camara esta lejos de el.
/// Pensado para zonas de viento: ponlo en la zona de atras y a partir de
/// "Hide Distance" deja de verse.
///
/// Modo suave (por defecto): deja de emitir y las particulas que ya existen
/// terminan su vida solas, asi no desaparecen de golpe.
/// Modo instantaneo: borra todo y apaga el renderer al momento.
///
/// Uso: añadelo al mismo objeto que el ParticleSystem.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleDistanceCull : MonoBehaviour
{
    [Header("Camara")]
    [Tooltip("Si esta vacia usa Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Distancia")]
    [Tooltip("Desde el centro de este objeto (o del de abajo) hasta la camara.")]
    [SerializeField] private Transform distanceReference;
    [Tooltip("A partir de esta distancia (en metros) el sistema se oculta.")]
    [SerializeField] private float hideDistance = 30f;
    [Tooltip("Se vuelve a mostrar al bajar de (Hide Distance - Hysteresis). Evita parpadeos en el limite.")]
    [SerializeField] private float hysteresis = 3f;

    [Header("Modo")]
    [Tooltip("Si esta activo se oculta al instante; si no, las particulas existentes mueren solas.")]
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

        // Si empieza lejos, arranca ya oculto y sin particulas
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

        // Modo suave: cuando ya no queda ninguna particula viva, apaga el renderer
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
