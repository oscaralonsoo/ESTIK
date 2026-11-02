using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SkinCarouselItem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform ball;
    [SerializeField] Renderer ballRenderer;
    [SerializeField] GameObject lockIcon;

    [Header("Feel")]
    [SerializeField] float spinSpeed = 40f;

    [Header("Modules")]
    [SerializeField] SkinTinter tinter = new SkinTinter();
    [SerializeField] SkinVisual visual = new SkinVisual();
    [SerializeField] SkinGlowEffect glow = new SkinGlowEffect();
    [SerializeField] SkinReceivePulse receivePulse = new SkinReceivePulse();
    [SerializeField] SkinUnlockAnimation unlockAnim = new SkinUnlockAnimation();

    public RectTransform Rect { get; private set; }
    public int Index { get; private set; }

    const float MaxFrameTime = 1f / 30f;

    Vector3 _baseBallScale = Vector3.one;
    Vector3 _lockBaseScale = Vector3.one;
    Quaternion _lockBaseRot = Quaternion.identity;

    bool _valid;
    bool _selected;
    bool _locked;
    bool _unlocking;
    bool _visible;

    float _scale = 1f;

    float _appliedBall = float.NaN;
    float _appliedLockScale = float.NaN;
    float _appliedLockRot = float.NaN;
    float _appliedBrightness = float.NaN;

    Coroutine _unlockCo;
    Action _onUnlockDone;

    void Awake()
    {
        Rect = (RectTransform)transform;

        if (ballRenderer == null)
            ballRenderer = GetComponentInChildren<Renderer>(true);
        if (ball == null && ballRenderer != null)
            ball = ballRenderer.transform;

        _valid = ball != null && ballRenderer != null;
        if (!_valid)
        {
            Debug.LogError("SkinCarouselItem: el prefab necesita una bola 3D hija con MeshRenderer", this);
            return;
        }

        _baseBallScale = ball.localScale;

        if (lockIcon != null)
        {
            _lockBaseScale = lockIcon.transform.localScale;
            _lockBaseRot = lockIcon.transform.localRotation;
        }

        visual.Init(ball, ballRenderer);
        glow.Init(ball, ballRenderer);
    }

    void OnDisable()
    {
        if (_unlockCo != null) { StopCoroutine(_unlockCo); _unlockCo = null; }

        receivePulse.Reset();
        unlockAnim.Reset();
        tinter.Flash = 0f;
        glow.Reset();

        if (_unlocking)
            FinishUnlock();
    }

    void Update()
    {
        if (!_valid) return;

        // Un item oculto no necesita animarse
        if (!_visible && !_unlocking) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);

        receivePulse.Tick(dt);
        glow.Tick(dt);

        if (_selected)
            ball.Rotate(0f, spinSpeed * dt, 0f, Space.Self);

        ApplyScale();

        if (_visible)
            tinter.ApplyIfChanged();
    }

    public void Init(SkinData data, int index)
    {
        Index = index;
        if (!_valid) return;

        ApplyBallMaterial(data);
        visual.Spawn(data);
        tinter.Build(visual.GetTintRenderers());
        UpdateLockState(data);
    }

    public void UpdateLockState(SkinData data)
    {
        _locked = !SkinService.IsUnlocked(data);
        tinter.Darkness = _locked ? 1f : 0f;
        tinter.Flash = 0f;

        if (lockIcon != null)
            lockIcon.SetActive((_locked || _unlocking) && visual.Shown);

        visual.SetParticlesActive(!_locked);
        tinter.Refresh();
    }

    void ApplyBallMaterial(SkinData data)
    {
        if (data.ballMaterial != null)
            ballRenderer.sharedMaterial = data.ballMaterial;
        else
            Debug.LogWarning($"Skin '{data.id}' no tiene Ball Material asignado", data);
    }

    public void PlayReceivePulse()
    {
        if (!_valid) return;
        receivePulse.Trigger();
    }

    public void PlayUnlockAnimation(Action onDone = null)
    {
        if (!_valid) { onDone?.Invoke(); return; }

        if (!isActiveAndEnabled)
        {
            _unlocking = true;
            _onUnlockDone = onDone;
            FinishUnlock();
            return;
        }

        if (_unlockCo != null) StopCoroutine(_unlockCo);
        _onUnlockDone = onDone;
        _unlockCo = StartCoroutine(UnlockRoutine());
    }

    IEnumerator UnlockRoutine()
    {
        _unlocking = true;
        receivePulse.Reset();
        if (lockIcon != null) lockIcon.SetActive(visual.Shown);

        yield return unlockAnim.Run(
            tinter,
            onShakeEnd: () =>
            {
                _locked = false;
                visual.SetParticlesActive(true);
                glow.Play();
            },
            onFrame: () =>
            {
                if (visual.Shown) tinter.Apply();
            });

        _unlockCo = null;
        FinishUnlock();
    }

    void FinishUnlock()
    {
        _unlocking = false;
        _locked = false;

        tinter.Darkness = 0f;
        tinter.Flash = 0f;
        unlockAnim.Reset();
        receivePulse.Reset();

        if (lockIcon != null)
        {
            lockIcon.transform.localRotation = _lockBaseRot;
            lockIcon.SetActive(false);
        }
        _appliedBall = float.NaN;
        _appliedLockScale = float.NaN;
        _appliedLockRot = float.NaN;
        if (_valid) ApplyScale();

        visual.SetParticlesActive(true);
        tinter.Refresh();

        Action cb = _onUnlockDone;
        _onUnlockDone = null;
        cb?.Invoke();
    }

    public Vector3 GetBallPosition()
    {
        return ball != null ? ball.position : transform.position;
    }

    public void SetSelected(bool selected)
    {
        if (_selected == selected) return;

        _selected = selected;
        if (!selected && _valid)
            ball.localRotation = Quaternion.identity;
    }

    public void SetScale(float s)
    {
        if (!_valid) return;
        _scale = s;
        ApplyScale();
    }

    void ApplyScale()
    {
        float ballS = _scale * unlockAnim.BallPunch;
        if (!Mathf.Approximately(ballS, _appliedBall))
        {
            _appliedBall = ballS;
            ball.localScale = _baseBallScale * ballS;
        }

        if (lockIcon != null)
        {
            float lockS = _scale * (1f + receivePulse.Punch) * unlockAnim.LockScale;
            if (!Mathf.Approximately(lockS, _appliedLockScale))
            {
                _appliedLockScale = lockS;
                lockIcon.transform.localScale = _lockBaseScale * lockS;
            }

            float rot = unlockAnim.LockRot;
            if (!Mathf.Approximately(rot, _appliedLockRot))
            {
                _appliedLockRot = rot;
                lockIcon.transform.localRotation = _lockBaseRot * Quaternion.Euler(0f, 0f, rot);
            }
        }

        glow.Apply(_baseBallScale.x * _scale, visual.Shown);
    }

    public void SetVisual(float brightness, bool visible)
    {
        if (!_valid) return;

        if (visible == _visible && Mathf.Approximately(brightness, _appliedBrightness))
            return;

        _visible = visible;
        _appliedBrightness = brightness;

        tinter.Brightness = brightness;

        bool ballShouldShow = visible && !visual.UsesPrefab;
        if (ballRenderer.enabled != ballShouldShow)
            ballRenderer.enabled = ballShouldShow;

        visual.SetShown(visible);

        if (lockIcon != null)
        {
            bool lockShouldShow = (_locked || _unlocking) && visible;
            if (lockIcon.activeSelf != lockShouldShow)
                lockIcon.SetActive(lockShouldShow);
        }

        if (!visible) return;
        tinter.ApplyIfChanged();
    }
}