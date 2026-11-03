using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBetweenPoints : MonoBehaviour
{
    [Header("ROUTE PROPERTIES")]
    [SerializeField] private List<Transform> points;
    [SerializeField] private bool random = false;
    [SerializeField] private bool restartOnEnd = false;

    [Header("MOVEMENT PROPERTIES")]
    [SerializeField] private float speed;
    [SerializeField] private float waitTime = 1;

    private int currentTargetIndex = 0;
    private bool movingForward = true;

    private bool canMove = false;

    private void Start()
    {
        if (points != null && points.Count > 0 && random)
        {
            currentTargetIndex = Random.Range(0, points.Count);
        }
        StartCoroutine(WaitCoroutine());
    }

    private void Update()
    {
        if (points == null || points.Count == 0) return;

        if (canMove)
        {
            Vector3 targetPosition = points[currentTargetIndex].localPosition;

            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.localPosition, targetPosition) < 0.001f)
            {
                SetNextTarget();
            }
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

    private IEnumerator WaitCoroutine()
    {
        canMove = false;
        yield return new WaitForSeconds(waitTime);
        canMove = true;
    }
}


