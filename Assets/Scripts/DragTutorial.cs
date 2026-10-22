using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragTutorial : MonoBehaviour
{
    [SerializeField] private TraceBehaviour indicator;
    [SerializeField] private BallController ball;
    [SerializeField] private GoalController goal;
    [SerializeField] private Image drag;

    [SerializeField] private float dragDistance = 150f;
    [SerializeField] private float animationTime = 2f;
    [SerializeField] private bool aereal = false;
    [SerializeField] private float dragOffset = 0f;

    private Camera mainCamera = null;
    private Vector3 startPositionDrag = Vector3.zero;
    private float timer = 0f;

    void Start()
    {
        mainCamera = Camera.main;

        if (ball == null)
            ball = FindAnyObjectByType<BallController>(FindObjectsInactive.Include);

        if (indicator == null)
            indicator = FindAnyObjectByType<TraceBehaviour>(FindObjectsInactive.Include);

        if (goal == null)
            goal = FindAnyObjectByType<GoalController>(FindObjectsInactive.Include);

        if (!mainCamera || !indicator || !ball || !goal || !drag)
        {
            gameObject.SetActive(false);
            return;
        }

        indicator.gameObject.SetActive(true);
        startPositionDrag = drag.rectTransform.localPosition;
        startPositionDrag.y -= dragOffset;
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

        indicator.SetFillAmount(currentProgress);

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

        Vector2 ballScreenPos = mainCamera.WorldToScreenPoint(ball.transform.position);
        Vector2 goalScreenPos = mainCamera.WorldToScreenPoint(goal.transform.position);
        Vector2 screenDirectionAway = (ballScreenPos - goalScreenPos).normalized;

        Vector3 dragMovement = (Vector3)(screenDirectionAway * (currentProgress * dragDistance));
        drag.rectTransform.localPosition = startPositionDrag + dragMovement;

        Time.timeScale = 0.01f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}