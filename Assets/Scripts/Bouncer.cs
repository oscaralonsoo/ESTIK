using System.Collections;
using UnityEngine;

public class Bouncer : MonoBehaviour
{
    [Header("Push")]
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private float upwardBoost = 1f;
    [SerializeField] private bool trigger = true;

    [Header("Scale Animation")]
    [Tooltip("Multiplicador de escala al encogerse (0.7 = 70% del tamaño original)")]
    [SerializeField] private float shrinkMultiplier = 0.7f;
    [SerializeField] private float shrinkDuration = 0.08f;
    [SerializeField] private float growDuration = 0.15f;
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalScale;
    private Coroutine scaleRoutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        scaleRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!trigger) return;
        Push(other.attachedRigidbody);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (trigger) return;
        Push(other.rigidbody);
    }

    private void Push(Rigidbody rb)
    {
        if (rb == null) return;

        Vector3 pushDirection = rb.transform.position - transform.position;

        if (pushDirection.y < upwardBoost)
        {
            pushDirection.y = upwardBoost;
        }

        pushDirection.Normalize();

        rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);

        PlayScaleAnimation();
    }

    private void PlayScaleAnimation()
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleRoutine());
    }

    private IEnumerator ScaleRoutine()
    {
        Vector3 smallScale = originalScale * shrinkMultiplier;
        Vector3 startScale = transform.localScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / shrinkDuration;
            transform.localScale = Vector3.LerpUnclamped(startScale, smallScale, shrinkCurve.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / growDuration;
            transform.localScale = Vector3.LerpUnclamped(smallScale, originalScale, growCurve.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }

        transform.localScale = originalScale;
        scaleRoutine = null;
    }
}