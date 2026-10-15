
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objectives")]
    public Transform ball;
    public Transform goal;

    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0f, 5f, -8f);
    public float followSpeed = 5f;

    [Header("Weights")]
    [Range(0f, 1f)]
    public float goalWeight = 0.4f;

    [Header("Goal Influence")]
    public float fadeStart = 0.25f;
    public float fadeEnd = 0.5f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (ball == null) return;

        // Ball: maximum priority
        Vector3 desiredPosition = ball.position + offset;

        if (goal != null && goalWeight > 0f)
        {
            // Measure how far the goal is from the screen center.
            Vector3 viewport = GetComponent<Camera>()
                .WorldToViewportPoint(goal.position);

            float distanceFromCenter = Mathf.Max(
                Mathf.Abs(viewport.x - 0.5f),
                Mathf.Abs(viewport.y - 0.5f)
            );

            // Reduce the goal's weight as it moves toward the edges.
            float visibilityWeight = 1f - Mathf.SmoothStep(
                fadeStart,
                fadeEnd,
                distanceFromCenter
            );

            if (viewport.z <= 0f)
                visibilityWeight = 0f;

            float weight = goalWeight * visibilityWeight;

            // The goal only influences camera position.
            Vector3 goalPosition = goal.position + offset;

            desiredPosition = Vector3.Lerp(
                desiredPosition,
                goalPosition,
                weight
            );
        }

        // Position only. Rotation is never changed.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            1f / Mathf.Max(followSpeed, 0.01f)
        );
    }
}
