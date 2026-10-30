using System.Collections;
using UnityEngine;

public class PurchaseCollector : MonoBehaviour
{
    public static PurchaseCollector Instance { get; private set; }

    [Header("Configuración de Cámara")]
    [SerializeField] private Camera customCamera;

    [Header("Configuración de Prefabs 2D")]
    [SerializeField] private GameObject point2DPrefab;
    [SerializeField] private RectTransform sourceUIPoint;

    [Header("Parámetros de Tamaño y Valor")]
    [SerializeField] private float globalSizeMultiplier = 0.6f;
    [SerializeField] private float bigPointScaleMultiplier = 1.5f;

    [Header("Configuración Dinámica por Precio")]
    [SerializeField] private float baseDuration = 0.5f;
    [SerializeField] private float maxDuration = 2.5f;        // Duración máxima para que no se eternice aunque cueste mucho
    [SerializeField] private float baseSpreadRadius = 0.6f;   // Radio de dispersión amplio durante el vuelo
    [SerializeField] private float finalClusterRadius = 0.12f; // <--- NUEVO: Radio muy pequeño al final para que se junten compactos en la bola
    [SerializeField] private int referenceMaxPrice = 500;     // Precio de referencia para alcanzar el máximo de velocidad/extensión

    [Header("Efecto de Curva")]
    [SerializeField] private float curveHeight = 2.0f;

    [Header("Orden de Renderizado (Sprite 2D)")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100;

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

        if (customCamera == null)
        {
            customCamera = Camera.main;
        }
    }

    public void SpawnPurchaseEffect(Vector3 targetWorldPosition, int skinPrice, System.Action onComplete = null)
    {
        int bigPointsCount = skinPrice / 5;
        int normalPointsCount = skinPrice % 5;
        int totalItemsCount = bigPointsCount + normalPointsCount;

        // Factor de precio normalizado (de 0 a 1 según lo caro que sea respecto al precio de referencia)
        float priceFactor = Mathf.Clamp01((float)skinPrice / referenceMaxPrice);

        // 1. Duración más ágil y rápida pero escalada según el precio
        float dynamicDuration = Mathf.Lerp(baseDuration, maxDuration, priceFactor * 0.7f);

        // 2. Sincronizamos el texto del marcador con esta misma duración
        // Sincronizamos el texto pasando únicamente el precio de la skin
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.AnimateScoreSpend(skinPrice);
        }

        StartCoroutine(SpawnRoutine(targetWorldPosition, bigPointsCount, normalPointsCount, dynamicDuration, skinPrice, onComplete));
    }

    private IEnumerator SpawnRoutine(Vector3 targetWorldPosition, int bigPoints, int normalPoints, float flightDuration, int skinPrice, System.Action onComplete)
    {
        if (point2DPrefab == null || sourceUIPoint == null || customCamera == null)
        {
            Debug.LogWarning("PurchaseCollector: Faltan referencias por asignar (Prefab, UI Point o Cámara).");
            onComplete?.Invoke();
            yield break;
        }

        Vector3 uiScreenPos = RectTransformUtility.WorldToScreenPoint(customCamera, sourceUIPoint.position);
        Vector3 startWorldPos = customCamera.ScreenToWorldPoint(new Vector3(uiScreenPos.x, uiScreenPos.y, customCamera.nearClipPlane + 5f));

        int totalItems = bigPoints + normalPoints;
        if (totalItems <= 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        // Intervalo de salida más fluido y rápido
        float calculatedSpawnDelay = Mathf.Max(0.01f, (flightDuration * 0.6f) / totalItems);

        for (int i = 0; i < bigPoints; i++)
        {
            StartCoroutine(AnimatePointRoutine(startWorldPos, targetWorldPosition, flightDuration, skinPrice, true));
            yield return new WaitForSeconds(calculatedSpawnDelay);
        }

        for (int i = 0; i < normalPoints; i++)
        {
            StartCoroutine(AnimatePointRoutine(startWorldPos, targetWorldPosition, flightDuration, skinPrice, false));
            yield return new WaitForSeconds(calculatedSpawnDelay);
        }

        yield return new WaitForSeconds(flightDuration);
        onComplete?.Invoke();
    }

    private IEnumerator AnimatePointRoutine(Vector3 startWorldPos, Vector3 targetWorldPos, float duration, int skinPrice, bool isBigPoint)
    {
        GameObject pointInstance = Instantiate(point2DPrefab, startWorldPos, Quaternion.identity);

        SpriteRenderer spriteRenderer = pointInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (customCamera != null)
        {
            pointInstance.transform.rotation = customCamera.transform.rotation;
        }

        Vector3 originalPrefabScale = pointInstance.transform.localScale;
        Vector3 scaledBaseSize = originalPrefabScale * globalSizeMultiplier;
        Vector3 targetScale = isBigPoint ? scaledBaseSize * bigPointScaleMultiplier : scaledBaseSize;

        pointInstance.transform.localScale = Vector3.zero;

        // Offset aleatorio inicial (durante el vuelo se dispersan con amplitud)
        Vector2 flightOffset2D = UnityEngine.Random.insideUnitCircle * baseSpreadRadius;
        Vector3 midTargetPos = targetWorldPos + new Vector3(flightOffset2D.x, flightOffset2D.y, 0f);

        // Offset final muy pequeño para que al llegar a la bola se agrupen y junten mucho más
        Vector2 finalOffset2D = UnityEngine.Random.insideUnitCircle * finalClusterRadius;
        Vector3 finalTargetPos = targetWorldPos + new Vector3(finalOffset2D.x, finalOffset2D.y, 0f);

        Vector3 midPoint = (startWorldPos + midTargetPos) / 2f;
        float priceFactor = Mathf.Clamp01((float)skinPrice / referenceMaxPrice);
        float dynamicCurveHeight = Mathf.Lerp(curveHeight, curveHeight * 1.5f, priceFactor);
        Vector3 curveOffset = new Vector3(UnityEngine.Random.Range(-dynamicCurveHeight, dynamicCurveHeight) * 0.5f, dynamicCurveHeight, 0f);
        Vector3 controlPoint = midPoint + curveOffset;

        float spawnScaleDuration = 0.12f;
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

        while (flightElapsedTime < duration && pointInstance != null)
        {
            flightElapsedTime += Time.deltaTime;
            float t = flightElapsedTime / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Transición suave desde el punto de dispersión en el aire hacia el clúster compacto final
            Vector3 dynamicTargetPos = Vector3.Lerp(midTargetPos, finalTargetPos, smoothT);

            Vector3 currentPos = Mathf.Pow(1f - smoothT, 2f) * startWorldPos +
                                   2f * (1f - smoothT) * smoothT * controlPoint +
                                   Mathf.Pow(smoothT, 2f) * dynamicTargetPos;

            pointInstance.transform.position = currentPos;

            if (customCamera != null)
            {
                pointInstance.transform.rotation = customCamera.transform.rotation;
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
            Destroy(pointInstance);
        }
    }
}