using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Tutorial1 : MonoBehaviour
{
    [SerializeField] private TraceBehaviour indicator;
    [SerializeField] private BallController ball;
    [SerializeField] private Image drag;

    [SerializeField] private float dragDistance = 1f;
    [SerializeField] private float animationTime = 2f;

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

        if (!mainCamera || !indicator || !ball || !drag)
        {
            gameObject.SetActive(false);
            return;
        }

        indicator.gameObject.SetActive(true);
        startPositionDrag = drag.rectTransform.localPosition;
    }

    private void OnEnable()
    {
        ball.draging = true;
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


        indicator.transform.position = ball.transform.position;
        indicator.SetFillAmount(progress);
        
    }
}