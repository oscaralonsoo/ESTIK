using System.Collections;
using UnityEngine;
using TMPro;

public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance { get; private set; }

    [Header("UI del Marcador")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string scorePrefix = "";

    [Header("Animación y Tiempos del Texto")]
    [SerializeField] private float punchScaleMultiplier = 1.3f; // Cuánto crece el texto al recibir un punto
    [SerializeField] private float punchDuration = 0.15f;     // Lo que dura el efecto de pulso
    [SerializeField] private float showScaleDuration = 0.2f;  // Lo que tarda en aparecer con animación al inicio
    [SerializeField] private float hideDelay = 1.0f;          // Tiempo que se queda visible en pantalla tras terminar de sumar

    [Header("Puntuación")]
    private int currentScore = 0;
    private const string SCORE_KEY = "PlayerScore";

    private Vector3 originalScale;
    private Coroutine punchCoroutine;
    private Coroutine hideCoroutine;
    private Coroutine showCoroutine;

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

        if (scoreText != null)
        {
            originalScale = scoreText.transform.localScale;
        }
    }

    private void Start()
    {
        currentScore = PlayerPrefs.GetInt(SCORE_KEY, 0);
        UpdateScoreUI();

        // Asegurar que comience desactivado al iniciar el juego
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra el marcador anticipadamente con una animación suave de aparición (*pop*).
    /// </summary>
    public void ShowScore()
    {
        if (scoreText != null)
        {
            // Cancelar temporizadores previos de ocultado
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }

            // Cancelar animación de aparición anterior si estuviera activa
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
            }

            scoreText.gameObject.SetActive(true);
            UpdateScoreUI();

            // Disparar la animación de aparición
            showCoroutine = StartCoroutine(ShowTextRoutine());
        }
    }

    public void AddPoints(int amount)
    {
        currentScore += amount;

        PlayerPrefs.SetInt(SCORE_KEY, currentScore);
        PlayerPrefs.Save();

        UpdateScoreUI();

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }

            // Reiniciar la animación de pulso al recibir puntos
            if (punchCoroutine != null)
            {
                StopCoroutine(punchCoroutine);
            }
            punchCoroutine = StartCoroutine(PunchTextRoutine());
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = scorePrefix + currentScore.ToString();
        }
    }

    private IEnumerator ShowTextRoutine()
    {
        float elapsedTime = 0f;
        scoreText.transform.localScale = Vector3.zero; // Nace desde escala 0

        while (elapsedTime < showScaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / showScaleDuration;

            // Curva suave elástica o progresiva hacia el tamaño original
            scoreText.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        scoreText.transform.localScale = originalScale;
    }

    private IEnumerator PunchTextRoutine()
    {
        float elapsedTime = 0f;
        Vector3 targetScale = originalScale * punchScaleMultiplier;

        // Fase 1: Crecer rápido
        while (elapsedTime < punchDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (punchDuration / 2f);
            scoreText.transform.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        elapsedTime = 0f;

        // Fase 2: Volver a su tamaño original
        while (elapsedTime < punchDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (punchDuration / 2f);
            scoreText.transform.localScale = Vector3.Lerp(targetScale, originalScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        scoreText.transform.localScale = originalScale;

        // Iniciar el tiempo de espera antes de ocultarlo
        hideCoroutine = StartCoroutine(HideTextAfterDelayRoutine());
    }

    private IEnumerator HideTextAfterDelayRoutine()
    {
        yield return new WaitForSeconds(hideDelay);

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }

    public void ResetScore()
    {
        currentScore = 0;
        PlayerPrefs.SetInt(SCORE_KEY, currentScore);
        PlayerPrefs.Save();
        UpdateScoreUI();

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }
}