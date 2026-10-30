using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{

    [Header("Al pasar el ratón")]
    [SerializeField] float hoverScale = 1.05f;
    [SerializeField] Vector2 hoverOffset = new Vector2(0f, 4f);
    [SerializeField] float hoverPulse = 0f;
    [SerializeField] float pulseSpeed = 6f;
    [SerializeField] float hoverTilt = 0f;

    [Header("Al pulsar")]
    [SerializeField] float pressedScale = 0.95f;
    [SerializeField] float pressedSquash = 0.04f;

    [Header("General")]
    [SerializeField] float speed = 16f;
    [SerializeField] float appearDuration = 0.38f;
    [SerializeField] float appearOvershoot = 1.18f;
    [SerializeField] float disappearDuration = 0.16f;
    [SerializeField] Vector2 disappearOffset = new Vector2(0f, -30f);

    Toggle toggle;
    RectTransform rt;
    float current = 1f;
    float squash;

    Vector2 restPos;
    Vector2 offset;
    bool offsetActive;

    float appearTimer, appearT;
    bool appearing;
    Vector2 appearOffset;

    float disappearTimer, disappearT;
    bool disappearing, exited;
    Vector2 fromOffset;
    float fromScale;

    bool hover, pressed, inited;

    void Init()
    {
        if (inited) return;
        inited = true;
        toggle = GetComponent<Toggle>();
        rt = (RectTransform)transform;
    }

    void Awake() => Init();

    void OnDestroy()
    {
    }

    void OnDisable()
    {
        if ((offsetActive || appearing || disappearing) && rt != null)
            rt.anchoredPosition = restPos;

        hover = pressed = appearing = offsetActive = false;
        disappearing = exited = false;
        appearTimer = disappearTimer = 0f;
        offset = Vector2.zero;
        current = 1f;
        squash = 0f;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    public void Appear(float delay) => Appear(delay, new Vector2(0f, -40f));

    public void Appear(float delay, Vector2 startOffset)
    {
        Init();

        disappearing = false;
        exited = false;
        disappearTimer = 0f;
        disappearT = 0f;

        if (offsetActive && rt != null)
        {
            rt.anchoredPosition = restPos;
            offsetActive = false;
            offset = Vector2.zero;
        }

        appearTimer = Mathf.Max(delay, 0.0001f);
        appearOffset = startOffset;
        appearing = false;
        current = 1f;
        squash = 0f;
        transform.localRotation = Quaternion.identity;

        CaptureRest();

        transform.localScale = Vector3.zero;
    }

    public void Disappear(float delay)
    {
        Init();
        disappearTimer = Mathf.Max(delay, 0.0001f);
        appearTimer = 0f;
        appearing = false;
        hover = pressed = false;
    }

    void CaptureRest()
    {
        if (rt == null) return;

        Vector2 currentAnchored = rt.anchoredPosition;
        if (offsetActive) currentAnchored -= offset;

        if (transform.parent is RectTransform parent)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        restPos = offsetActive ? currentAnchored : rt.anchoredPosition;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        if (exited) return;

        if (disappearTimer > 0f)
        {
            disappearTimer -= dt;
            if (disappearTimer > 0f) return;

            if (!offsetActive) CaptureRest();
            offsetActive = true;
            disappearing = true;
            disappearT = 0f;
            fromOffset = offset;
            fromScale = current;
        }

        if (disappearing)
        {
            disappearT += dt;
            float k = Mathf.Clamp01(disappearT / disappearDuration);

            const float c1 = 1.7f;
            float e = (c1 + 1f) * k * k * k - c1 * k * k;

            offset = Vector2.Lerp(fromOffset, disappearOffset, k * k);
            rt.anchoredPosition = restPos + offset;
            transform.localScale = Vector3.one * Mathf.Max(0f, fromScale * (1f - e));
            transform.localRotation = Quaternion.identity;

            if (k >= 1f)
            {
                transform.localScale = Vector3.zero;
                exited = true;
            }
            return;
        }

        if (appearTimer > 0f)
        {
            appearTimer -= dt;
            if (appearTimer > 0f)
            {
                transform.localScale = Vector3.zero;
                return;
            }
            CaptureRest();
            appearing = true;
            appearT = 0f;
        }

        if (appearing)
        {
            appearT += dt;
            float k = Mathf.Clamp01(appearT / appearDuration);

            float slide = 1f - Mathf.Pow(1f - k, 3f);
            float c3 = appearOvershoot + 1f;
            float pop = 1f + c3 * Mathf.Pow(k - 1f, 3f) + appearOvershoot * Mathf.Pow(k - 1f, 2f);

            offset = Vector2.Lerp(appearOffset, Vector2.zero, slide);
            current = pop;
            rt.anchoredPosition = restPos + offset;
            transform.localScale = Vector3.one * Mathf.Max(0f, current);

            if (k >= 1f)
            {
                appearing = false;
                offset = Vector2.zero;
                current = 1f;
                rt.anchoredPosition = restPos;
                transform.localScale = Vector3.one;
            }
            return;
        }

        Vector2 targetOffset = Vector2.zero;
        float targetScale = 1f;
        float targetSquash = 0f;
        float targetTilt = 0f;

        if (toggle != null && toggle.interactable)
        {
            if (pressed)
            {
                targetScale = pressedScale;
                targetSquash = pressedSquash;
            }
            else if (hover)
            {
                targetOffset = hoverOffset;
                targetScale = hoverScale + Mathf.Sin(Time.unscaledTime * pulseSpeed) * hoverPulse;
                targetTilt = Mathf.Sin(Time.unscaledTime * pulseSpeed * 0.5f) * hoverTilt;
            }
        }

        float lerp = 1f - Mathf.Exp(-speed * dt);
        current = Mathf.Lerp(current, targetScale, lerp);
        squash = Mathf.Lerp(squash, targetSquash, lerp);

        transform.localScale = new Vector3(current * (1f + squash), current * (1f - squash), 1f);

        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetTilt);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, lerp);

        bool moving = offset.sqrMagnitude > 0.0001f || targetOffset != Vector2.zero;
        if (moving)
        {
            if (!offsetActive)
            {
                CaptureRest();
                offsetActive = true;
            }

            offset = Vector2.Lerp(offset, targetOffset, lerp);
            rt.anchoredPosition = restPos + offset;

            if (targetOffset == Vector2.zero && offset.sqrMagnitude < 0.01f)
            {
                offset = Vector2.zero;
                rt.anchoredPosition = restPos;
                offsetActive = false;
            }
        }
    }

    public void OnPointerEnter(PointerEventData e) => hover = true;
    public void OnPointerExit(PointerEventData e) => hover = false;
    public void OnPointerDown(PointerEventData e) => pressed = true;
    public void OnPointerUp(PointerEventData e)
    {
        if (pressed && toggle != null && toggle.interactable && !appearing && !disappearing)
            current = hoverScale + 0.05f;
        pressed = false;
    }
}