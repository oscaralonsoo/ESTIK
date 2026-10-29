using System.Collections;
using UnityEngine;

public class PointCollector : MonoBehaviour
{
    [Header("Configuración de Prefabs 3D")]
    public GameObject point3DPrefab;     // El prefab 3D
    public RectTransform targetUIPoint;  // El UI destino en el Canvas

    [Header("Parámetros por Defecto")]
    public int minPoints = 5;            // Mínimo por defecto
    public int maxPoints = 10;           // Máximo por defecto
    public float spreadRadius = 1.5f;    // Radio de dispersión inicial

    [Header("Velocidad y Ritmo")]
    public float duration = 0.2f;        // Tiempo que tarda en volar (Menor = más rápido)
    public float spawnDelay = 0.01f;     // Retraso entre puntos
    public float spawnScaleDuration = 0.1f; // Tiempo de crecimiento al nacer

    [Header("Efecto de Curva")]
    public float curveHeight = 2.0f;     // Altura o desvío de la curva durante el vuelo

    public float zDepthToCamera = 5f;    // Distancia Z frente a la cámara


    public void SpawnPointsEffect(Vector3 worldSpawnPosition, int customMin = -1, int customMax = -1, float customDuration = -1f)
    {
        int min = customMin != -1 ? customMin : minPoints;
        int max = customMax != -1 ? customMax : maxPoints;
        float finalDuration = customDuration != -1f ? customDuration : duration;

        StartCoroutine(SpawnRoutine(worldSpawnPosition, min, max, finalDuration));
    }

    private IEnumerator SpawnRoutine(Vector3 worldSpawnPosition, int min, int max, float flightDuration)
    {
        if (point3DPrefab == null || targetUIPoint == null || Camera.main == null)
        {
            yield break;
        }

        int pointsToSpawn = Random.Range(min, max + 1);

        for (int i = 0; i < pointsToSpawn; i++)
        {
            StartCoroutine(AnimatePoint3DRoutine(worldSpawnPosition, flightDuration));
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private IEnumerator AnimatePoint3DRoutine(Vector3 worldPos, float flightDuration)
    {
        Vector3 randomOffset = Random.insideUnitSphere * spreadRadius;
        Vector3 startWorldPos = worldPos + randomOffset;

        GameObject pointInstance = Instantiate(point3DPrefab, startWorldPos, Random.rotation);
        Vector3 targetScale = pointInstance.transform.localScale;
        pointInstance.transform.localScale = Vector3.zero;

        Vector3 uiScreenPos = RectTransformUtility.WorldToScreenPoint(null, targetUIPoint.position);
        uiScreenPos.z = zDepthToCamera;
        Vector3 targetWorldPos = Camera.main.ScreenToWorldPoint(uiScreenPos);

        Vector3 midPoint = (startWorldPos + targetWorldPos) / 2f;

        Vector3 curveOffset = Random.insideUnitSphere * (curveHeight * 0.5f);
        curveOffset.y += curveHeight;
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

            Vector3 currentPos = Mathf.Pow(1f - smoothT, 2f) * startWorldPos +
                                 2f * (1f - smoothT) * smoothT * controlPoint +
                                 Mathf.Pow(smoothT, 2f) * targetWorldPos;

            pointInstance.transform.position = currentPos;

            if (t > 0.5f)
            {
                float shrinkT = (t - 0.5f) * 2f;
                pointInstance.transform.localScale = Vector3.Lerp(targetScale, targetScale * 0.1f, shrinkT);
            }

            yield return null;
        }

        if (pointInstance != null)
        {
            // --- SUMAR AL MARCADOR AL LLEGAR ---
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.AddPoints(1);
            }
            // -----------------------------------

            Destroy(pointInstance);
        }
    }
}