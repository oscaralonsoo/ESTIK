using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [Header("SHOOT PROPERTIES")]
    [SerializeField] private float shootSpeed = 30f;
    [SerializeField] private float maxDragDistance = 0.2f;

    [Header("TRACE PROPERTIES")]
    [SerializeField] private Sprite traceArrowSprite;
    [SerializeField] private Color traceArrowColor = Color.white;
    [SerializeField] private float traceArrowSize = 0.2f;
    [SerializeField] private float arrowAngleOffset = 0f;
    [SerializeField] private bool lieFlatOnGround = true;
    [SerializeField] private int traceSortingOrder = 10;
    [SerializeField] private int maxTraceArrows = 15;
    [SerializeField] private float arrowSpacing = 1f;
    [SerializeField] private float traceDuration = 1.5f;

    [Header("SLOWMO PROPERTIES")]
    [SerializeField] private bool slowmo = true;
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowmoJoinTransitionDuration = 0.5f;
    [SerializeField] private float slowmoLeaveTransitionDuration = 0f;

    [Header("DEBUG")]
    [SerializeField] private bool draging = false;
    [SerializeField] private float currentShootSpeed = 0f;
    [SerializeField] private Vector2 dragStartPosition = Vector2.zero;
    [SerializeField] private Vector2 dragEndPosition = Vector2.zero;
    [SerializeField] private Vector2 dragCurrentPosition = Vector2.zero;
    [SerializeField] private GameObject[] traceArrows;
    [SerializeField] private Rigidbody ballRigidbody = null;


    void Start()
    {
        if (this.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            ballRigidbody = rb;
        }

        CreateTraceArrows();
    }

    void Update()
    {
        if (Mouse.current == null || ballRigidbody == null)
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

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = shootDir * currentShootSpeed;
    }

    void OnShoot()
    {
        HideTrace();

        draging = false;
        dragStartPosition = Vector2.zero;
        dragCurrentPosition = Vector2.zero;
        dragEndPosition = Vector2.zero;
        currentShootSpeed = 0f;
    }

    #region Trace
    private void CreateTraceArrows()
    {
        traceArrows = new GameObject[maxTraceArrows];

        float spriteWidth = traceArrowSprite != null ? traceArrowSprite.bounds.size.x : 1f;
        float scale = spriteWidth > 0f ? traceArrowSize / spriteWidth : 1f;

        for (int i = 0; i < maxTraceArrows; i++)
        {
            GameObject arrow = new GameObject("TraceArrow_" + i);
            arrow.transform.SetParent(transform);
            arrow.transform.localScale = Vector3.one * scale;

            SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
            sr.sprite = traceArrowSprite;
            sr.color = traceArrowColor;
            sr.sortingOrder = traceSortingOrder;

            arrow.SetActive(false);
            traceArrows[i] = arrow;
        }
    }

    private void ShowTrace()
    {
        Vector2 dragDir = dragStartPosition - dragCurrentPosition;

        currentShootSpeed = dragDir.magnitude > maxDragDistance
            ? shootSpeed
            : dragDir.magnitude / maxDragDistance * shootSpeed;

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        if (shootDir.sqrMagnitude < 0.0001f || currentShootSpeed <= 0f)
        {
            HideTrace();
            return;
        }

        float traceLength = currentShootSpeed * traceDuration;
        int arrowsToShow = Mathf.Min(
            maxTraceArrows,
            Mathf.FloorToInt(traceLength / Mathf.Max(arrowSpacing, 0.0001f))
        );

        Vector3 startPosition = ballRigidbody.transform.position;
        Quaternion arrowRotation = GetArrowRotation(shootDir);

        for (int i = 0; i < maxTraceArrows; i++)
        {
            bool visible = i < arrowsToShow;
            traceArrows[i].SetActive(visible);

            if (!visible)
                continue;

            traceArrows[i].transform.SetPositionAndRotation(
                startPosition + shootDir * ((i + 1) * arrowSpacing),
                arrowRotation
            );
        }
    }

    private void HideTrace()
    {
        for (int i = 0; i < traceArrows.Length; i++)
        {
            traceArrows[i].SetActive(false);
        }
    }
    private Quaternion GetArrowRotation(Vector3 shootDir)
    {
        if (lieFlatOnGround)
        {
            Quaternion flat = Quaternion.LookRotation(
                Vector3.down,
                Vector3.Cross(Vector3.down, shootDir)
            );
            return flat * Quaternion.Euler(0f, 0f, arrowAngleOffset);
        }

        Transform cam = Camera.main.transform;
        float angle = Mathf.Atan2(
            Vector3.Dot(shootDir, cam.up),
            Vector3.Dot(shootDir, cam.right)
        ) * Mathf.Rad2Deg;

        return cam.rotation * Quaternion.Euler(0f, 0f, angle + arrowAngleOffset);
    }

    #endregion
}