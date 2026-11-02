using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Al pasar el ratón (solo ratón/lápiz)")]
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
    [SerializeField] float pressSpeed = 28f;
    [SerializeField] float appearDuration = 0.38f;
    [SerializeField] float appearOvershoot = 1.18f;
    [SerializeField] float disappearDuration = 0.16f;
    [SerializeField] Vector2 disappearOffset = new Vector2(0f, -30f);

    Button button;
    RectTransform rt;
    float current = 1f;
    float squash;
    float tilt;

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

    bool settled;
    bool lastInteractable = true;

    const float Eps = 0.0005f;

    void Init()
    {
        if (inited) return;
        inited = true;
        button = GetComponent<Button>();
        rt = (RectTransform)transform;
    }

    void Awake() => Init();

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
        tilt = 0f;
        settled = true;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    public void Appear(float delay) => Appear(delay, new Vector2(0f, -40f), true);

    /// <param name="rebuildLayout"></param>
    public void Appear(float delay, Vector2 startOffset, bool rebuildLayout = true)
    {
        Init();
        settled = false;

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
        tilt = 0f;
        transform.localRotation = Quaternion.identity;

        CaptureRest(rebuildLayout);

        transform.localScale = Vector3.zero;
    }

    public void Disappear(float delay)
    {
        Init();
        settled = false;
        disappearTimer = Mathf.Max(delay, 0.0001f);
        appearTimer = 0f;
        appearing = false;
        hover = pressed = false;
    }

    void CaptureRest(bool rebuild = true)
    {
        if (rt == null) return;

        Vector2 currentAnchored = rt.anchoredPosition;
        if (offsetActive) currentAnchored -= offset;

        if (rebuild && transform.parent is RectTransform parent)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        restPos = offsetActive ? currentAnchored : rt.anchoredPosition;
    }

    void Update()
    {
        if (exited) return;

        bool interactable = button != null && button.interactable;
        if (interactable != lastInteractable)
        {
            lastInteractable = interactable;
            settled = false;
        }

        if (settled && disappearTimer <= 0f && appearTimer <= 0f && !appearing && !disappearing)
            return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

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
            CaptureRest(false); 
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
                settled = !hover && !pressed;
            }
            return;
        }

        Vector2 targetOffset = Vector2.zero;
        float targetScale = 1f;
        float targetSquash = 0f;
        float targetTilt = 0f;
        bool animatedHover = false;

        if (interactable)
        {
            if (pressed)
            {
                targetScale = pressedScale;
                targetSquash = pressedSquash;
            }
            else if (hover)
            {
                targetOffset = hoverOffset;
                targetScale = hoverScale;

                if (hoverPulse != 0f)
                {
                    targetScale += Mathf.Sin(Time.unscaledTime * pulseSpeed) * hoverPulse;
                    animatedHover = true;
                }
                if (hoverTilt != 0f)
                {
                    targetTilt = Mathf.Sin(Time.unscaledTime * pulseSpeed * 0.5f) * hoverTilt;
                    animatedHover = true;
                }
            }
        }

        float lerp = 1f - Mathf.Exp(-(pressed ? pressSpeed : speed) * dt);
        current = Mathf.Lerp(current, targetScale, lerp);
        squash = Mathf.Lerp(squash, targetSquash, lerp);

        transform.localScale = new Vector3(current * (1f + squash), current * (1f - squash), 1f);

        if (hoverTilt != 0f || Mathf.Abs(tilt) > Eps)
        {
            tilt = Mathf.LerpAngle(tilt, targetTilt, lerp);
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

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
        if (!animatedHover
            && Mathf.Abs(current - targetScale) < Eps
            && Mathf.Abs(squash - targetSquash) < Eps
            && Mathf.Abs(tilt - targetTilt) < Eps
            && (offset - targetOffset).sqrMagnitude < 0.0001f)
        {
            current = targetScale;
            squash = targetSquash;
            tilt = targetTilt;
            if (!moving || targetOffset == Vector2.zero)
            {
                offset = targetOffset;
                if (offsetActive && targetOffset == Vector2.zero)
                {
                    rt.anchoredPosition = restPos;
                    offsetActive = false;
                }
            }
            transform.localScale = new Vector3(current * (1f + squash), current * (1f - squash), 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            settled = true;
        }
    }
    static bool IsTouch(PointerEventData e) => e.pointerId >= 0;

    public void OnPointerEnter(PointerEventData e)
    {
        if (IsTouch(e)) return;
        hover = true;
        settled = false;
    }

    public void OnPointerExit(PointerEventData e)
    {
        hover = false;
        settled = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        pressed = true;
        settled = false;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (pressed && button != null && button.interactable && !appearing && !disappearing)
            current = hoverScale + 0.05f;

        pressed = false;
        settled = false;

        if (IsTouch(e)) hover = false;
    }
}