using UnityEngine;

// Ponlo en el objeto raíz del tornado (el mismo que SandTornadoAppear / SandTornadoMotion).
// Si la bola está cerca, el tornado la atrae, la hace girar y la sube. Si la atracción es floja
// y el giro fuerte, la bola se va abriendo en espiral hasta salir del radio y sigue por inercia.
public class SandTornadoPull : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Rigidbody de la bola. Vacío = busca el objeto con tag Player.")]
    public Rigidbody ball;

    [Header("Alcance")]
    [Tooltip("Distancia horizontal desde el eje del tornado a la que empieza a afectar a la bola.")]
    public float radius = 6f;
    [Tooltip("Altura del tornado. Al llegar arriba la suelta. Ponla muy alta si quieres que salga por el lateral en vez de por arriba.")]
    public float height = 30f;

    [Header("Fuerzas (aceleración)")]
    [Tooltip("Atracción hacia el eje. Bajo = la bola se abre en espiral y sale del radio. Alto = se queda pegada.")]
    public float pull = 5f;
    [Tooltip("Giro alrededor del tornado. Alto = da vueltas más rápido y la fuerza centrífuga la saca fuera.")]
    public float swirl = 30f;
    [Tooltip("Empuje hacia arriba. Tiene que ser mayor que la gravedad (~9.8) para que suba.")]
    public float lift = 26f;
    [Tooltip("Hace que la bola iguale la velocidad del tornado (la arrastra con él).")]
    public float carry = 1f;
    [Tooltip("Empuje extra en la dirección en la que avanza el tornado, en la mitad de arriba. 0 = desactivado.")]
    public float push = 0f;
    [Tooltip("1 = giro en un sentido, -1 = en el contrario.")]
    public float swirlDirection = 1f;

    [Header("Suelta")]
    [Tooltip("Segundos tras soltarla en los que el tornado no vuelve a atraerla (para que salga por inercia).")]
    public float releaseCooldown = 1.5f;

    [Header("Visualización")]
    [Tooltip("Dibuja el radio y la altura en la escena (y en Game si tienes activado el botón Gizmos).")]
    public bool showRadius = true;

    SandTornadoAppear appear;
    Vector3 lastPos;
    Vector3 tornadoVel;
    Vector3 travelDir = Vector3.left;
    float cooldown;
    bool warned;

    void Awake()
    {
        appear = GetComponent<SandTornadoAppear>();
        lastPos = transform.position;
    }

    void FixedUpdate()
    {
        // Velocidad y dirección del tornado (se mueve por transform, no por física)
        tornadoVel = (transform.position - lastPos) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        lastPos = transform.position;

        Vector3 flatVel = new Vector3(tornadoVel.x, 0f, tornadoVel.z);
        if (flatVel.sqrMagnitude > 0.0001f) travelDir = flatVel.normalized;

        if (ball == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) ball = go.GetComponent<Rigidbody>();
            if (ball == null)
            {
                if (!warned) { Debug.LogWarning("SandTornadoPull: asigna la bola (Rigidbody) o ponle el tag Player.", this); warned = true; }
                return;
            }
        }
        if (ball.isKinematic) return;

        if (cooldown > 0f) { cooldown -= Time.fixedDeltaTime; return; }
        if (appear != null && !appear.IsVisible) return;

        Vector3 to = ball.position - transform.position;
        Vector3 flat = new Vector3(to.x, 0f, to.z);
        float d = flat.magnitude;

        if (d > radius || to.y < -1f) return;

        // Llegó arriba: se suelta y sigue por su propia inercia
        if (to.y > height)
        {
            cooldown = releaseCooldown;
            return;
        }

        // Fuerza plena en la mitad interior del radio, y suave hacia el borde
        float k = Mathf.SmoothStep(0f, 1f, (1f - d / radius) * 2f);

        float climb = Mathf.Clamp01(to.y / height);

        // El empuje hacia arriba se apaga suave cerca de la cima; el empuje hacia delante sube con la altura
        float topFade = 1f - Mathf.SmoothStep(0.7f, 1f, climb);
        float pushRamp = Mathf.SmoothStep(0.5f, 1f, climb);

        Vector3 axisDir = d > 0.01f ? flat / d : Vector3.zero;

        Vector3 inward = -axisDir * pull;
        Vector3 spin = Vector3.Cross(Vector3.up, axisDir) * swirl * swirlDirection;
        Vector3 up = Vector3.up * lift * topFade;
        Vector3 match = new Vector3(tornadoVel.x - ball.linearVelocity.x, 0f, tornadoVel.z - ball.linearVelocity.z) * carry;
        Vector3 forward = travelDir * push * pushRamp;

        ball.AddForce((inward + spin + up + match + forward) * k, ForceMode.Acceleration);
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

    void OnDrawGizmos()
    {
        if (!showRadius) return;

        Vector3 c = transform.position;
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        DrawRing(c, radius);
        DrawRing(c + Vector3.up * height, radius);
        Gizmos.DrawLine(c, c + Vector3.up * height);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 0.5f;
            Vector3 e = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(c + e, c + e + Vector3.up * height);
        }

        // Círculo del radio a la altura de la bola: verde = dentro, rojo = fuera
        if (ball != null)
        {
            Vector3 b = ball.position;
            float d = new Vector2(b.x - c.x, b.z - c.z).magnitude;
            Gizmos.color = d <= radius ? Color.green : Color.red;
            DrawRing(new Vector3(c.x, b.y, c.z), radius);
            Gizmos.DrawLine(b, new Vector3(c.x, b.y, c.z));
        }
    }
}
