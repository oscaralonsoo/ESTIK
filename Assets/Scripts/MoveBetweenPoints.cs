using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBetweenPoints : MonoBehaviour
{
    [Header("ROUTE PROPERTIES")]
    [SerializeField] private bool useCurves = false;
    [SerializeField] private List<Transform> points;
    [Tooltip("Solo funciona con useCurves desactivado.")]
    [SerializeField] private bool random = false;
    [SerializeField] private bool restartOnEnd = false;

    [Header("MOVEMENT PROPERTIES")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float waitTime = 1f;

    [Header("ROTATION")]
    [SerializeField] private bool rotateToDirection = true;
    [Tooltip("Activa esto para sprites 2D (rota en el eje Z, mirando hacia la derecha).")]
    [SerializeField] private bool is2D = false;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("GIZMOS")]
    [SerializeField] private int gizmoResolution = 30;

    private const int LengthSamples = 20;

    private bool canMove = false;
    private bool movingForward = true;

    private int currentTargetIndex = 0;

    // Modo curvas
    private int segmentIndex = 0;
    private float t = 0f;
    private float[] segmentLengths;

    private int SegmentCount => (points == null || points.Count < 3) ? 0 : (points.Count - 1) / 2;

    private void Start()
    {
        if (points == null || points.Count == 0) return;

        if (useCurves)
        {
            if (SegmentCount == 0)
            {
                Debug.LogWarning("MoveBetweenPoints: con curvas necesitas al menos 3 puntos (inicio, vértice, final).", this);
                enabled = false;
                return;
            }

            if (points.Count % 2 == 0)
                Debug.LogWarning("MoveBetweenPoints: con curvas necesitas un número impar de puntos. El último se ignorará.", this);

            CacheLengths();
            transform.localPosition = Evaluate(0, 0f);
        }
        else if (random)
        {
            currentTargetIndex = Random.Range(0, points.Count);
        }

        StartCoroutine(WaitCoroutine());
    }

    private void Update()
    {
        if (points == null || points.Count == 0 || !canMove) return;

        if (useCurves) UpdateCurve();
        else UpdateLinear();
    }


    private void UpdateLinear()
    {
        Vector3 targetPosition = points[currentTargetIndex].localPosition;

        RotateTowards(targetPosition - transform.localPosition);

        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.localPosition, targetPosition) < 0.001f)
        {
            SetNextTarget();
        }
    }

    private void SetNextTarget()
    {
        StartCoroutine(WaitCoroutine());
        if (points.Count <= 1) return;

        if (random)
        {
            int newIndex = currentTargetIndex;
            while (newIndex == currentTargetIndex)
            {
                newIndex = Random.Range(0, points.Count);
            }
            currentTargetIndex = newIndex;
        }
        else if (restartOnEnd)
        {
            currentTargetIndex = (currentTargetIndex + 1) % points.Count;
        }
        else
        {
            if (movingForward)
            {
                currentTargetIndex++;
                if (currentTargetIndex >= points.Count)
                {
                    currentTargetIndex = points.Count - 2;
                    movingForward = false;
                }
            }
            else
            {
                currentTargetIndex--;
                if (currentTargetIndex < 0)
                {
                    currentTargetIndex = 1;
                    movingForward = true;
                }
            }
        }
    }


    private void UpdateCurve()
    {
        float length = Mathf.Max(segmentLengths[segmentIndex], 0.0001f);
        float dir = movingForward ? 1f : -1f;
        t += dir * (speed * Time.deltaTime) / length;

        if (movingForward && t >= 1f)
        {
            t = 1f;
            ApplyCurvePosition();
            ArriveAtEnd();
        }
        else if (!movingForward && t <= 0f)
        {
            t = 0f;
            ApplyCurvePosition();
            ArriveAtStart();
        }
        else
        {
            ApplyCurvePosition();
        }

        Vector3 tangent = EvaluateTangent(segmentIndex, Mathf.Clamp01(t));
        if (!movingForward) tangent = -tangent;
        RotateTowards(tangent);
    }

    private void ApplyCurvePosition()
    {
        transform.localPosition = Evaluate(segmentIndex, t);
    }

    private Vector3 Evaluate(int segment, float t)
    {
        Vector3 a = points[segment * 2].localPosition;
        Vector3 b = points[segment * 2 + 1].localPosition;
        Vector3 c = points[segment * 2 + 2].localPosition;

        float l0 = (2f * t - 1f) * (t - 1f);
        float l1 = 4f * t * (1f - t);
        float l2 = t * (2f * t - 1f);

        return a * l0 + b * l1 + c * l2;
    }

    private void ArriveAtEnd()
    {
        if (segmentIndex < SegmentCount - 1)
        {
            segmentIndex++;
            t = 0f;
        }
        else if (restartOnEnd)
        {
            segmentIndex = 0;
            t = 0f;
            ApplyCurvePosition();
        }
        else
        {
            movingForward = false;
            t = 1f;
        }
        StartCoroutine(WaitCoroutine());
    }

    private void ArriveAtStart()
    {
        if (segmentIndex > 0)
        {
            segmentIndex--;
            t = 1f;
        }
        else
        {
            movingForward = true;
            t = 0f;
        }
        StartCoroutine(WaitCoroutine());
    }

    private void CacheLengths()
    {
        segmentLengths = new float[SegmentCount];
        for (int s = 0; s < SegmentCount; s++)
        {
            float len = 0f;
            Vector3 prev = Evaluate(s, 0f);
            for (int i = 1; i <= LengthSamples; i++)
            {
                Vector3 p = Evaluate(s, i / (float)LengthSamples);
                len += Vector3.Distance(prev, p);
                prev = p;
            }
            segmentLengths[s] = len;
        }
    }


    private IEnumerator WaitCoroutine()
    {
        canMove = false;
        yield return new WaitForSeconds(waitTime);
        canMove = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (points == null || points.Count < 2) return;
        foreach (var p in points) if (p == null) return;

        Transform parent = transform.parent;

        if (useCurves)
        {
            if (SegmentCount == 0) return;
            Gizmos.color = Color.cyan;

            for (int s = 0; s < SegmentCount; s++)
            {
                Vector3 prev = ToWorld(parent, Evaluate(s, 0f));
                for (int i = 1; i <= gizmoResolution; i++)
                {
                    Vector3 p = ToWorld(parent, Evaluate(s, i / (float)gizmoResolution));
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
            }
        }
        else
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Gizmos.DrawLine(ToWorld(parent, points[i].localPosition),
                                ToWorld(parent, points[i + 1].localPosition));
            }
        }
    }

    private Vector3 ToWorld(Transform parent, Vector3 local)
    {
        return parent != null ? parent.TransformPoint(local) : local;
    }

    // Derivada de la parábola: hacia dónde apunta la curva en t
    private Vector3 EvaluateTangent(int segment, float t)
    {
        Vector3 a = points[segment * 2].localPosition;
        Vector3 b = points[segment * 2 + 1].localPosition;
        Vector3 c = points[segment * 2 + 2].localPosition;

        float d0 = 4f * t - 3f;
        float d1 = 4f - 8f * t;
        float d2 = 4f * t - 1f;

        return a * d0 + b * d1 + c * d2;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (!rotateToDirection || direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation;

        if (is2D)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(direction.normalized);
        }

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}

