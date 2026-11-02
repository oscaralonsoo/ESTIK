using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BallController : MonoBehaviour
{
    [Header("SHOOT PROPERTIES")]
    [SerializeField] private float shootSpeed = 30f;
    [SerializeField] private float minDragDistance = 0.03f;
    [SerializeField] private float maxDragDistance = 0.2f;
    [SerializeField] private float maxAerealDragDistance = 0.3f;
    [SerializeField] private float shootAngle = 30f;
    [SerializeField] private float shootMovingSpeed = 5f;
    [SerializeField] private float shootDelay = 1f;

    [Header("TRACE PROPERTIES")]
    [SerializeField] private GameObject tracePrefab;
    [SerializeField] private GameObject traceShadowPrefab;

    [Header("STICK PROPERTIES")]
    [SerializeField] private GameObject stickPrefab;
    [SerializeField] private Vector3 stickDistance = new Vector3(1.469f, 1.597f, 0.5f);
    [SerializeField] private float stickHitDuration = 0.1f;
    [SerializeField] private float stickFollowThrough = 0.05f;

    [Header("AUDIO")]
    [SerializeField] private BallSoundController ballSoundController;

    [Header("SLOWMO PROPERTIES")]
    [SerializeField] public float slowMotionScale = 0.2f;
    [SerializeField] private float slowmoJoinTransitionDuration = 0.5f;
    [SerializeField] public float slowMotionAerealScale = 0.2f;
    [SerializeField] private float slowmoAerealJoinTransitionDuration = 0.1f;
    [SerializeField] private float slowmoLeaveTransitionDuration = 0f;

    [Header("GROUND CHECK")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float maxGroundAngle = 10f;

    [Header("ZONAS SIN LANZAMIENTO")]
    [SerializeField] private RectTransform[] blockedZones;

    [Header("GROUND FRICTION")]
    [SerializeField] private float groundLinearDamping = 1.5f;
    [SerializeField] private float groundAngularDamping = 1.0f;
    [SerializeField] private float shootLinearDamping = 0.75f;
    [SerializeField] private float shootAngularDamping = 0.5f;
    [SerializeField] private float airLinearDamping = 0f;
    [SerializeField] private float airAngularDamping = 0.05f;

    [Header("DEBUG")]
    bool initialized = true;
    [SerializeField] public bool draging = false;
    [SerializeField] private bool isShooting = false;
    [SerializeField] private bool gestureBlocked = false;
    [SerializeField] private bool aerealShot = false;
    [SerializeField] private bool goal = false;
    [SerializeField] private bool slowmo = false;
    [SerializeField] private bool grounded = false;
    [SerializeField] private int remainingShots = 1000;
    [SerializeField] private float currentShootSpeed = 0f;
    [SerializeField] private float currentShootAngle = 0f;
    [SerializeField] private float currentGroundAngle = 0f;
    [SerializeField] private Vector2 dragStartPosition = Vector2.zero;
    [SerializeField] private Vector2 dragEndPosition = Vector2.zero;
    [SerializeField] private Vector2 dragCurrentPosition = Vector2.zero;
    [SerializeField] private GameObject[] traceArrows;
    [SerializeField] private Rigidbody ballRigidbody = null;

    private DesaturationSlowmo postProcessSlowmo;
    private TraceBehaviour traceBehaviour;
    private GameObject traceShadowObj;
    private StickBehaviour stickBehaviour;
    private Camera mainCamera;
    private string currentSkinName;
    private bool isOnRamp = false;

    [SerializeField] private float currentSlowmoScale = 0.2f;
    [SerializeField] private float currentSlowmoJoinTransitionDuration = 0.5f;

    private Coroutine shootCoroutine;
    private const string SkinKey = "SelectedBall";

    private void Awake()
    {
        initialized = true;

        GameObject trace = Instantiate(tracePrefab);
        trace.SetActive(false);
        traceBehaviour = trace.GetComponentInChildren<TraceBehaviour>();
        if (!traceBehaviour) initialized = false;

        traceShadowObj = Instantiate(traceShadowPrefab);
        traceShadowObj.SetActive(false);

        GameObject stick = Instantiate(stickPrefab);
        stick.SetActive(false);
        stickBehaviour = stick.GetComponentInChildren<StickBehaviour>();
        if (!stickBehaviour) initialized = false;

        currentSlowmoScale = slowMotionScale;
        currentSlowmoJoinTransitionDuration = slowmoJoinTransitionDuration;
    }

    private void OnEnable()
    {
        BallSkinController.SkinChanged += OnSkinChanged;
    }

    private void OnDisable()
    {
        BallSkinController.SkinChanged -= OnSkinChanged;
    }

    void Start()
    {
        postProcessSlowmo = FindAnyObjectByType<DesaturationSlowmo>();
        if (!postProcessSlowmo) initialized = false;

        ballRigidbody = GetComponent<Rigidbody>();
        if (!ballRigidbody) initialized = false;

        if (ballSoundController == null)
        {
            ballSoundController = GetComponent<BallSoundController>();
        }

        LoadSavedSkin();
        mainCamera = Camera.main;
        if (mainCamera == null) initialized = false;
    }

    void Update()
    {
        if (!initialized || Pointer.current == null || ballRigidbody == null)
            return;

        if (PauseMenuManager.GamePaused)
        {
            CancelAim();
            gestureBlocked = false;
            return;
        }

        if (!isShooting)
        {

            if (gestureBlocked)
            {
                if (Pointer.current.press.wasReleasedThisFrame) gestureBlocked = false;
            }
            else
            {
                if (Pointer.current.press.wasPressedThisFrame)
                {
                    if (IsInBlockedZone(Pointer.current.position.ReadValue()))
                    {
                        gestureBlocked = true;
                    }
                    else if (remainingShots > 0 && grounded)
                    {
                        draging = true;
                        Vector2 ballScreenPos = mainCamera.WorldToScreenPoint(ballRigidbody.position);
                        dragStartPosition = ballScreenPos / Screen.height;
                        dragCurrentPosition = dragStartPosition;
                    }
                }

                if (draging)
                {
                    dragCurrentPosition = Pointer.current.position.ReadValue() / Screen.height;
                    ShowTrace();
                    ShowStick();
                }

                if (Pointer.current.press.wasReleasedThisFrame && draging)
                {
                    dragEndPosition = Pointer.current.position.ReadValue() / Screen.height;

                    if (GetPower01((dragStartPosition - dragEndPosition).magnitude) <= 0f)
                    {
                        CancelAim();
                    }
                    else
                    {
                        isShooting = true;
                        draging = false;
                        shootCoroutine = StartCoroutine(PerformShootSequence());
                    }
                }
            }
        }

        slowmo = draging || (aerealShot && remainingShots != 0);

        float targetTimeScale = slowmo ? currentSlowmoScale : 1.0f;
        float currentTransitionDuration = slowmo ? currentSlowmoJoinTransitionDuration : slowmoLeaveTransitionDuration;
        float changeSpeed = currentTransitionDuration > 0f ? (1f / currentTransitionDuration) * Time.unscaledDeltaTime : 1000f;
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetTimeScale, changeSpeed);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (slowmo) postProcessSlowmo.ActivatePostProcess(); else postProcessSlowmo.DeactivatePostProcess();
    }
    private void FixedUpdate()
    {
        if (!initialized || ballRigidbody == null) return;

        grounded = false;
        currentGroundAngle = 0f;

        if (Physics.Raycast(ballRigidbody.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            currentGroundAngle = Vector3.Angle(hit.normal, Vector3.up);
            grounded = currentGroundAngle <= maxGroundAngle;
        }

        if (aerealShot && grounded)
        {
            ballRigidbody.linearDamping = shootLinearDamping;
            ballRigidbody.angularDamping = shootAngularDamping;
        }
        else if (isOnRamp)
        {
            ballRigidbody.linearDamping = shootLinearDamping;
            ballRigidbody.angularDamping = shootAngularDamping;
        }
        else if (grounded)
        {
            ballRigidbody.linearDamping = groundLinearDamping;
            ballRigidbody.angularDamping = groundAngularDamping;
        }
        else
        {
            ballRigidbody.linearDamping = airLinearDamping;
            ballRigidbody.angularDamping = airAngularDamping;
        }
    }

    private float GetPower01(float dragMagnitude)
    {
        return Mathf.InverseLerp(minDragDistance, maxDragDistance, dragMagnitude);
    }

    private bool IsInBlockedZone(Vector2 screenPos)
    {
        if (blockedZones == null) return false;

        foreach (var zone in blockedZones)
        {
            if (zone == null || !zone.gameObject.activeInHierarchy) continue;

            Camera cam = null;
            Canvas canvas = zone.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas = canvas.rootCanvas;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    cam = canvas.worldCamera;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(zone, screenPos, cam))
                return true;
        }

        return false;
    }

    private void CancelAim()
    {
        if (!draging) return;

        HideTrace();
        HideStick();
        draging = false;
        dragStartPosition = Vector2.zero;
        dragCurrentPosition = Vector2.zero;
        dragEndPosition = Vector2.zero;
        currentShootSpeed = 0f;
    }

    private Vector3 GetCameraRelativeDirection(Vector2 dragDirection)
    {
        Transform camTransform = mainCamera.transform;

        Vector3 camForward = camTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = camTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 worldDirection = (camForward * dragDirection.y) + (camRight * dragDirection.x);

        return worldDirection.normalized;
    }

    private IEnumerator PerformShootSequence()
    {
        isShooting = true;
        Vector2 dragDir = dragStartPosition - dragEndPosition;
        currentShootSpeed = GetPower01(dragDir.magnitude) * shootSpeed;
        float fillPercentage = currentShootSpeed / shootSpeed;

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        if (aerealShot)
        {
            float aerialPercentage = Mathf.InverseLerp(maxDragDistance, maxAerealDragDistance, dragDir.magnitude);
            currentShootAngle = Mathf.Lerp(0f, Mathf.Abs(shootAngle), aerialPercentage);
            if (mainCamera.TryGetComponent<CameraFollow>(out CameraFollow cameraFollow))
            {
                cameraFollow.LockCamera();
                StartCoroutine(FailShoot());
            }
        }
        else
        {
            currentShootAngle = 0f;
        }

        if (currentShootAngle > 0f)
        {
            Vector3 rightAxis = Vector3.Cross(Vector3.up, shootDir);
            shootDir = Quaternion.AngleAxis(-currentShootAngle, rightAxis) * shootDir;
        }

        HideTrace();

        if (stickBehaviour != null && stickBehaviour.gameObject.activeInHierarchy)
        {
            float elapsed = 0f;
            Quaternion aimRotation = Quaternion.LookRotation(-GetCameraRelativeDirection(dragDir));
            Vector3 stickBasePos = ballRigidbody.transform.position + (aimRotation * stickDistance);

            while (elapsed < stickHitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / stickHitDuration;

                float currentAnimPercent = Mathf.Lerp(fillPercentage, 0f, t);

                stickBehaviour.SetAimAndFill(stickBasePos, aimRotation, currentAnimPercent);
                yield return null;
            }
        }

        if (ballSoundController != null)
        {
            ballSoundController.PlayHitSound(fillPercentage);
        }

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = shootDir * currentShootSpeed;

        yield return new WaitForSecondsRealtime(stickFollowThrough);

        HideStick();
        remainingShots--;
        draging = false;
        isShooting = false;

        dragStartPosition = Vector2.zero;
        dragCurrentPosition = Vector2.zero;
        dragEndPosition = Vector2.zero;
        currentShootSpeed = 0f;
    }

    private IEnumerator FailShoot()
    {
        yield return new WaitForSeconds(2f);

        if (goal) yield break;
        if (FastSceneLoader.Instance == null) yield break;
        if (FastSceneLoader.Instance.GoalScored) yield break;

        FastSceneLoader.Instance.RestartLevel();
    }

    private IEnumerator ShootDelay()
    {
        isShooting = true;
        yield return new WaitForSecondsRealtime(shootDelay);
        isShooting = false;
    }

    #region Skin
    private void OnSkinChanged(string skinName)
    {
        LoadSavedSkin();
    }

    private void LoadSavedSkin()
    {
        string savedSkinName = PlayerPrefs.GetString(SkinKey, "");
        if (string.IsNullOrEmpty(savedSkinName)) savedSkinName = "BallDefault";

        if (savedSkinName == currentSkinName) return;

        GameObject prefab = Resources.Load<GameObject>(savedSkinName);

        if (prefab == null)
        {
            savedSkinName = "BallDefault";
            prefab = Resources.Load<GameObject>(savedSkinName);
        }

        if (prefab == null) return;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        Instantiate(prefab, transform);
        currentSkinName = savedSkinName;
    }
    #endregion

    #region Trace
    private void ShowTrace()
    {
        Vector2 dragDir = dragStartPosition - dragCurrentPosition;

        currentShootSpeed = GetPower01(dragDir.magnitude) * shootSpeed;

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
        Vector3 tracePosition = ballRigidbody.transform.position;

        if (currentShootAngle > 0f)
        {
            traceShadowObj.SetActive(true);
            traceShadowObj.transform.SetPositionAndRotation(tracePosition, Quaternion.LookRotation(shootDir));
            Vector3 rightAxis = Vector3.Cross(Vector3.up, shootDir);
            shootDir = Quaternion.AngleAxis(-currentShootAngle, rightAxis) * shootDir;
        }
        else
        {
            traceShadowObj.SetActive(false);
        }

        if (shootDir.sqrMagnitude < 0.0001f || currentShootSpeed <= 0f)
        {
            HideTrace();
            return;
        }

        float fillPercentatge = currentShootSpeed / shootSpeed;


        Quaternion traceRotation = Quaternion.identity;
        if (shootDir != Vector3.zero) traceRotation = Quaternion.LookRotation(shootDir);

        traceBehaviour.SetFillAmount(fillPercentatge);
        traceBehaviour.gameObject.SetActive(true);
        traceBehaviour.transform.SetPositionAndRotation(tracePosition, traceRotation);
    }

    private void HideTrace()
    {
        if (traceBehaviour)
        {
            traceBehaviour.gameObject.SetActive(false);
        }
        if (traceShadowObj)
        {
            traceShadowObj.SetActive(false);
        }
    }

    #endregion

    #region Stick
    private void ShowStick()
    {
        if (!stickBehaviour) return;

        Vector2 dragDir = dragStartPosition - dragCurrentPosition;

        currentShootSpeed = GetPower01(dragDir.magnitude) * shootSpeed;
        float fillPercentatge = currentShootSpeed / shootSpeed;

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        if (shootDir.sqrMagnitude < 0.0001f || currentShootSpeed <= 0f)
        {
            HideStick();
            return;
        }

        Quaternion aimRotation = Quaternion.identity;
        if (shootDir != Vector3.zero)
        {
            aimRotation = Quaternion.LookRotation(-shootDir);
        }

        Vector3 stickPosition = ballRigidbody.transform.position + (aimRotation * stickDistance);

        stickBehaviour.gameObject.SetActive(true);
        stickBehaviour.SetAimAndFill(stickPosition, aimRotation, fillPercentatge);
    }

    private void HideStick()
    {
        if (stickBehaviour != null)
        {
            stickBehaviour.gameObject.SetActive(false);
        }
    }
    #endregion




    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Area"))
        {
            StopCoroutine(shootCoroutine);
            isShooting = false;
            draging = false;
            HideTrace();
            HideStick();

            Vector3 currentVelocity = ballRigidbody.linearVelocity.normalized;
            currentVelocity *= shootMovingSpeed;
            ballRigidbody.linearVelocity = currentVelocity;

            currentSlowmoScale = slowMotionAerealScale;
            currentSlowmoJoinTransitionDuration = slowmoAerealJoinTransitionDuration;
            aerealShot = true;
            remainingShots = 1;
            gestureBlocked = false;

            if (mainCamera.TryGetComponent<CameraFollow>(out CameraFollow cameraFollow))
            {
                cameraFollow.SetShooting(true);
            }
            StartCoroutine(ShootDelay());
        }

        if (other.CompareTag("Goal"))
        {
            goal = true;

            if (FastSceneLoader.Instance != null)
            {
                FastSceneLoader.Instance.NotifyGoalScored();
            }
        }

        if (other.gameObject.CompareTag("Ramp"))
        {
            isOnRamp = true;
        }

        if (other.CompareTag("Post"))
        {
            if (ballSoundController != null)
            {
                ballSoundController.PlayPostSound(ballRigidbody.linearVelocity.magnitude / shootSpeed);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Ramp"))
        {
            isOnRamp = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wood"))
        {
            if (ballSoundController != null)
            {
                ballSoundController.PlayWoodSound(collision.relativeVelocity.magnitude / shootSpeed);
            }
        }
    }
}