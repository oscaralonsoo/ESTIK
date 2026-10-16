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
    [SerializeField] private bool slowmo = true;
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowmoJoinTransitionDuration = 0.5f;
    [SerializeField] private float slowmoLeaveTransitionDuration = 0f;

    [Header("DEBUG")]
    bool initialized = true;
    [SerializeField] private bool draging = false;
    [SerializeField] private bool aerealShot = false;
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


    private void Awake()
    {
       
    }

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
    }

    void Update()
    {
        if (!initialized || Mouse.current == null || ballRigidbody == null || remainingShots <= 0)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            draging = true;
        }

        if (draging)
        {
            Vector3 ballScreenPos3D = Camera.main.WorldToScreenPoint(ballRigidbody.position);
            Vector2 ballScreenPos = new Vector2(ballScreenPos3D.x, ballScreenPos3D.y);
            dragStartPosition = ballScreenPos / Screen.height;

            Vector2 rawPos = Mouse.current.position.ReadValue();
            dragCurrentPosition = rawPos / Screen.height;
            ShowTrace();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && draging)
        {
            Vector3 ballScreenPos3D = Camera.main.WorldToScreenPoint(ballRigidbody.position);
            Vector2 ballScreenPos = new Vector2(ballScreenPos3D.x, ballScreenPos3D.y);
            dragStartPosition = ballScreenPos / Screen.height;

            Vector2 rawPos = Mouse.current.position.ReadValue();
            dragEndPosition = rawPos / Screen.height;

            Shoot();
            OnShoot();
        }

        if (slowmo)
        {
            float targetTimeScale = draging ? slowMotionScale : 1.0f;
            float currentTransitionDuration = draging ? slowmoJoinTransitionDuration : slowmoLeaveTransitionDuration;
            float changeSpeed = currentTransitionDuration > 0f ? (1f / currentTransitionDuration) * Time.unscaledDeltaTime : 1000f;
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetTimeScale, changeSpeed);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            if (draging) postProcessSlowmo.ActivatePostProcess(); else postProcessSlowmo.DeactivatePostProcess();
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector2 dragDirection)
    {
        Transform camTransform = Camera.main.transform;

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

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Area")
        {
            aerealShot = true;
            remainingShots = 1;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Area")
        {
            aerealShot = false;
        }
    }
}