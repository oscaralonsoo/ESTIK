using UnityEngine;

public class SandTornadoPull : MonoBehaviour
{
    [Header("Objetivo")]
    public Rigidbody ball;

    [Header("Alcance")]
    public float radius = 6f;
    public float height = 30f;

    [Header("Fuerzas (aceleración)")]
    public float pull = 5f;
    public float swirl = 30f;
    public float lift = 26f;
    public float carry = 1f;
    public float push = 0f;
    public float swirlDirection = 1f;

    [Header("Suelta")]
    public float releaseCooldown = 1.5f;

    [Header("Lanzamiento al desaparecer")]
    public bool launchOnDisappear = true;
    [Tooltip("Velocidad extra proporcional a la velocidad que lleva la bola (0.6 = +60%)")]
    public float speedBoost = 0.6f;
    [Tooltip("Velocidad extra mínima, por si la bola va muy lenta")]
    public float minLaunch = 5f;
    [Tooltip("Cuánto dura la suelta. Más tiempo = más suave y natural")]
    public float releaseTime = 0.7f;
    [Tooltip("Velocidad extra hacia arriba repartida durante la suelta")]
    public float launchUp = 3f;

    [Header("Visualización")]
    public bool showRadius = true;

    SandTornadoAppear appear;
    Vector3 lastPos;
    Vector3 tornadoVel;
    Vector3 travelDir = Vector3.left;
    float cooldown;
    bool wasVisible = true;
    bool inside;

    bool releasing;
    float releaseT;
    float extraSpeed;
    Vector3 releaseDir;

    void Awake()
    {
        appear = GetComponent<SandTornadoAppear>();
        lastPos = transform.position;
    }

    void FixedUpdate()
    {
        tornadoVel = (transform.position - lastPos) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        lastPos = transform.position;

        Vector3 flatVel = new Vector3(tornadoVel.x, 0f, tornadoVel.z);
        if (flatVel.sqrMagnitude > 0.0001f) travelDir = flatVel.normalized;

        if (ball == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) ball = go.GetComponent<Rigidbody>();
        }
        if (ball == null || ball.isKinematic) return;

        bool visible = appear == null || appear.IsVisible;
        if (launchOnDisappear && wasVisible && !visible && inside) StartRelease();
        wasVisible = visible;
        inside = false;

        ApplyRelease();

        if (cooldown > 0f) { cooldown -= Time.fixedDeltaTime; return; }
        if (!visible) return;

        Vector3 to = ball.position - transform.position;
        Vector3 flat = new Vector3(to.x, 0f, to.z);
        float d = flat.magnitude;

        if (d > radius || to.y < -1f) return;

        if (to.y > height)
        {
            cooldown = releaseCooldown;
            return;
        }

        float k = Mathf.SmoothStep(0f, 1f, (1f - d / radius) * 2f);

        float climb = Mathf.Clamp01(to.y / height);

        float topFade = 1f - Mathf.SmoothStep(0.7f, 1f, climb);
        float pushRamp = Mathf.SmoothStep(0.5f, 1f, climb);

        Vector3 axisDir = d > 0.01f ? flat / d : Vector3.zero;

        Vector3 inward = -axisDir * pull;
        Vector3 spin = Vector3.Cross(Vector3.up, axisDir) * swirl * swirlDirection;
        Vector3 up = Vector3.up * lift * topFade;
        Vector3 match = new Vector3(tornadoVel.x - ball.linearVelocity.x, 0f, tornadoVel.z - ball.linearVelocity.z) * carry;
        Vector3 forward = travelDir * push * pushRamp;

        ball.AddForce((inward + spin + up + match + forward) * k, ForceMode.Acceleration);
        inside = true;
    }

    void StartRelease()
    {
        Vector3 v = ball.linearVelocity;
        Vector3 flatV = new Vector3(v.x, 0f, v.z);
        float speed = flatV.magnitude;

        releaseDir = speed > 0.5f ? flatV / speed : travelDir;
        extraSpeed = Mathf.Max(minLaunch, speed * speedBoost);
        releaseT = 0f;
        releasing = true;
    }
    void ApplyRelease()
    {
        if (!releasing) return;

        float T = Mathf.Max(0.05f, releaseTime);
        releaseT += Time.fixedDeltaTime;
        float s = releaseT / T;
        if (s >= 1f) { releasing = false; return; }

        float bell = 6f * s * (1f - s) / T;

        Vector3 v = ball.linearVelocity;
        Vector3 flatV = new Vector3(v.x, 0f, v.z);
        Vector3 dir = flatV.sqrMagnitude > 0.25f ? flatV.normalized : releaseDir;

        ball.AddForce((dir * extraSpeed + Vector3.up * launchUp) * bell, ForceMode.Acceleration);
    }

    void DrawRing(Vector3 c, float r)
    {
        const int seg = 48;
        Vector3 prev = c + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}