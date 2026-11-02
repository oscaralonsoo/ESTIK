using System.Collections;
using UnityEngine;

public class PointCollector : MonoBehaviour
{
    public static PointCollector Instance { get; private set; }

    [Header("Configuración de Prefabs 2D")]
    [SerializeField] private GameObject point2DPrefab;
    [SerializeField] private RectTransform targetUIPoint;

    [Header("Parámetros por Defecto")]
    [SerializeField] private int minPoints = 5;
    [SerializeField] private int maxPoints = 10;
    [SerializeField] private float spreadRadius = 1.5f;

    [Header("Velocidad y Ritmo")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float spawnDelay = 0.05f;
    [SerializeField] private float spawnScaleDuration = 0.1f;

    [Header("Efecto de Curva")]
    [SerializeField] private float curveHeight = 2.0f;

    [Header("Orden de Renderizado (Sprite 2D)")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100;

    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        mainCamera = Camera.main;
    }

    public void SpawnPointsEffect(Vector3 worldSpawnPosition, int customMin = -1, int customMax = -1, float customDuration = -1f)
    {
        int min = customMin != -1 ? customMin : minPoints;
        int max = customMax != -1 ? customMax : maxPoints;
        float finalDuration = customDuration != -1f ? customDuration : duration;
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.ShowScore();
        }

        int multiplier = ComboManager.Instance.CurrentMultiplier;

        StartCoroutine(SpawnRoutine(worldSpawnPosition, min, max, finalDuration, multiplier));
    }

    private IEnumerator SpawnRoutine(Vector3 worldSpawnPosition, int min, int max, float flightDuration, int multiplier)
    {
        if (point2DPrefab == null || targetUIPoint == null || mainCamera == null)
        {
            Debug.LogWarning("PointCollector: Faltan referencias por asignar.");
            yield break;
        }

        int pointsToSpawn = Random.Range(min, max + 1);

        for (int i = 0; i < pointsToSpawn; i++)
        {
            StartCoroutine(AnimatePoint2DRoutine(worldSpawnPosition, flightDuration, multiplier));
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private IEnumerator AnimatePoint2DRoutine(Vector3 worldPos, float flightDuration, int multiplier)
    {
        Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
        Vector3 startWorldPos = worldPos + new Vector3(randomOffset.x, randomOffset.y, 0f);

        GameObject pointInstance = Instantiate(point2DPrefab, startWorldPos, Quaternion.identity);

        SpriteRenderer spriteRenderer = pointInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (mainCamera != null)
        {
            pointInstance.transform.rotation = mainCamera.transform.rotation;
        }

        Vector3 targetScale = pointInstance.transform.localScale;
        pointInstance.transform.localScale = Vector3.zero;
        Vector3 uiScreenPos = RectTransformUtility.WorldToScreenPoint(null, targetUIPoint.position);
        Vector3 targetWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(uiScreenPos.x, uiScreenPos.y, mainCamera.nearClipPlane + 5f));

        Vector3 startPos3D = startWorldPos;
        Vector3 midPoint = (startPos3D + targetWorldPos) / 2f;


        Vector3 curveOffset = new Vector3(Random.Range(-curveHeight, curveHeight) * 0.5f, curveHeight, 0f);
        Vector3 controlPoint = midPoint + curveOffset;


        float spawnElapsedTime = 0f;
        while (spawnElapsedTime < spawnScaleDuration && pointInstance != null)
        {
            spawnElapsedTime += Time.deltaTime;
            float scaleT = spawnElapsedTime / spawnScaleDuration;
            pointInstance.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, Mathf.SmoothStep(0f, 1f, scaleT));
            yield return null;
        }

        if (pointInstance == null) yield break;


        float flightElapsedTime = 0f;

        while (flightElapsedTime < flightDuration && pointInstance != null)
        {
            flightElapsedTime += Time.deltaTime;
            float t = flightElapsedTime / flightDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPos = Mathf.Pow(1f - smoothT, 2f) * startPos3D +
                                 2f * (1f - smoothT) * smoothT * controlPoint +
                                 Mathf.Pow(smoothT, 2f) * targetWorldPos;

            pointInstance.transform.position = currentPos;

            if (mainCamera != null)
            {
                pointInstance.transform.rotation = mainCamera.transform.rotation;
            }

            if (t > 0.5f)
            {
                float shrinkT = (t - 0.5f) * 2f;
                pointInstance.transform.localScale = Vector3.Lerp(targetScale, targetScale * 0.1f, shrinkT);
            }

            yield return null;
        }

        if (pointInstance != null)
        {
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.AddPoints(multiplier);
            }

            Destroy(pointInstance);
        }
    }
}