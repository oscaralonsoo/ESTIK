using UnityEngine;

public class DefenderController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("If empty, an object with the given tag is searched for.")]
    [SerializeField] private Transform ball;
    [SerializeField] private string ballTag = "Ball";

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [Tooltip("Extra range before the defender gives up chasing.")]
    [SerializeField] private float loseRangeMargin = 1.5f;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [Tooltip("Distance at which the defender stops when reaching the ball.")]
    [SerializeField] private float stopDistance = 0.8f;

    [Header("Push")]
    [Tooltip("Distance at which the defender pushes the ball (should be >= Stop Distance).")]
    [SerializeField] private float pushDistance = 1.2f;
    [Tooltip("Exact velocity the ball gets after a push, in units per second.")]
    [SerializeField] private float pushSpeed = 8f;
    [Tooltip("Vertical component of the push direction (0 = along the ground).")]
    [SerializeField] private float pushUpward = 0.1f;
    [Tooltip("Seconds the defender stays still after pushing.")]
    [SerializeField] private float pauseDuration = 1.5f;
    [Tooltip("Maximum height difference between ball and defender to allow a push.")]
    [SerializeField] private float maxPushHeight = 1.5f;

    private Vector3 homePosition;
    private bool isChasing;
    private Rigidbody ballRb;
    private float pausedUntil;

    private void Start()
    {
        homePosition = transform.position;

        if (ball == null)
        {
            GameObject ballObj = GameObject.FindGameObjectWithTag(ballTag);
            if (ballObj != null) ball = ballObj.transform;
        }

        if (ball != null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (ball == null) return;
        if (Time.time < pausedUntil) return;

        float distanceToBall = FlatDistance(transform.position, ball.position);

        if (!isChasing && distanceToBall <= detectionRange)
            isChasing = true;
        else if (isChasing && distanceToBall > detectionRange + loseRangeMargin)
            isChasing = false;

        if (isChasing)
        {
            if (distanceToBall > stopDistance)
                MoveTowards(ball.position, chaseSpeed);
            else
                LookAt(ball.position);

            TryPushBall(distanceToBall);
        }
        else if (FlatDistance(transform.position, homePosition) > 0.1f)
        {
            MoveTowards(homePosition, returnSpeed);
        }
    }

    private void TryPushBall(float distanceToBall)
    {
        if (ballRb == null) return;
        if (distanceToBall > pushDistance) return;
        if (Mathf.Abs(ball.position.y - transform.position.y) > maxPushHeight) return;

        Vector3 direction = ball.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

        direction = (direction.normalized + Vector3.up * pushUpward).normalized;

        SetBallVelocity(direction * pushSpeed);
        pausedUntil = Time.time + pauseDuration;
    }

    private void SetBallVelocity(Vector3 velocity)
    {
        ballRb.linearVelocity = velocity;
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);

        LookAt(flatTarget);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);
    }

    private void LookAt(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange + loseRangeMargin);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pushDistance);
    }
}