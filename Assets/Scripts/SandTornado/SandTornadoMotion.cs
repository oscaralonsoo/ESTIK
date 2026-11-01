using UnityEngine;

public class SandTornadoMotion : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 2.5f;
    public float sway = 0.5f;
    public float swayFrequency = 0.8f;
    [Tooltip("Ida y vuelta: al llegar a un extremo da la vuelta. Si está desactivado, usa 'loop'.")]
    public bool pingPong = true;
    [Tooltip("Solo si pingPong está desactivado: al llegar al final reaparece en el inicio.")]
    public bool loop = true;
    [Tooltip("Punto de inicio en X.")]
    public float startX = 12f;
    [Tooltip("Punto final en X. Si es mayor que startX va de izquierda a derecha, si es menor de derecha a izquierda.")]
    public float endX = -12f;
    public float tilt = 6f;
    public float leanWobble = 4f;
    public float leanSpeed = 2.2f;
    [Tooltip("Qué tan rápido se gira la inclinación al cambiar de sentido.")]
    public float turnSpeed = 3f;

    [Header("Anillos")]
    [Tooltip("Padre de los anillos. Vacío = busca el hijo llamado Body.")]
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

    bool goingToEnd = true;
    float dir = -1f;
    float facing = -1f;

    void OnEnable() { Init(); }

    void Init()
    {
        phase = Random.value * 10f;

        goingToEnd = true;
        dir = Mathf.Sign(endX - startX);
        if (dir == 0f) dir = -1f;
        facing = dir;

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
        Vector3 p = transform.position;

        float targetX = goingToEnd ? endX : startX;
        if (!Mathf.Approximately(targetX, p.x)) dir = Mathf.Sign(targetX - p.x);

        p.x = Mathf.MoveTowards(p.x, targetX, speed * Time.deltaTime);
        p.z += Mathf.Cos((Time.time + phase) * swayFrequency) * sway * Time.deltaTime;

        if (Mathf.Approximately(p.x, targetX))
        {
            if (pingPong) goingToEnd = !goingToEnd;
            else if (loop) p.x = startX;
        }

        transform.position = p;

        facing = Mathf.MoveTowards(facing, dir, turnSpeed * Time.deltaTime);
        float lean = tilt * facing + Mathf.Sin(Time.time * leanSpeed + phase) * leanWobble;
        transform.rotation = Quaternion.Euler(0f, 0f, lean);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 p = transform.position;
        Gizmos.DrawLine(new Vector3(startX, p.y, p.z), new Vector3(endX, p.y, p.z));
    }
}