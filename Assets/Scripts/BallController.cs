using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [Header("SHOOT PROPERTIES")]
    [SerializeField] private float shootSpeed = 30f;
    [SerializeField] private float maxDragDistance = 0.2f;
    [SerializeField] private float maxAerealDragDistance = 0.3f;
    [SerializeField] private float shootAngle = 30f;

    [Header("TRACE PROPERTIES")]
    [SerializeField] private GameObject tracePrefab;

    [Header("SLOWMO PROPERTIES")]

    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowmoJoinTransitionDuration = 0.5f;
    [SerializeField] private float slowmoLeaveTransitionDuration = 0f;

    [Header("DEBUG")]
    bool initialized = true;
    [SerializeField] private bool draging = false;
    [SerializeField] private bool aerealShot = false;
    [SerializeField] private bool slowmo = false;
    [SerializeField] private bool grounded = false;
    [SerializeField] private int remainingShots = 1000;
    [SerializeField] private float currentShootSpeed = 0f;
    [SerializeField] private float currentShootAngle = 0f;
    [SerializeField] private Vector2 dragStartPosition = Vector2.zero;
    [SerializeField] private Vector2 dragEndPosition = Vector2.zero;
    [SerializeField] private Vector2 dragCurrentPosition = Vector2.zero;
    [SerializeField] private GameObject[] traceArrows;
    [SerializeField] private Rigidbody ballRigidbody = null;

    private DesaturationSlowmo postProcessSlowmo;
    private TraceBehaviour traceBehaviour;
    private Camera camera;


    void Start()
    {
        initialized = true;

        //POST PROCESSING
        postProcessSlowmo = FindAnyObjectByType<DesaturationSlowmo>();
        if (!postProcessSlowmo) initialized = false;

        //BALL RIGIDBODY
        ballRigidbody = GetComponent<Rigidbody>();
        if (!ballRigidbody) initialized = false;

        //TRACE
        GameObject trace = Instantiate(tracePrefab);
        trace.SetActive(false);
        traceBehaviour = trace.GetComponentInChildren<TraceBehaviour>();
        if (!traceBehaviour) initialized = false;

        camera = Camera.main;
        if (!camera) initialized = false;
    }

    void Update()
    {
        if (!initialized || Pointer.current == null || ballRigidbody == null)
            return;

        if (Pointer.current.press.wasPressedThisFrame && remainingShots > 0 && grounded)
        {
            draging = true;
        }

        if (draging)
        {
            Vector3 ballScreenPos3D = camera.WorldToScreenPoint(ballRigidbody.position);
            Vector2 ballScreenPos = new Vector2(ballScreenPos3D.x, ballScreenPos3D.y);
            dragStartPosition = ballScreenPos / Screen.height;

            Vector2 rawPos = Pointer.current.position.ReadValue();
            dragCurrentPosition = rawPos / Screen.height;
            ShowTrace();
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }

        if (Pointer.current.press.wasReleasedThisFrame && draging)
        {
            Vector3 ballScreenPos3D = camera.WorldToScreenPoint(ballRigidbody.position);
            Vector2 ballScreenPos = new Vector2(ballScreenPos3D.x, ballScreenPos3D.y);
            dragStartPosition = ballScreenPos / Screen.height;

            Vector2 rawPos = Pointer.current.position.ReadValue();
            dragEndPosition = rawPos / Screen.height;

            Shoot();
            OnShoot();
        }

        slowmo = draging || (aerealShot && remainingShots != 0);

        float targetTimeScale = slowmo ? slowMotionScale : 1.0f;
        float currentTransitionDuration = draging ? slowmoJoinTransitionDuration : slowmoLeaveTransitionDuration;
        float changeSpeed = currentTransitionDuration > 0f ? (1f / currentTransitionDuration) * Time.unscaledDeltaTime : 1000f;
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetTimeScale, changeSpeed);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (slowmo) postProcessSlowmo.ActivatePostProcess(); else postProcessSlowmo.DeactivatePostProcess();
    }

    private Vector3 GetCameraRelativeDirection(Vector2 dragDirection)
    {
        Transform camTransform = camera.transform;

        Vector3 camForward = camTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = camTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 worldDirection = (camForward * dragDirection.y) + (camRight * dragDirection.x);

        return worldDirection.normalized;
    }

    void Shoot()
    {
        if (!ballRigidbody) return;

        Vector2 dragDir = dragStartPosition - dragEndPosition;

        currentShootSpeed = dragDir.magnitude > maxDragDistance
            ? shootSpeed
            : dragDir.magnitude / maxDragDistance * shootSpeed;

        if (aerealShot)
        {
            float aerialPercentage = Mathf.InverseLerp(maxDragDistance, maxAerealDragDistance, dragDir.magnitude);
            currentShootAngle = Mathf.Lerp(0f, Mathf.Abs(shootAngle), aerialPercentage);
        }
        else
        {
            currentShootAngle = 0f;
        }

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        if (currentShootAngle > 0f)
        {
            Vector3 rightAxis = Vector3.Cross(Vector3.up, shootDir);
            shootDir = Quaternion.AngleAxis(-currentShootAngle, rightAxis) * shootDir;
        }

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = shootDir * currentShootSpeed;
    }

    void OnShoot()
    {
        HideTrace();
        remainingShots--;
        draging = false;
        dragStartPosition = Vector2.zero;
        dragCurrentPosition = Vector2.zero;
        dragEndPosition = Vector2.zero;
        currentShootSpeed = 0f;
    }

    #region Trace
    private void ShowTrace()
    {
        Vector2 dragDir = dragStartPosition - dragCurrentPosition;

        currentShootSpeed = dragDir.magnitude > maxDragDistance
            ? shootSpeed
            : dragDir.magnitude / maxDragDistance * shootSpeed;

        if (aerealShot)
        {
            float aerialPercentage = Mathf.InverseLerp(maxDragDistance, maxAerealDragDistance, dragDir.magnitude);
            aerialPercentage = Mathf.Sqrt(aerialPercentage);
            currentShootAngle = Mathf.Lerp(0f, Mathf.Abs(shootAngle), aerialPercentage);

            if (aerialPercentage > 0f) traceBehaviour.SetColor(1);
            else traceBehaviour.SetColor(0);
        }
        else
        {
            currentShootAngle = 0f;
            traceBehaviour.SetColor(0);
        }

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        if (currentShootAngle > 0f)
        {
            Vector3 rightAxis = Vector3.Cross(Vector3.up, shootDir);
            shootDir = Quaternion.AngleAxis(-currentShootAngle, rightAxis) * shootDir;
        }

        if (shootDir.sqrMagnitude < 0.0001f || currentShootSpeed <= 0f)
        {
            HideTrace();
            return;
        }

        float fillPercentatge = currentShootSpeed / shootSpeed;

        Vector3 tracePosition = ballRigidbody.transform.position;
        Quaternion traceRotation = Quaternion.identity;
        if (shootDir != Vector3.zero) traceRotation = Quaternion.LookRotation(shootDir);

        traceBehaviour.SetFillAmount(fillPercentatge);
        traceBehaviour.gameObject.SetActive(true);
        traceBehaviour.transform.SetPositionAndRotation(tracePosition, traceRotation);
        
    }

    private void HideTrace()
    {
        if (traceBehaviour != null)
        {
            traceBehaviour.gameObject.SetActive(false);
        }
    }

    #endregion


    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Floor"))
        {
            grounded = true;
        }
    }
    private void OnCollisionExit(Collision other)
    {
        if(other.collider.CompareTag("Floor"))
        {
            grounded = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Area"))
        {
            aerealShot = true;
            remainingShots = 1;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Area"))
        {
            aerealShot = false;
        }
    }
}