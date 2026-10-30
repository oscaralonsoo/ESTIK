using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BallController : MonoBehaviour
{
    [Header("SHOOT PROPERTIES")]
    [SerializeField] private float shootSpeed = 30f;
    [Tooltip("Por debajo de esta distancia de drag (en alturas de pantalla) el disparo se cancela.")]
    [SerializeField] private float minDragDistance = 0.03f;
    [SerializeField] private float maxDragDistance = 0.2f;
    [SerializeField] private float maxAerealDragDistance = 0.3f;
    [SerializeField] private float shootAngle = 30f;

    [Header("TRACE PROPERTIES")]
    [SerializeField] private GameObject tracePrefab;

    [Header("SLOWMO PROPERTIES")]
    [SerializeField] public float slowMotionScale = 0.2f;
    [SerializeField] private float slowmoJoinTransitionDuration = 0.5f;
    [SerializeField] private float slowmoLeaveTransitionDuration = 0f;

    [Header("ZONAS SIN LANZAMIENTO")]
    [Tooltip("Boton de pausa (o un area mayor a su alrededor). Un click que empieza aqui no lanza la bola.")]
    [SerializeField] private RectTransform[] blockedZones;

    [Header("DEBUG")]
    bool initialized = true;
    [SerializeField] public bool draging = false;
    [SerializeField] private bool gestureBlocked = false;
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
    private Camera mainCamera;
    private string currentSkinName;

    private const string SkinKey = "SelectedBall";

    private void Awake()
    {
        initialized = true;

        //TRACE
        GameObject trace = Instantiate(tracePrefab);
        trace.SetActive(false);
        traceBehaviour = trace.GetComponentInChildren<TraceBehaviour>();
        if (!traceBehaviour) initialized = false;
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
        //POST PROCESSING
        postProcessSlowmo = FindAnyObjectByType<DesaturationSlowmo>();
        if (!postProcessSlowmo) initialized = false;

        //BALL RIGIDBODY
        ballRigidbody = GetComponent<Rigidbody>();
        if (!ballRigidbody) initialized = false;

        //SKIN
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

        if (gestureBlocked)
        {
            if (Pointer.current.press.wasReleasedThisFrame) gestureBlocked = false;
            return;
        }

        if (Pointer.current.press.wasPressedThisFrame)
        {
            if (IsInBlockedZone(Pointer.current.position.ReadValue()))
            {
                gestureBlocked = true;
                return;
            }

            if (remainingShots > 0 && grounded)
            {
                draging = true;
                dragStartPosition = Pointer.current.position.ReadValue() / Screen.height;
                dragCurrentPosition = dragStartPosition;
            }
        }

        if (draging)
        {
            dragCurrentPosition = Pointer.current.position.ReadValue() / Screen.height;
            ShowTrace();
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
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
                Shoot();
                OnShoot();
            }
        }

        slowmo = draging || (aerealShot && remainingShots != 0);

        float targetTimeScale = slowmo ? slowMotionScale : 1.0f;
        float currentTransitionDuration = draging ? slowmoJoinTransitionDuration : slowmoLeaveTransitionDuration;
        float changeSpeed = currentTransitionDuration > 0f ? (1f / currentTransitionDuration) * Time.unscaledDeltaTime : 1000f;
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetTimeScale, changeSpeed);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (slowmo) postProcessSlowmo.ActivatePostProcess(); else postProcessSlowmo.DeactivatePostProcess();
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

    void Shoot()
    {
        if (!ballRigidbody) return;

        Vector2 dragDir = dragStartPosition - dragEndPosition;

        currentShootSpeed = GetPower01(dragDir.magnitude) * shootSpeed;

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


    private void OnCollisionStay(Collision other)
    {
        if (other.collider.CompareTag("Floor"))
        {
            grounded = true;
        }
    }
    private void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Floor"))
        {
            grounded = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Area"))
        {
            if (remainingShots != 0)
            {

                aerealShot = true;
                remainingShots = 1;
                if (mainCamera.TryGetComponent<CameraFollow>(out CameraFollow cameraFollow))
                {
                    cameraFollow.SetShooting(true);
                }
            }
        }
    }

    private IEnumerator FailShoot()
    {
        yield return new WaitForSeconds(2f);
        FastSceneLoader.Instance.RestartLevel();
    }

    /*    public bool IsAiming => draging;

        public float CurrentShootSpeed => currentShootSpeed;

        public float MaxShootSpeed => shootSpeed;

        public Vector2 DragDirection => dragStartPosition - dragCurrentPosition;

        public int ShotCount { get; private set; }

        public Vector3 LastShootDirection { get; private set; }

        public Vector3 BallPosition => ballRigidbody != null
            ? ballRigidbody.position
            : transform.position;

        public Vector3 ShootDirection
        {
            get
            {
                if (camera == null)
                    return Vector3.zero;

                Vector3 direction = GetCameraRelativeDirection(DragDirection);

                if (currentShootAngle > 0f && direction.sqrMagnitude > 0.0001f)
                {
                    Vector3 rightAxis = Vector3.Cross(Vector3.up, direction);
                    direction = Quaternion.AngleAxis(-currentShootAngle, rightAxis)
                        * direction;
                }

                return direction.normalized;
            }
        }*/
}