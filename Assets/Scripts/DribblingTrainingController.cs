
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DribblingTrainingController : MonoBehaviour
{
    [Header("Decals fade")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;

    private class LimitData
    {
        public GameObject limit;
        public Collider trigger;
        public DecalProjector decal;
        public bool ballInside;
        public Coroutine fadeRoutine;
    }

    private class GateData
    {
        public GameObject gate;
        public Collider trigger;
        public DecalProjector decal;
        public bool ballInside;
        public Coroutine fadeRoutine;
        public List<LimitData> limits = new List<LimitData>();
    }

    private readonly List<GateData> dribblingGates = new List<GateData>();
    private int currentGateIndex = 0;

    private GameObject ball;
    private BallController ballController;
    private Collider[] ballColliders;

    private void Start()
    {

        ballController = GameObject.FindAnyObjectByType<BallController>();
        ball = ballController.gameObject;

        if (ball == null) return;

        ballColliders = ball.GetComponentsInChildren<Collider>();

        foreach (Transform child in transform)
        {
            Transform limitsParent = child.Find("DribblingLimit");

            Collider gateTrigger = null;

            foreach (Collider col in child.GetComponentsInChildren<Collider>(true))
            {
                if (col.isTrigger &&
                    (limitsParent == null ||
                     !col.transform.IsChildOf(limitsParent)))
                {
                    gateTrigger = col;
                    break;
                }
            }

            DecalProjector decal = null;

            foreach (DecalProjector dp in
                     child.GetComponentsInChildren<DecalProjector>(true))
            {
                if (limitsParent == null ||
                    !dp.transform.IsChildOf(limitsParent))
                {
                    decal = dp;
                    break;
                }
            }

            GateData data = new GateData
            {
                gate = child.gameObject,
                trigger = gateTrigger,
                decal = decal,
                ballInside = false
            };

            if (decal != null)
                SetOpacity(decal, 0f);

            if (limitsParent != null)
            {
                foreach (Transform limitTransform in limitsParent)
                {
                    Collider limitTrigger = null;

                    foreach (Collider col in
                             limitTransform.GetComponentsInChildren<Collider>(true))
                    {
                        if (col.isTrigger)
                        {
                            limitTrigger = col;
                            break;
                        }
                    }

                    DecalProjector limitDecal =
                        limitTransform.GetComponentInChildren<DecalProjector>(true);

                    LimitData limitData = new LimitData
                    {
                        limit = limitTransform.gameObject,
                        trigger = limitTrigger,
                        decal = limitDecal,
                        ballInside = false
                    };

                    data.limits.Add(limitData);

                    if (limitDecal != null)
                        SetOpacity(limitDecal, 0f);

                }
            }

            dribblingGates.Add(data);
        }

        UpdateCurrentDecal();
    }

    private void Update()
    {
        if (ball == null || currentGateIndex >= dribblingGates.Count)
            return;

        GateData currentGate = dribblingGates[currentGateIndex];

        bool isInsideGate = IsBallInside(currentGate.trigger);

        if (isInsideGate && !currentGate.ballInside)
        {
            currentGate.ballInside = true;
        }

        if (!isInsideGate && currentGate.ballInside)
        {
            currentGate.ballInside = false;

            FadeDecal(currentGate, 0f, fadeOutDuration);

            foreach (LimitData limit in currentGate.limits)
            {
                FadeDecal(limit, 0f, fadeOutDuration);
                limit.ballInside = false;
            }

            currentGateIndex++;
            UpdateCurrentDecal();
            return;
        }

        foreach (LimitData limit in currentGate.limits)
        {
            if (limit.trigger == null)
                continue;

            bool isInsideLimit = IsBallInside(limit.trigger);

            if (isInsideLimit && !limit.ballInside)
            {
                limit.ballInside = true;
            }

            if (!isInsideLimit && limit.ballInside)
            {
                limit.ballInside = false;

                if (ballController) ballController.Die(false);
                FastSceneLoader.Instance.RestartLevel();
                return;
            }
        }
    }

    private bool IsBallInside(Collider target)
    {
        if (target == null || !target.enabled)
            return false;

        foreach (Collider ballCollider in ballColliders)
        {
            if (ballCollider == null || !ballCollider.enabled)
                continue;

            if (target.bounds.Intersects(ballCollider.bounds))
                return true;
        }

        return false;
    }

    private void UpdateCurrentDecal()
    {
        if (currentGateIndex >= dribblingGates.Count)
        {
            return;
        }

        GateData gate = dribblingGates[currentGateIndex];

        FadeDecal(gate, 1f, fadeInDuration);

        foreach (LimitData limit in gate.limits)
        {
            FadeDecal(limit, 1f, fadeInDuration);
        }
    }

    private void FadeDecal(GateData gate, float targetOpacity, float duration)
    {
        if (gate.decal == null)
            return;

        if (gate.fadeRoutine != null)
            StopCoroutine(gate.fadeRoutine);

        gate.fadeRoutine = StartCoroutine(
            FadeOpacity(gate.decal, targetOpacity, duration));
    }

    private void FadeDecal(LimitData limit, float targetOpacity, float duration)
    {
        if (limit.decal == null)
            return;

        if (limit.fadeRoutine != null)
            StopCoroutine(limit.fadeRoutine);

        limit.fadeRoutine = StartCoroutine(
            FadeOpacity(limit.decal, targetOpacity, duration));
    }

    private IEnumerator FadeOpacity(
        DecalProjector decal, float targetOpacity, float duration)
    {
        float startOpacity = decal.fadeFactor;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            SetOpacity(decal, targetOpacity);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float opacity = Mathf.Lerp(startOpacity, targetOpacity, t);

            SetOpacity(decal, opacity);

            yield return null;
        }

        SetOpacity(decal, targetOpacity);
    }

    private void SetOpacity(DecalProjector decal, float opacity)
    {
        if (decal != null)
            decal.fadeFactor = Mathf.Clamp01(opacity);
    }
}
