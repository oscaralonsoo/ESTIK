using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class SkinUnlockAnimation
{
    [Header("Unlock Anim")]
    [SerializeField] float shakeDuration = 0.35f;
    [SerializeField] float shakeAngle = 18f;
    [SerializeField] float lockPopDuration = 0.25f;
    [SerializeField] float lockPopScale = 1.6f;
    [SerializeField] float revealDuration = 0.55f;
    [SerializeField] float ballPunchScale = 1.35f;
    public float BallPunch { get; private set; } = 1f;
    public float LockScale { get; private set; } = 1f;
    public float LockRot { get; private set; }

    public void Reset()
    {
        BallPunch = 1f;
        LockScale = 1f;
        LockRot = 0f;
    }

    /// <param name="onShakeEnd">Se llama al terminar el shake (el item marca unlocked, arranca partículas y glow).</param>
    /// <param name="onFrame">Se llama cada frame de la fase de reveal (el item reaplica el tinte).</param>
    public IEnumerator Run(SkinTinter tinter, Action onShakeEnd, Action onFrame)
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / shakeDuration);

            LockRot = Mathf.Sin(k * Mathf.PI * 6f) * shakeAngle * (1f - k * 0.3f);
            LockScale = 1f + 0.1f * Mathf.Sin(k * Mathf.PI);
            BallPunch = 1f - 0.1f * Mathf.Sin(k * Mathf.PI);
            yield return null;
        }

        onShakeEnd?.Invoke();

        t = 0f;
        float total = Mathf.Max(revealDuration, lockPopDuration);
        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            float kp = Mathf.Clamp01(t / lockPopDuration);
            LockScale = Mathf.Lerp(1f, lockPopScale, kp) * (1f - kp * kp);
            LockRot = Mathf.Lerp(0f, 25f, kp);

            float kr = Mathf.Clamp01(t / revealDuration);
            tinter.Darkness = 1f - Mathf.SmoothStep(0f, 1f, kr);
            tinter.Flash = Mathf.Sin(Mathf.Clamp01(kr * 2f) * Mathf.PI * 0.5f) * (1f - kr) * 0.8f;
            BallPunch = 1f + (ballPunchScale - 1f) * Mathf.Sin(kr * Mathf.PI);

            onFrame?.Invoke();
            yield return null;
        }
    }
}
