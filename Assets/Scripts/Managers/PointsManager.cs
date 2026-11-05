using System.Collections;
using UnityEngine;
using TMPro;

public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance { get; private set; }

    [Header("UI del Marcador")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private bool alwaysShowScore = false;
    [SerializeField] private float leftTargetPadding = 10f;

    [Header("Audio")]
    [SerializeField] private UISFXManager sfx;

    [Header("Multiplicador (xN)")]
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private string multiplierFormat = "x{0}";
    [SerializeField] private bool showAtX1 = false;

    [Header("Multiplicador - Tamaño y posición")]
    [SerializeField] private bool matchScoreSize = true;
    [SerializeField] private float sizeRatio = 0.6f;
    [SerializeField] private bool followScoreText = true;
    [SerializeField] private float spacing = 10f;

    [Header("Multiplicador - Animación")]
    [SerializeField] private Color hotColor = new Color(1f, 0.35f, 0.1f);
    [SerializeField] private int colorMaxMultiplier = 8;
    [SerializeField] private float growthPerStep = 0.08f;
    [SerializeField] private int maxGrowthSteps = 8;
    [SerializeField] private float punchScale = 0.35f;
    [SerializeField] private float punchDecay = 0.3f;
    [SerializeField] private float wobbleSpeed = 6f;
    [SerializeField] private float wobbleDegreesPerStep = 0.6f;

    [Header("Animación y Tiempos del Texto")]
    [SerializeField] private float hideDelay = 1.0f;

    [Header("Configuración Dinámica por Precio")]
    [SerializeField] private float baseDuration = 0.5f;
    [SerializeField] private float maxDuration = 2.0f;
    [SerializeField] private int referenceMaxPrice = 500;
    [SerializeField] private AnimationCurve priceDurationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Puntuación")]
    [SerializeField] private int points = 0;
    private const string SCORE_KEY = "PlayerScore";
    public static int RunTotal { get; private set; }

    public static void ResetRun()
    {
        RunTotal = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        RunTotal = 0;
    }

    private Vector3 originalScale;
    private Coroutine hideCoroutine;
    private Coroutine scoreAnimCoroutine;

    private Vector3 multiplierBaseScale = Vector3.one;
    private Quaternion multiplierBaseRotation = Quaternion.identity;
    private Color multiplierBaseColor = Color.white;
    private float punch;
    private int lastMultiplier = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (scoreText != null)
        {
            originalScale = scoreText.transform.localScale;
        }

        if (multiplierText != null)
        {
            multiplierBaseScale = multiplierText.transform.localScale;
            multiplierBaseRotation = multiplierText.transform.localRotation;
            multiplierBaseColor = multiplierText.color;

            if (matchScoreSize) multiplierText.enableAutoSizing = false;
            if (followScoreText) multiplierText.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        points = PlayerPrefs.GetInt(SCORE_KEY, 0);
    }

    private void Start()
    {
        UpdateScoreUI();
        UpdateMultiplierUI(false);

        SetScoreVisible(alwaysShowScore);
    }

    private void Update()
    {
        if (multiplierText == null || !multiplierText.gameObject.activeInHierarchy) return;

        punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, punchDecay));
        float eased = punch * punch;

        int multiplier = GetMultiplier();
        int steps = Mathf.Min(multiplier - 1, maxGrowthSteps);

        float growth = 1f + steps * growthPerStep;
        multiplierText.transform.localScale = multiplierBaseScale * growth * (1f + punchScale * eased);

        float angle = Mathf.Sin(Time.unscaledTime * wobbleSpeed) * steps * wobbleDegreesPerStep;
        multiplierText.transform.localRotation = multiplierBaseRotation * Quaternion.Euler(0f, 0f, angle);

        float tier = Mathf.InverseLerp(2f, Mathf.Max(3, colorMaxMultiplier), multiplier);
        Color tierColor = Color.Lerp(multiplierBaseColor, hotColor, tier);
        multiplierText.color = Color.Lerp(tierColor, Color.white, eased * 0.5f);
    }

    private void LateUpdate()
    {
        FitMultiplierToScore();
    }

    private void FitMultiplierToScore()
    {
        if (multiplierText == null || scoreText == null) return;
        if (!multiplierText.gameObject.activeInHierarchy || !scoreText.gameObject.activeInHierarchy) return;
        if (!matchScoreSize && !followScoreText) return;

        scoreText.ForceMeshUpdate();

        if (matchScoreSize)
        {
            multiplierText.enableAutoSizing = false;
            multiplierText.fontSize = scoreText.fontSize * sizeRatio;
        }

        multiplierText.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal, multiplierText.preferredWidth);

        if (followScoreText)
        {
            Bounds b = scoreText.textBounds;
            Vector3 rightEdge = scoreText.transform.TransformPoint(new Vector3(b.max.x, b.center.y, 0f));

            RectTransform rt = multiplierText.rectTransform;
            rt.position = rightEdge;
            rt.anchoredPosition += new Vector2(spacing, 0f);
        }
    }

    public int GetCurrentScore()
    {
        return points;
    }

    public bool TryGetScoreLeftEdgeWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (scoreText == null) return false;

        scoreText.ForceMeshUpdate();

        Bounds b = scoreText.textBounds;
        Vector3 localLeft = new Vector3(b.min.x - leftTargetPadding, b.center.y, 0f);
        worldPosition = scoreText.transform.TransformPoint(localLeft);
        return true;
    }

    private static int GetMultiplier()
    {
        return 1 + LevelLoader.GetConsecutiveLevels();
    }

    public void AddPoints(int amount)
    {
        points += amount;
        if (amount > 0) RunTotal += amount;
        PlayerPrefs.SetInt(SCORE_KEY, points);
        PlayerPrefs.Save();
        UpdateScoreUI();
        UpdateMultiplierUI(true);
        ShowScore();

        if (amount > 0)
        {
            punch = Mathf.Max(punch, 0.8f);
            if (sfx != null) sfx.PlayPoint();
        }
    }

    public void SpendPoints(int amount)
    {
        points -= amount;
        points = Mathf.Max(0, points);
        PlayerPrefs.SetInt(SCORE_KEY, points);
        PlayerPrefs.Save();
    }

    public void AnimateScoreSpend(int skinPrice)
    {
        float priceFactor = Mathf.Clamp01((float)skinPrice / referenceMaxPrice);

        float curvedFactor = priceDurationCurve.Evaluate(priceFactor);

        float dynamicDuration = Mathf.Lerp(baseDuration, maxDuration, curvedFactor);

        if (scoreText != null)
        {
            SetScoreVisible(true);

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
        }

        if (scoreAnimCoroutine != null)
        {
            StopCoroutine(scoreAnimCoroutine);
        }

        scoreAnimCoroutine = StartCoroutine(AnimateScoreSpendRoutine(skinPrice, dynamicDuration));
    }

    private IEnumerator AnimateScoreSpendRoutine(int amountToSubtract, float duration)
    {
        int startScore = points + amountToSubtract;
        int targetScore = points;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            int displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, smoothT));

            if (scoreText != null)
            {
                scoreText.text = displayedScore.ToString();
            }

            yield return null;
        }

        UpdateScoreUI();

        if (!alwaysShowScore)
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            hideCoroutine = StartCoroutine(HideScoreAfterDelay());
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = points.ToString();
        }
    }

    private void UpdateMultiplierUI(bool animateChange)
    {
        if (multiplierText == null) return;

        int multiplier = GetMultiplier();
        multiplierText.text = string.Format(multiplierFormat, multiplier);

        if (animateChange && lastMultiplier != -1 && multiplier != lastMultiplier)
        {
            punch = 1f;
        }

        lastMultiplier = multiplier;
    }
    private void SetScoreVisible(bool visible)
    {
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(visible);
        }

        if (multiplierText != null)
        {
            bool showMultiplier = visible && (showAtX1 || GetMultiplier() > 1);
            multiplierText.gameObject.SetActive(showMultiplier);
        }
    }

    public void ShowScore()
    {
        if (scoreText != null)
        {
            SetScoreVisible(true);

            if (alwaysShowScore) return;

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            hideCoroutine = StartCoroutine(HideScoreAfterDelay());
        }
    }

    private IEnumerator HideScoreAfterDelay()
    {
        if (alwaysShowScore) yield break;

        yield return new WaitForSeconds(hideDelay);
        SetScoreVisible(false);
    }
}