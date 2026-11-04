using System.Collections;
using UnityEngine;

public class GoalKeeper : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Transform goalLine;
    [SerializeField] Animator animator;

    [Header("Tiempo que tarda cada animación en llegar al punto de contacto (segundos)")]
    [SerializeField] float diveLeftContactTime = 0.5f;
    [SerializeField] float diveRightContactTime = 0.5f;

    [Header("Ajustes")]
    [SerializeField] float centerZone = 1f;
    [SerializeField] float trackingSpeed = 10f;
    [SerializeField, Range(0f, 1f)] float positionBetweenGoalAndBall = 0.5f;

    Rigidbody ballRb;
    bool shotHandled;
    Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;

        BallController ballController = FindAnyObjectByType<BallController>();
        if (!ballController) return;
        ballRb = ballController.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (ballRb && !shotHandled && goalLine)
        {
            Vector3 dir = ballRb.transform.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            float targetX = Mathf.Lerp(goalLine.position.x, ballRb.transform.position.x, positionBetweenGoalAndBall);

            Vector3 targetPosition = new Vector3(targetX, initialPosition.y, initialPosition.z);

            transform.position = Vector3.Lerp(transform.position, targetPosition, trackingSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (shotHandled || !ballRb) return;
        if (other.attachedRigidbody != ballRb) return;

        Vector3 toBall = ballRb.position - goalLine.position;
        Vector3 vel = ballRb.linearVelocity;

        float dist = Vector3.Dot(toBall, goalLine.forward);
        float speedToGoal = -Vector3.Dot(vel, goalLine.forward);
        if (speedToGoal < 0.1f) return;

        float timeToArrive = dist / speedToGoal;

        float lateral = Vector3.Dot(toBall, goalLine.right);
        float predicted = lateral + Vector3.Dot(vel, goalLine.right) * timeToArrive;

        shotHandled = true;
        StartCoroutine(SaveRoutine(timeToArrive, predicted));
    }

    IEnumerator SaveRoutine(float timeToArrive, float predicted)
    {
        string trigger;
        float contactTime;

        if (predicted < 0f)
        {
            trigger = "SaveLeft";
            contactTime = diveLeftContactTime;
        }
        else
        {
            trigger = "SaveRight";
            contactTime = diveRightContactTime;
        }

        float delay = timeToArrive - contactTime;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        animator.SetTrigger(trigger);
    }

    public void ResetKeeper()
    {
        StopAllCoroutines();
        shotHandled = false;
        initialPosition = transform.position;
    }
}