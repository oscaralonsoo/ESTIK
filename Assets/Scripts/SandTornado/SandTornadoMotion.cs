using UnityEngine;

public class SandTornadoMotion : MonoBehaviour
{
    [Header("Recorrido")]
    [Tooltip("Punto de inicio. Puede ser hijo del tornado: su posicion se fija al empezar.")]
    public Transform pointA;
    [Tooltip("Punto final. Puede ser hijo del tornado: su posicion se fija al empezar.")]
    public Transform pointB;
    [Tooltip("Si esta activo, A y B se siguen en vivo (solo para puntos que NO sean hijos del tornado y se muevan solos). Por defecto se fijan al empezar.")]
    public bool followPointsLive = false;

    [Header("Movimiento")]
    public float speed = 2.5f;
    public float sway = 0.5f;
    public float swayFrequency = 0.8f;
    [Tooltip("Va de A a B y vuelve de B a A.")]
    public bool pingPong = true;
    [Tooltip("Solo si PingPong esta desactivado: al llegar a B reaparece en A y repite.")]
    public bool loop = true;
    public float tilt = 6f;
    public float leanWobble = 4f;
    public float leanSpeed = 2.2f;
    public float turnSpeed = 3f;

    [Header("Anillos")]
    public Transform body;
    public float tierSpin = 2.5f;
    public float wobble = 0.18f;
    public float wobbleSpeed = 3f;
    [Range(0f, 0.2f)] public float squash = 0.08f;
    public float squashSpeed = 5f;

    Transform[] tiers;
    float[] baseY;
    float[] tn;
    float[] spin;
    float phase;
    float height;

    bool goingToB = true;
    float facing = 1f;
    Vector3 basePos;

    // Posiciones del mundo de A y B, fijadas en Awake (antes de que el tornado se mueva)
    Vector3 worldA;
    Vector3 worldB;
    bool hasPoints;

    Vector3 GetA() => followPointsLive && pointA != null ? pointA.position : worldA;
    Vector3 GetB() => followPointsLive && pointB != null ? pointB.position : worldB;

    void Awake()
    {
        hasPoints = pointA != null && pointB != null;
        if (pointA != null) worldA = pointA.position;
        if (pointB != null) worldB = pointB.position;
    }

    void OnEnable() { Init(); }

    void Init()
    {
        phase = Random.value * 10f;

        goingToB = true;
        facing = 1f;
        basePos = hasPoints ? GetA() : transform.position;

        if (body == null) body = transform.Find("Body");
        if (body == null) { tiers = new Transform[0]; return; }

        int n = body.childCount;
        tiers = new Transform[n];
        baseY = new float[n];
        tn = new float[n];
        spin = new float[n];

        for (int i = 0; i < n; i++)
        {
            tiers[i] = body.GetChild(i);
            baseY[i] = tiers[i].localPosition.y;
        }

        float tierH = n > 1 ? baseY[1] - baseY[0] : 1f;
        height = n > 0 ? baseY[n - 1] + tierH * 0.5f : 1f;

        for (int i = 0; i < n; i++)
        {
            tn[i] = Mathf.Clamp01(baseY[i] / height);
            spin[i] = tierSpin * (0.8f + 0.4f * Mathf.Repeat(i * 0.4142136f, 1f));
        }
    }

    void Update()
    {
        if (!hasPoints) return;

        Vector3 a = GetA();
        Vector3 b = GetB();
        Vector3 target = goingToB ? b : a;

        // Avance sobre la linea A <-> B
        basePos = Vector3.MoveTowards(basePos, target, speed * Time.deltaTime);

        if ((basePos - target).sqrMagnitude < 0.0001f)
        {
            if (pingPong) goingToB = !goingToB;
            else if (loop) basePos = a;
        }

        // Direccion horizontal del recorrido (A -> B)
        Vector3 pathH = b - a;
        pathH.y = 0f;
        if (pathH.sqrMagnitude < 0.0001f) pathH = Vector3.right;
        else pathH.Normalize();

        // Balanceo lateral, perpendicular al recorrido
        Vector3 lateral = Vector3.Cross(Vector3.up, pathH);
        float amp = swayFrequency > 0.0001f ? sway / swayFrequency : 0f;
        float side = Mathf.Sin((Time.time + phase) * swayFrequency) * amp;

        transform.position = basePos + lateral * side;

        // Inclinacion: la parte de arriba se queda atras respecto al avance
        float dir = goingToB ? 1f : -1f;
        facing = Mathf.MoveTowards(facing, dir, turnSpeed * Time.deltaTime);

        float lean = (tilt * facing + Mathf.Sin(Time.time * leanSpeed + phase) * leanWobble) * Mathf.Deg2Rad;
        Vector3 tiltedUp = (Vector3.up * Mathf.Cos(lean) - pathH * Mathf.Sin(lean)).normalized;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, tiltedUp);
    }

    void LateUpdate()
    {
        if (tiers == null) return;
        float time = Time.time + phase;

        for (int i = 0; i < tiers.Length; i++)
        {
            if (tiers[i] == null) continue;

            float ph = time * wobbleSpeed - tn[i] * 4f;
            float wx = Mathf.Sin(ph) * wobble * tn[i];
            float wz = Mathf.Cos(ph) * wobble * tn[i];

            tiers[i].localPosition = new Vector3(wx, baseY[i], wz);
            tiers[i].localRotation = Quaternion.Euler(0f, time * spin[i] * Mathf.Rad2Deg, 0f);

            float k = Mathf.Sin(time * squashSpeed - i * 0.9f) * squash;
            tiers[i].localScale = new Vector3(1f - k * 0.5f, 1f + k, 1f - k * 0.5f);
        }
    }

    // Recorrido en la Scene al seleccionar el tornado: verde = A, rojo = B
    void OnDrawGizmosSelected()
    {
        Vector3 a, b;

        if (Application.isPlaying && hasPoints)
        {
            a = GetA();
            b = GetB();
        }
        else
        {
            if (pointA == null || pointB == null) return;
            a = pointA.position;
            b = pointB.position;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(a, b);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(a, 0.6f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(b, 0.6f);
    }
}