
using UnityEngine;

public class StickController : MonoBehaviour
{
/*    [Header("REFERENCES")]
    [SerializeField] private BallController ballController;
    [SerializeField] private GameObject stickPrefab;

    [Header("POSITION")]
    [SerializeField] private float baseDistance = 0.5f;
    [SerializeField] private float maxPullbackDistance = 1.5f;
    [SerializeField] private float horizontalOffset = 0f;
    [SerializeField] private float heightOffset = 0f;

    [Header("HOCKEY CHARGE ROTATION")]
    [SerializeField] private float pullbackRotationAngle = 35f;
    [SerializeField] private float pullbackSideAngle = 10f;

    [Header("IDLE ROTATION")]
    [SerializeField] private float rotationAngle = 3f;
    [SerializeField] private float rotationSpeed = 4f;

    [Header("HIT ANIMATION")]
    [SerializeField] private float hitDuration = 0.12f;
    [SerializeField] private float hitRotationAngle = 15f;
    [SerializeField] private float hitOvershoot = 0.15f;

    private GameObject stickInstance;

    private int lastShotCount;
    private bool isHitting;
    private float hitTimer;

    private Vector3 hitStartPosition;
    private Quaternion hitStartRotation;
    private Vector3 hitTargetPosition;
    private Quaternion hitTargetRotation;

    private void Start()
    {
        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();

        if (ballController == null || stickPrefab == null)
        {
            Debug.LogError("StickController: faltan referencias.");
            enabled = false;
            return;
        }

        stickInstance = Instantiate(stickPrefab);
        stickInstance.SetActive(false);

        lastShotCount = ballController.ShotCount;
    }

    private void Update()
    {
        if (ballController == null || stickInstance == null)
            return;

        // Detectar el disparo.
        if (ballController.ShotCount != lastShotCount)
        {
            lastShotCount = ballController.ShotCount;
            StartHit();
        }

        if (isHitting)
        {
            UpdateHit();
            return;
        }

        if (!ballController.IsAiming)
        {
            if (stickInstance.activeSelf)
                stickInstance.SetActive(false);

            return;
        }

        if (!stickInstance.activeSelf)
            stickInstance.SetActive(true);

        UpdateStick();
    }

    private void UpdateStick()
    {
        Vector3 ballPosition = ballController.BallPosition;
        Vector3 direction = ballController.ShootDirection;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        // Fuerza normalizada entre 0 y 1.
        float forcePercentage = ballController.MaxShootSpeed > 0f
            ? Mathf.Clamp01(
                ballController.CurrentShootSpeed /
                ballController.MaxShootSpeed
            )
            : 0f;

        // Carga hacia atrás según la fuerza.
        float pullbackDistance = Mathf.Lerp(
            0f,
            maxPullbackDistance,
            forcePercentage
        );

        // Ejes relativos a la dirección del disparo.
        Vector3 right = Vector3.Cross(
            Vector3.up,
            direction
        ).normalized;

        Vector3 position =
            ballPosition
            - direction * (baseDistance + pullbackDistance)
            + right * horizontalOffset;

        position.y += heightOffset;

        // Rotación base.
        Quaternion baseRotation = Quaternion.LookRotation(
            direction,
            Vector3.up
        );

        // Cuanto más se carga, más rota el stick.
        // Se utiliza el mismo porcentaje que para el retroceso.
        float chargeAngle =
            pullbackRotationAngle * forcePercentage;

        float sideAngle =
            pullbackSideAngle * forcePercentage;

        // Rotación de carga estilo hockey.
        Quaternion chargeRotation =
            Quaternion.Euler(
                chargeAngle,
                0f,
                sideAngle
            );

        // Pequeña oscilación mientras se apunta.
        float wobble =
            Mathf.Sin(Time.unscaledTime * rotationSpeed)
            * rotationAngle;

        Quaternion idleRotation =
            baseRotation
            * chargeRotation
            * Quaternion.Euler(wobble, 0f, 0f);

        stickInstance.transform.SetPositionAndRotation(
            position,
            idleRotation
        );
    }

    private void StartHit()
    {
        Vector3 direction = ballController.LastShootDirection;

        if (direction.sqrMagnitude < 0.0001f)
        {
            stickInstance.SetActive(false);
            return;
        }

        direction.Normalize();

        stickInstance.SetActive(true);

        hitStartPosition = stickInstance.transform.position;
        hitStartRotation = stickInstance.transform.rotation;

        Vector3 ballPosition = ballController.BallPosition;

        Vector3 right = Vector3.Cross(
            Vector3.up,
            direction
        ).normalized;

        // El stick avanza hacia la bola.
        hitTargetPosition =
            ballPosition
            - direction * hitOvershoot
            + right * horizontalOffset;

        hitTargetPosition.y += heightOffset;

        Quaternion baseRotation = Quaternion.LookRotation(
            direction,
            Vector3.up
        );

        // Al golpear, rota hacia delante para acompañar el impacto.
        hitTargetRotation =
            baseRotation *
            Quaternion.Euler(-hitRotationAngle, 0f, 0f);

        hitTimer = 0f;
        isHitting = true;
    }

    private void UpdateHit()
    {
        hitTimer += Time.unscaledDeltaTime;

        float duration = Mathf.Max(0.01f, hitDuration);
        float t = Mathf.Clamp01(hitTimer / duration);

        // Golpe rápido y suave.
        float easedT = t * t * (3f - 2f * t);

        stickInstance.transform.position = Vector3.Lerp(
            hitStartPosition,
            hitTargetPosition,
            easedT
        );

        stickInstance.transform.rotation = Quaternion.Slerp(
            hitStartRotation,
            hitTargetRotation,
            easedT
        );

        if (t >= 1f)
        {
            isHitting = false;
            stickInstance.SetActive(false);
        }
    }*/
}
