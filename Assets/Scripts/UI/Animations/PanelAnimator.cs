using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelAnimator : MonoBehaviour
{
    public enum ButtonsAppear { CascadeSides, CascadeUp, WithPanel }
    public enum PanelStyle { Pop, Slide, Drop, Jelly, Unfold }

    [Header("Panel")]
    [SerializeField] RectTransform window;
    [SerializeField] PanelStyle style = PanelStyle.Pop;
    [SerializeField] bool animateScale = true;

    [Header("Entrada")]
    [SerializeField] float enterDuration = 0.35f;
    [SerializeField] float slideDistance = 120f;
    [SerializeField] float dropHeight = 300f;
    [SerializeField] float startScale = 0.6f;
    [SerializeField] float overshoot = 1.7f;

    [Header("Salida")]
    [SerializeField] float exitDuration = 0.25f;
    [SerializeField] float exitSlide = 40f;
    [SerializeField] float exitScale = 0.85f;

    [Header("Botones")]
    [SerializeField] ButtonsAppear buttonsAppear = ButtonsAppear.CascadeUp;
    [SerializeField] float buttonRise = 30f;
    [SerializeField] float buttonStagger = 0.07f;
    [SerializeField] float maxCascadeWindow = 0.55f;
    [SerializeField] float exitButtonStagger = 0.03f;

    // Un frame largo (por ejemplo al instanciar contenido la primera vez que se abre
    // el panel) no debe "comerse" la animacion.
    const float MaxFrameTime = 1f / 30f;

    /// <summary>
    /// 0 = panel invisible, 1 = totalmente visible. Sirve para que contenido 3D
    /// (que el CanvasGroup no puede desvanecer) siga la misma transicion.
    /// </summary>
    public float Visibility { get; private set; } = 1f;

    Vector2 restPos;
    CanvasGroup group;
    bool inited, hiding;

    static float FrameTime => Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);

    float EnterTime
    {
        get
        {
            switch (style)
            {
                case PanelStyle.Drop: return enterDuration * 1.6f;
                case PanelStyle.Jelly: return enterDuration * 1.4f;
                case PanelStyle.Unfold: return enterDuration * 1.1f;
                default: return enterDuration;
            }
        }
    }

    // ---------------- CICLO DE VIDA ----------------

    void Init()
    {
        if (inited) return;
        inited = true;
        if (window == null) window = GetComponent<RectTransform>();
        restPos = window.anchoredPosition;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        Init();
        if (window == null) return;
        hiding = false;
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    void OnDisable()
    {
        if (!inited || window == null) return;
        window.anchoredPosition = restPos;
        window.localScale = Vector3.one;
        SetAlpha(1f);
        group.interactable = true;
        group.blocksRaycasts = true;
        hiding = false;
    }

    void SetAlpha(float a)
    {
        Visibility = a;
        group.alpha = a;
    }

    // ---------------- ENTRADA ----------------

    IEnumerator Play()
    {
        group.blocksRaycasts = false;
        group.interactable = true;
        float dur = EnterTime;

        SetupButtons(0.08f);
        ApplyEnter(0f);
        yield return null;

        float t = 0f;
        while (t < dur)
        {
            t += FrameTime;
            ApplyEnter(Mathf.Min(t, dur) / dur);
            yield return null;
        }

        window.anchoredPosition = restPos;
        window.localScale = Vector3.one;
        SetAlpha(1f);
        group.blocksRaycasts = true;
    }

    void ApplyEnter(float k)
    {
        Vector2 pos = restPos;
        Vector3 scale = Vector3.one;
        float alpha = Mathf.Clamp01(k / 0.35f);

        switch (style)
        {
            case PanelStyle.Pop:
                {
                    float s = Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(k, overshoot));
                    scale = Vector3.one * s;
                    break;
                }
            case PanelStyle.Slide:
                {
                    pos = restPos + Vector2.up * slideDistance * (1f - EaseOutCubic(k));
                    scale = Vector3.one * Mathf.Lerp(0.9f, 1f, EaseOutCubic(k));
                    break;
                }
            case PanelStyle.Drop:
                {
                    pos = restPos + Vector2.up * dropHeight * (1f - EaseOutBounce(k));
                    alpha = Mathf.Clamp01(k / 0.15f);
                    break;
                }
            case PanelStyle.Jelly:
                {
                    float d = Mathf.Exp(-5f * k);
                    float sx = 1f - (1f - startScale) * d * Mathf.Cos(k * 11f);
                    float sy = 1f - (1f - startScale) * d * Mathf.Cos(k * 8.5f);
                    scale = new Vector3(sx, sy, 1f);
                    break;
                }
            case PanelStyle.Unfold:
                {
                    float sy = Mathf.LerpUnclamped(0.05f, 1f, EaseOutBack(k, 1.3f));
                    float sx = Mathf.Lerp(0.85f, 1f, EaseOutCubic(k));
                    scale = new Vector3(sx, sy, 1f);
                    break;
                }
        }

        window.anchoredPosition = pos;
        window.localScale = animateScale ? scale : Vector3.one;
        SetAlpha(alpha);
    }

    // ---------------- SALIDA ----------------

    public void Hide(System.Action onDone = null)
    {
        if (hiding) return;

        if (window == null || !gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            onDone?.Invoke();
            return;
        }

        hiding = true;
        StopAllCoroutines();
        StartCoroutine(HideRoutine(onDone));
    }

    IEnumerator HideRoutine(System.Action onDone)
    {
        group.blocksRaycasts = false;

        var uiButtons = window.GetComponentsInChildren<Button>(true);

        float maxExitWindow = 0.15f;
        float actualExitStagger = uiButtons.Length > 1
            ? Mathf.Min(exitButtonStagger, maxExitWindow / (uiButtons.Length - 1))
            : exitButtonStagger;

        for (int i = 0; i < uiButtons.Length; i++)
        {
            var feedback = uiButtons[i].GetComponent<ButtonFeedback>();
            if (feedback == null) feedback = uiButtons[i].gameObject.AddComponent<ButtonFeedback>();
            feedback.Disappear((uiButtons.Length - 1 - i) * actualExitStagger);
        }

        float buttonsTime = uiButtons.Length > 0
            ? (uiButtons.Length - 1) * actualExitStagger + 0.12f
            : 0f;
        float total = Mathf.Max(exitDuration, buttonsTime);

        // Se aplica siempre el ultimo frame (alpha 0) antes de desactivar,
        // para que no quede un "pop" con el panel a medio desvanecer.
        float t = 0f;
        while (t < total)
        {
            t += FrameTime;
            ApplyExit(Mathf.Min(t, total), total);
            yield return null;
        }

        var cb = onDone;
        gameObject.SetActive(false);
        cb?.Invoke();
    }

    void ApplyExit(float t, float total)
    {
        bool slides = style == PanelStyle.Slide || style == PanelStyle.Drop;

        float k = Mathf.Clamp01(t / exitDuration);
        float e = k * k;
        float f = Mathf.Clamp01(t / total);

        window.anchoredPosition = slides ? restPos + Vector2.down * exitSlide * e : restPos;

        if (animateScale)
        {
            window.localScale = style == PanelStyle.Unfold
                ? new Vector3(Mathf.Lerp(1f, 0.9f, e), Mathf.Lerp(1f, 0.05f, e), 1f)
                : Vector3.one * Mathf.Lerp(1f, exitScale, e);
        }

        SetAlpha(1f - f * f);
    }

    // ---------------- BOTONES ----------------

    static bool IsBackButton(string objectName)
    {
        string n = objectName.ToLower();
        return n.Contains("back") || n.Contains("atras") || n.Contains("close") || n.Contains("return");
    }

    void SetupButtons(float delayStart)
    {
        var uiButtons = window.GetComponentsInChildren<Button>(true);
        if (uiButtons.Length == 0) return;

        int cascadeCount = 0;
        foreach (var b in uiButtons)
            if (!IsBackButton(b.gameObject.name)) cascadeCount++;

        float actualStagger = cascadeCount > 1
            ? Mathf.Min(buttonStagger, maxCascadeWindow / (cascadeCount - 1))
            : buttonStagger;

        int cascadeIndex = 0;
        for (int i = 0; i < uiButtons.Length; i++)
        {
            var feedback = uiButtons[i].GetComponent<ButtonFeedback>();
            if (feedback == null) feedback = uiButtons[i].gameObject.AddComponent<ButtonFeedback>();

            if (buttonsAppear == ButtonsAppear.WithPanel) continue;

            bool isBack = IsBackButton(uiButtons[i].gameObject.name);
            float delay = isBack ? 0f : delayStart + (cascadeIndex++) * actualStagger;

            Vector2 offset = buttonsAppear == ButtonsAppear.CascadeUp
                ? new Vector2(0f, -buttonRise)
                : new Vector2((i % 2 == 0 ? -1f : 1f) * buttonRise, 0f);

            feedback.Appear(delay, offset);
        }
    }

    // ---------------- EASINGS ----------------

    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);

    static float EaseOutBack(float x, float c1)
    {
        x = Mathf.Clamp01(x);
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    static float EaseOutBounce(float x)
    {
        x = Mathf.Clamp01(x);
        const float n1 = 7.5625f, d1 = 2.75f;
        if (x < 1f / d1) return n1 * x * x;
        if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
        if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
        x -= 2.625f / d1; return n1 * x * x + 0.984375f;
    }
}