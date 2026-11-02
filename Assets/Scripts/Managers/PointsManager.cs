using System.Collections;
using UnityEngine;
using TMPro;

public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance { get; private set; }

    [Header("UI del Marcador")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private bool alwaysShowScore = false;

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

        points = PlayerPrefs.GetInt(SCORE_KEY, 0);
    }

    private void Start()
    {
        UpdateScoreUI();

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(alwaysShowScore);
        }
    }

    public int GetCurrentScore()
    {
        return points;
    }

    public void AddPoints(int amount)
    {
        points += amount;
        PlayerPrefs.SetInt(SCORE_KEY, points);
        PlayerPrefs.Save();
        UpdateScoreUI();
        ShowScore();
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