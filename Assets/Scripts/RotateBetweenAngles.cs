using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBetweenAngles : MonoBehaviour
{
    [Header("ROUTE PROPERTIES (Angles / Euler)")]
    [SerializeField] private List<Vector3> rotationPoints;
    [SerializeField] private bool random = false;
    [SerializeField] private bool restartOnEnd = false;

    [Header("ROTATION PROPERTIES")]
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private float waitTime = 1f;

    private int currentTargetIndex = 0;
    private bool movingForward = true;
    private bool canMove = false;

    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float elapsedTime = 0f;
    private float journeyLength = 0f;

    private void Start()
    {
        if (rotationPoints != null && rotationPoints.Count > 0 && random)
        {
            currentTargetIndex = Random.Range(0, rotationPoints.Count);
        }

        if (rotationPoints != null && rotationPoints.Count > 0)
        {
            transform.localRotation = Quaternion.Euler(rotationPoints[currentTargetIndex]);
        }

        StartCoroutine(WaitCoroutine());
    }

    private void Update()
    {
        if (rotationPoints == null || rotationPoints.Count == 0) return;

        if (canMove)
        {
            elapsedTime += Time.deltaTime;

            float rawPercent = elapsedTime / rotationDuration;

            float smoothedPercent = Mathf.SmoothStep(0f, 1f, rawPercent);

            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, smoothedPercent);

            if (rawPercent >= 1f || Quaternion.Angle(transform.localRotation, targetRotation) < 0.1f)
            {
                transform.localRotation = targetRotation;
                SetNextTarget();
            }
        }
    }

    private void PrepareNextRotation()
    {
        startRotation = transform.localRotation;
        targetRotation = Quaternion.Euler(rotationPoints[currentTargetIndex]);
        elapsedTime = 0f;
    }

    private void SetNextTarget()
    {
        StartCoroutine(WaitCoroutine());
        if (rotationPoints.Count <= 1) return;

        if (random)
        {
            int newIndex = currentTargetIndex;
            while (newIndex == currentTargetIndex)
            {
                newIndex = Random.Range(0, rotationPoints.Count);
            }
            currentTargetIndex = newIndex;
        }
        else if (restartOnEnd)
        {
            currentTargetIndex = (currentTargetIndex + 1) % rotationPoints.Count;
        }
        else
        {
            if (movingForward)
            {
                currentTargetIndex++;
                if (currentTargetIndex >= rotationPoints.Count)
                {
                    currentTargetIndex = rotationPoints.Count - 2;
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

    private IEnumerator WaitCoroutine()
    {
        canMove = false;
        yield return new WaitForSeconds(waitTime);

        // Preparamos el siguiente punto justo antes de volver a movernos
        PrepareNextRotation();

        canMove = true;
    }
}