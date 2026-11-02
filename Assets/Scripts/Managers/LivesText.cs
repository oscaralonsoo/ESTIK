using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LivesText : MonoBehaviour
{
    [Tooltip("{0} = vidas actuales, {1} = vidas maximas. Ejemplos: \"{0}\", \"x{0}\", \"{0}/{1}\"")]
    [SerializeField] private string format = "{0}";

    [Header("ANIMACION AL PERDER VIDA")]
    [SerializeField] private float reloadDelay = 0.2f;     // solo si la animacion se reproduce tras recargar la escena
    [SerializeField] private float punchScale = 1.8f;      // tamaño maximo
    [SerializeField] private float growTime = 0.4f;
    [SerializeField] private float holdTime = 0.5f;       // pausa con el numero grande, ya restado
    [SerializeField] private float shrinkTime = 0.6f;
    [Tooltip("Fuerza del rebote (0 = sin rebote, 1.7 = normal, 3+ = muy exagerado)")]
    [SerializeField] private float bounce = 2.5f;
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f);

    // Ultimo valor mostrado. Es static para sobrevivir a la recarga de escena
    // (al perder una vida se recarga el nivel y este objeto se destruye).
    private static int lastShownLives = -1;

    private TMP_Text label;
    private LivesManager manager;
    private Coroutine anim;
    private Vector3 baseScale;
    private Color baseColor;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        baseScale = transform.localScale;
        baseColor = label.color;
    }

    void OnEnable()
    {
        manager = LivesManager.Instance;
        manager.OnLivesChanged += OnLivesChanged;
    }

    void Start()
    {
        int current = manager.CurrentLives;

        // Venimos de una recarga tras perder una vida: mostramos el valor antiguo y animamos
        if (lastShownLives > current)
        {
            SetText(lastShownLives, manager.MaxLives);
            PlayLoseAnimation(current, manager.MaxLives, reloadDelay);
        }
        else
        {
            SetText(current, manager.MaxLives);
            lastShownLives = current;
        }
    }

    void OnDisable()
    {
        if (anim != null)
        {
            StopCoroutine(anim);
            anim = null;
        }

        transform.localScale = baseScale;
        if (label != null) label.color = baseColor;

        if (manager != null)
        {
            manager.OnLivesChanged -= OnLivesChanged;
            manager = null;
        }
    }

    void OnLivesChanged(int current, int max)
    {
        if (lastShownLives > current)
        {
            PlayLoseAnimation(current, max, 0f);
        }
        else
        {
            // Reset o ganancia de vidas: sin animacion
            if (anim != null) { StopCoroutine(anim); anim = null; }
            transform.localScale = baseScale;
            label.color = baseColor;
            SetText(current, max);
            lastShownLives = current;
        }
    }

    void PlayLoseAnimation(int newValue, int max, float delay)
    {
        if (!isActiveAndEnabled) { SetText(newValue, max); lastShownLives = newValue; return; }

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(LoseLifeRoutine(newValue, max, delay));
    }

    IEnumerator LoseLifeRoutine(int newValue, int max, float delay)
    {
        // Espera inicial (con tiempo no escalado)
        float t = 0f;
        while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }

        Vector3 big = baseScale * punchScale;

        // 1) Crece con el numero antiguo
        t = 0f;
        while (t < growTime)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOut(Mathf.Clamp01(t / growTime));
            transform.localScale = Vector3.LerpUnclamped(baseScale, big, p);
            label.color = Color.Lerp(baseColor, flashColor, p);
            yield return null;
        }

        // 2) Se resta (cambia el numero) estando grande
        SetText(newValue, max);
        lastShownLives = newValue;
        transform.localScale = big;
        label.color = flashColor;

        t = 0f;
        while (t < holdTime) { t += Time.unscaledDeltaTime; yield return null; }

        // 3) Vuelve a su tamaño
        t = 0f;
        while (t < shrinkTime)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOut(Mathf.Clamp01(t / shrinkTime));
            transform.localScale = Vector3.LerpUnclamped(big, baseScale, p);
            label.color = Color.Lerp(flashColor, baseColor, p);
            yield return null;
        }

        transform.localScale = baseScale;
        label.color = baseColor;
        anim = null;
    }

    // Ease out back: se pasa del objetivo y vuelve (efecto rebote)
    float EaseOut(float x)
    {
        float c3 = bounce + 1f;
        float u = x - 1f;
        return 1f + c3 * u * u * u + bounce * u * u;
    }

    void SetText(int current, int max)
    {
        label.text = string.Format(format, current, max);
    }
}