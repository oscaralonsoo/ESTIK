using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Tutorial1 : MonoBehaviour
{
    [SerializeField] private TraceBehaviour indicator;
    [SerializeField] private BallController ball;
    [SerializeField] private GoalController goal;
    [SerializeField] private Image drag;

    [SerializeField] private float dragDistance = 1f;
    [SerializeField] private float animationTime = 2f;
    [SerializeField] private bool aereal = false;
    [SerializeField] private float dragOffset = 0;

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

        timer = Mathf.PingPong(Time.unscaledTime, animationTime);
        float progress = timer / animationTime;

        float positionY = startPositionDrag.y - progress * dragDistance;
        drag.rectTransform.localPosition = new Vector3(startPositionDrag.x, positionY, startPositionDrag.z);

        float currentShootAngle = 0;

        Vector3 direction = goal.transform.position - ball.transform.position;
        direction.y = 0f;

        if (aereal)
        {
            progress *= 1.5f;
            float aerialPercentage = Mathf.InverseLerp(1, 1.5f, progress);
            currentShootAngle = Mathf.Lerp(0f, Mathf.Abs(30), aerialPercentage);
        }

        if (currentShootAngle > 0f)
        {
            Vector3 rightAxis = Vector3.Cross(Vector3.up, direction);
            direction = Quaternion.AngleAxis(-currentShootAngle, rightAxis) * direction;
            indicator.SetColor(1);
        }
        else indicator.SetColor(0);
        indicator.SetFillAmount(progress);


        if (direction != Vector3.zero)
            indicator.transform.rotation = Quaternion.LookRotation(direction);
        indicator.transform.position = ball.transform.position;

        Time.timeScale = 0.01f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

    }
}