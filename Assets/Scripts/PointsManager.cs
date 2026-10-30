using System.Collections;
using UnityEngine;
using TMPro;

public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance { get; private set; }

    [Header("UI del Marcador")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string scorePrefix = "";
    [SerializeField] private bool alwaysShowScore = false;

    [Header("Animación y Tiempos del Texto")]
    [SerializeField] private float punchScaleMultiplier = 1.3f;
    [SerializeField] private float punchDuration = 0.15f;
    [SerializeField] private float showScaleDuration = 0.2f;
    [SerializeField] private float hideDelay = 1.0f;

    [Header("Configuración Dinámica por Precio")]
    [SerializeField] private float baseDuration = 0.5f;
    [SerializeField] private float maxDuration = 2.0f;
    [SerializeField] private int referenceMaxPrice = 500;
    [Tooltip("Curva para modular cómo afecta el precio al tiempo de animación de forma personalizada.")]
    [SerializeField] private AnimationCurve priceDurationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Puntuación")]
    private int currentScore = 0;
    private const string SCORE_KEY = "PlayerScore";

    private Vector3 originalScale;
    private Coroutine hideCoroutine;
    private Coroutine scoreAnimCoroutine;

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
    }

    private void Start()
    {
        currentScore = PlayerPrefs.GetInt(SCORE_KEY, 0);
        currentScore = 100000; // Valor inicial para pruebas si lo deseas
        UpdateScoreUI();

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(alwaysShowScore);
        }
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public void AddPoints(int amount)
    {
        currentScore += amount;
        PlayerPrefs.SetInt(SCORE_KEY, currentScore);
        PlayerPrefs.Save();
        UpdateScoreUI();
        ShowScore();
    }

    public void SpendPoints(int amount)
    {
        currentScore -= amount;
        currentScore = Mathf.Max(0, currentScore);
        PlayerPrefs.SetInt(SCORE_KEY, currentScore);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Anima el texto bajando progresivamente calculando la duración según el precio de forma automática y modulada por curva.
    /// </summary>
    public void AnimateScoreSpend(int skinPrice)
    {
        // 1. Factor de precio normalizado (0 a 1 respecto al precio de referencia)
        float priceFactor = Mathf.Clamp01((float)skinPrice / referenceMaxPrice);

        // 2. Modulamos el factor usando la curva configurable desde el Inspector
        float curvedFactor = priceDurationCurve.Evaluate(priceFactor);

        // 3. Obtenemos la duración final interpolada
        float dynamicDuration = Mathf.Lerp(baseDuration, maxDuration, curvedFactor);

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);

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
        int startScore = currentScore + amountToSubtract;
        int targetScore = currentScore;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            int displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, smoothT));

            if (scoreText != null)
            {
                scoreText.text = scorePrefix + displayedScore.ToString();
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
            scoreText.text = scorePrefix + currentScore.ToString();
        }
    }

    public void ShowScore()
    {
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);

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
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }
}