using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelMapGenerator : MonoBehaviour
{
    [Header("Prefabs de UI")]
    public GameObject levelIconPrefab;
    public GameObject startBkgPrefab;
    public GameObject midBkgPrefab;
    public GameObject endBkgPrefab;
    public GameObject pathSegmentPrefab;
    public GameObject recordPrefab;

    [Header("Contenedor")]
    public Transform contentParent;
    public Transform backgroundParent;
    public Transform pathParent;
    public Transform recordParent;

    [Header("Selección / Scroll")]
    public ScrollRect scrollRect;
    public float centerDuration = 0.5f;
    public GameObject outlinePrefab;
    public Transform outlineParent;
    public float outlineFadeDuration = 0.2f;

    [Header("Audio")]
    public UISFXManager sfx;

    [Header("Configuración del record")]
    public float recordPadding = 300f;

    [Header("Configuración del Selector")]
    public int maxLevels = 50;
    public int showEveryNumLevels = 5;
    public float levelPadding = 250f;
    public float maxHorizontalOffset = 150f;
    public float lvlsTopAndBottonPadding = 100f;

    [Header("Configuración del Bkg")]
    public float bkgTopAndBottonPadding = 100f;

    [Header("Generación Procedural")]
    public int mapSeed = 12345;

    [Header("Configuración del Trazado Curvo")]
    public float segmentSpacing = 25f;
    public int pointsPerSegment = 20;

    private List<Vector3> nodePositions = new List<Vector3>();
    private readonly Dictionary<int, LevelIcon> icons = new Dictionary<int, LevelIcon>();

    private float panelHeight = 0;
    private int record = 1;

    private LevelIcon selected;
    private Transform outline;
    private CanvasGroup outlineGroup;
    private Coroutine centerRoutine;
    private Coroutine outlineRoutine;
    private bool generated;

    void OnEnable()
    {
        LevelIcon.Selected += HandleSelected;

        if (generated) CenterOn(selected, true);
    }

    void OnDisable()
    {
        LevelIcon.Selected -= HandleSelected;
    }

    void Start()
    {
        record = PlayerPrefs.GetInt("Record", 1);

        GenerateBackground();
        GenerateNodes();
        GenerateCurvedPath();
        generated = true;

        SelectInitial();
    }

    void SelectInitial()
    {
        int sel = PlayerPrefs.GetInt("SelectedLevel", 0);
        int key = (sel / showEveryNumLevels) * showEveryNumLevels;

        if (!icons.TryGetValue(key, out LevelIcon icon))
            icons.TryGetValue(0, out icon);

        if (icon == null) return;

        selected = icon;
        CenterOn(selected, true);
    }

    void HandleSelected(LevelIcon icon)
    {
        selected = icon;
        MoveOutlineWithFade(icon);
        CenterOn(selected, false);
    }
    void EnsureOutline()
    {
        if (outline != null || outlinePrefab == null) return;

        Transform parent = outlineParent != null ? outlineParent : contentParent;
        outline = Instantiate(outlinePrefab, parent).transform;
        outline.name = "SelectionOutline";

        outlineGroup = outline.GetComponent<CanvasGroup>();
        if (outlineGroup == null) outlineGroup = outline.gameObject.AddComponent<CanvasGroup>();
        outlineGroup.interactable = false;
        outlineGroup.blocksRaycasts = false;
    }
    void PlaceOutline(LevelIcon icon)
    {
        if (icon == null) return;

        EnsureOutline();
        if (outline == null) return;

        if (outlineRoutine != null) StopCoroutine(outlineRoutine);
        outline.position = icon.transform.position;
        outlineGroup.alpha = 1f;
    }
    void MoveOutlineWithFade(LevelIcon icon)
    {
        if (icon == null) return;

        EnsureOutline();
        if (outline == null) return;

        if (outlineRoutine != null) StopCoroutine(outlineRoutine);
        outlineRoutine = StartCoroutine(OutlineFadeRoutine(icon));
    }

    IEnumerator OutlineFadeRoutine(LevelIcon icon)
    {
        yield return FadeOutline(outlineGroup.alpha, 0f);
        outline.position = icon.transform.position;
        yield return FadeOutline(0f, 1f);
    }

    IEnumerator FadeOutline(float from, float to)
    {
        float duration = Mathf.Max(0.01f, outlineFadeDuration);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            outlineGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        outlineGroup.alpha = to;
    }
    void CenterOn(LevelIcon icon, bool instant)
    {
        if (icon == null || scrollRect == null || !isActiveAndEnabled) return;

        if (centerRoutine != null) StopCoroutine(centerRoutine);
        centerRoutine = StartCoroutine(CenterRoutine(icon, instant));
    }

    IEnumerator CenterRoutine(LevelIcon icon, bool instant)
    {

        yield return null;
        if (instant) PlaceOutline(icon);

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport != null
            ? scrollRect.viewport
            : (RectTransform)scrollRect.transform;

        float scrollable = content.rect.height - viewport.rect.height;
        if (scrollable <= 0f) yield break;

        float nodeFromBottom = content.InverseTransformPoint(icon.transform.position).y - content.rect.yMin;
        float target = Mathf.Clamp01((nodeFromBottom - viewport.rect.height * 0.5f) / scrollable);

        scrollRect.StopMovement();

        if (instant || centerDuration <= 0f)
        {
            scrollRect.verticalNormalizedPosition = target;
            yield break;
        }

        float start = scrollRect.verticalNormalizedPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / centerDuration;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        scrollRect.verticalNormalizedPosition = target;
    }
    void GenerateNodes()
    {
        Random.InitState(mapSeed);
        nodePositions.Clear();
        icons.Clear();

        float levelStartPosY = lvlsTopAndBottonPadding;
        float levelEndPosY = panelHeight - lvlsTopAndBottonPadding;

        int totalLevels = maxLevels / showEveryNumLevels;
        float totalLevelHeight = levelEndPosY - levelStartPosY;
        float levelHeight = totalLevelHeight / totalLevels;

        float previousPosition = levelStartPosY;

        for (int i = 0; i <= maxLevels; i += showEveryNumLevels)
        {
            float posY = lvlsTopAndBottonPadding + ((i / showEveryNumLevels) * levelHeight);
            float posX = Random.Range(-maxHorizontalOffset, maxHorizontalOffset);

            if (i == 0 || i >= maxLevels)
            {
                posX = 0f;
            }

            Vector3 spawnPosition = new Vector3(posX, posY, 0);
            nodePositions.Add(spawnPosition);

            GameObject newNode = Instantiate(levelIconPrefab, contentParent);
            newNode.name = "Nivel_" + i;

            RectTransform rectTransform = newNode.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = spawnPosition;
            }

            if (newNode.TryGetComponent(out LevelIcon levelIcon))
            {
                levelIcon.SetSfx(sfx);
                levelIcon.SetLevel(i);
                if (i <= record) levelIcon.UnlockLevel();
                icons[i] = levelIcon;
            }

            if (record >= i && record < i + showEveryNumLevels)
            {
                GameObject recordObj = Instantiate(recordPrefab, recordParent);
                RecordShow recordShow = recordObj.GetComponent<RecordShow>();

                if (recordShow)
                {
                    recordShow.SetRecord(record);
                    recordShow.SetY(posY + ((posY - previousPosition) / showEveryNumLevels) * (record - i));
                    if (posX < 0)
                    {
                        recordShow.SetTextX(recordPadding);
                    }
                    else
                    {
                        recordShow.SetTextX(-recordPadding);
                    }
                }
            }

            previousPosition = posY;
        }
    }

    void GenerateBackground()
    {
        float segmentHeight = 100f * 5.5f;
        float maxNodePosY = (maxLevels / showEveryNumLevels) * levelPadding;

        float minRequiredHeight = maxNodePosY + (bkgTopAndBottonPadding * 2);

        float heightWithoutEnds = minRequiredHeight - (segmentHeight * 2);
        int midCount = 0;
        if (heightWithoutEnds > 0)
        {
            midCount = Mathf.CeilToInt(heightWithoutEnds / segmentHeight);
        }

        float totalTilesHeight = (2 + midCount) * segmentHeight;

        panelHeight = bkgTopAndBottonPadding + totalTilesHeight;

        RectTransform mainPanelRect = transform.GetComponent<RectTransform>();
        if (mainPanelRect != null)
        {
            mainPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
        }

        RectTransform bkgRectTransform = backgroundParent.GetComponent<RectTransform>();
        if (bkgRectTransform != null)
        {
            bkgRectTransform.anchoredPosition = new Vector2(0, bkgTopAndBottonPadding);
        }

        float currentPosY = 0f;

        SpawnBkgSegment(startBkgPrefab, "Bkg_Start", ref currentPosY, segmentHeight);

        for (int i = 0; i < midCount; i++)
        {
            SpawnBkgSegment(midBkgPrefab, "Bkg_Mid_" + i, ref currentPosY, segmentHeight);
        }

        SpawnBkgSegment(endBkgPrefab, "Bkg_End", ref currentPosY, segmentHeight);
    }

    void SpawnBkgSegment(GameObject prefab, string objName, ref float posY, float height)
    {
        GameObject segment = Instantiate(prefab, backgroundParent);
        segment.name = objName;

        RectTransform rect = segment.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector3(0, posY, 0);
        }
        else
        {
            segment.transform.localPosition = new Vector3(0, posY, 0);
        }

        posY += height;
    }

    void GenerateCurvedPath()
    {
        if (nodePositions.Count < 2 || pathSegmentPrefab == null) return;

        List<Vector3> splinePoints = CalculateCatmullRomSpline(nodePositions, pointsPerSegment);

        float accumulatedDistance = 0f;

        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            Vector3 pA = splinePoints[i];
            Vector3 pB = splinePoints[i + 1];
            float dist = Vector3.Distance(pA, pB);

            accumulatedDistance += dist;

            if (accumulatedDistance >= segmentSpacing)
            {
                accumulatedDistance = 0f;

                GameObject segment = Instantiate(pathSegmentPrefab, pathParent != null ? pathParent : contentParent);

                RectTransform rect = segment.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = pA;

                    Vector2 direction = (pB - pA).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    rect.rotation = Quaternion.Euler(0, 0, angle - 90f);
                }
            }
        }
    }

    List<Vector3> CalculateCatmullRomSpline(List<Vector3> nodes, int resolution)
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? nodes[i - 1] : nodes[i];
            Vector3 p1 = nodes[i];
            Vector3 p2 = nodes[i + 1];
            Vector3 p3 = i < nodes.Count - 2 ? nodes[i + 2] : nodes[i + 1];

            for (int r = 0; r < resolution; r++)
            {
                float t = r / (float)resolution;
                Vector3 point = GetCatmullRomPosition(t, p0, p1, p2, p3);
                points.Add(point);
            }
        }

        points.Add(nodes[nodes.Count - 1]);
        return points;
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}