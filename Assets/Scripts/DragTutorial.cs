using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragTutorial : MonoBehaviour
{
    [SerializeField] private TraceBehaviour indicator;
    [SerializeField] private BallController ball;
    [SerializeField] private GoalController goal;
    [SerializeField] private Image drag;

    [SerializeField] private float dragOffset = 0.5f;
    [SerializeField] private float dragDistance = 1.5f;
    [SerializeField] private float animationTime = 2f;
    [SerializeField] private bool aereal = false;

    private Camera mainCamera = null;
    private float timer = 0f;

    void Start()
    {
        mainCamera = Camera.main;

        if (ball == null)
            ball = FindFirstObjectByType<BallController>(FindObjectsInactive.Include);

        if (indicator == null)
            indicator = FindFirstObjectByType<TraceBehaviour>(FindObjectsInactive.Include);

        if (goal == null)
            goal = FindFirstObjectByType<GoalController>(FindObjectsInactive.Include);

        if (!mainCamera || !indicator || !ball || !goal || !drag)
        {
            gameObject.SetActive(false);
            return;
        }

        indicator.gameObject.SetActive(true);
    }

    void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = mainCamera.transform.position + mainCamera.transform.forward.normalized * 5f;
        transform.rotation = mainCamera.transform.rotation;

        float maxProgress = aereal ? 1.5f : 1.0f;
        float totalDuration = animationTime * maxProgress;

        timer = Mathf.PingPong(Time.unscaledTime, totalDuration);
        float currentProgress = timer / animationTime;

        Vector3 directionToGoal = goal.transform.position - ball.transform.position;
        directionToGoal.y = 0f;

        float currentShootAngle = 0f;

        if (aereal && currentProgress > 1.0f)
        {
            float aerialPercentage = Mathf.InverseLerp(1.0f, 1.5f, currentProgress);
            currentShootAngle = Mathf.Lerp(0f, 30f, aerialPercentage);
        }

        Vector3 finalIndicatorDirection = directionToGoal;
        if (currentShootAngle > 0f)
        {
            Vector3 rightAxis = Vector3.Cross(Vector3.up, directionToGoal);
            finalIndicatorDirection = Quaternion.AngleAxis(-currentShootAngle, rightAxis) * directionToGoal;
            indicator.SetColor(1);
        }
        else
        {
            indicator.SetColor(0);
        }

        if (finalIndicatorDirection != Vector3.zero)
            indicator.transform.rotation = Quaternion.LookRotation(finalIndicatorDirection);

        indicator.transform.position = ball.transform.position;
        indicator.SetFillAmount(currentProgress);

        Vector3 directionAwayFromGoal = (ball.transform.position - goal.transform.position);
        directionAwayFromGoal.y = 0f;
        directionAwayFromGoal.Normalize();

        Vector3 worldStart = ball.transform.position + (directionAwayFromGoal * dragOffset);
        Vector3 worldEnd = ball.transform.position + (directionAwayFromGoal * dragDistance);


        Vector3 screenStart3D = mainCamera.WorldToScreenPoint(worldStart);
        Vector3 screenEnd3D = mainCamera.WorldToScreenPoint(worldEnd);


        Vector3 targetScreenPos = Vector3.Lerp(screenStart3D, screenEnd3D, currentProgress / maxProgress);

        RectTransform canvasRect = drag.canvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetScreenPos,
            drag.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out localPoint
        );

        drag.rectTransform.localPosition = localPoint;

        Time.timeScale = 0.01f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}