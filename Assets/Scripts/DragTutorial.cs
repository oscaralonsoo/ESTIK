using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragTutorial : MonoBehaviour
{
    [SerializeField] private TraceBehaviour indicator;
    [SerializeField] private BallController ball;
    [SerializeField] private GoalController goal;
    [SerializeField] private Image drag;

    [SerializeField] private float dragOffset = 0.5f;   // Distancia inicial en el mundo detrás de la pelota
    [SerializeField] private float dragDistance = 1.5f; // Distancia final en el mundo detrás de la pelota
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

    void OnDisable()
    {
        // Restaurar el tiempo normal cuando el tutorial se oculta
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (indicator != null)
        {
            indicator.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
            return;
        }

        // Mantener la UI del Canvas alineada a la cámara
        transform.position = mainCamera.transform.position + mainCamera.transform.forward.normalized * 5f;
        transform.rotation = mainCamera.transform.rotation;

        // 1. Manejo del progreso y rango aéreo
        float maxProgress = aereal ? 1.5f : 1.0f;
        float totalDuration = animationTime * maxProgress;

        timer = Mathf.PingPong(Time.unscaledTime, totalDuration);
        float currentProgress = timer / animationTime;

        // 2. Lógica del Indicador 3D hacia la portería
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

        // 3. POSICIONES DEL DRAG EN EL MUNDO (Detrás de la pelota)
        Vector3 directionAwayFromGoal = (ball.transform.position - goal.transform.position);
        directionAwayFromGoal.y = 0f;
        directionAwayFromGoal.Normalize();

        // Calcular puntos 3D de inicio y fin en la línea de la portería
        Vector3 worldStart = ball.transform.position + (directionAwayFromGoal * dragOffset);
        Vector3 worldEnd = ball.transform.position + (directionAwayFromGoal * dragDistance);

        // 4. CONVERSIÓN A PANTALLA (WorldToScreenPoint)
        Vector3 screenStart3D = mainCamera.WorldToScreenPoint(worldStart);
        Vector3 screenEnd3D = mainCamera.WorldToScreenPoint(worldEnd);

        // Interpolar en pantalla según el progreso actual
        Vector3 targetScreenPos = Vector3.Lerp(screenStart3D, screenEnd3D, currentProgress / maxProgress);

        // Convertir la posición de pantalla a espacio local del Canvas (RectTransform)
        RectTransform canvasRect = drag.canvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetScreenPos,
            drag.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out localPoint
        );

        drag.rectTransform.localPosition = localPoint;

        // Pausa del juego
        Time.timeScale = 0.01f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}