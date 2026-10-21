using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform goal;

    [Header("Normal Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Shoot Mode Settings")]
    [SerializeField] private bool isShooting = false;
    [SerializeField] private float shootDistance = 4f;
    [SerializeField] private float shootHeight = 2.2f;
    [SerializeField] private float shootSpeed = 12f;
    [SerializeField] private float lookAtHeightOffset = 0.3f;

    private bool locked = false;

    private void Start()
    {
        target = FindAnyObjectByType<BallController>().transform;
        goal = FindAnyObjectByType<GoalController>().transform;

        if (target == null) return;

        transform.position = GetDesiredPosition();
        transform.rotation = GetDesiredRotation();
    }

    private void LateUpdate()
    {
        if (target == null || locked)
            return;

        Vector3 targetPosition = GetDesiredPosition();
        Quaternion targetRotation = GetDesiredRotation();

        float speed = isShooting ? shootSpeed : followSpeed;
        float rotSpeed = isShooting ? shootSpeed : rotationSpeed;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            speed * Time.unscaledDeltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotSpeed * Time.unscaledDeltaTime
        );
    }

    private Vector3 GetDesiredPosition()
    {
        if (isShooting && goal != null)
        {
            Vector3 dirToGoal = (goal.position - target.position);
            dirToGoal.y = 0f;

            if (dirToGoal.sqrMagnitude > 0.001f)
                dirToGoal.Normalize();
            else
                dirToGoal = transform.forward;

            return target.position - (dirToGoal * shootDistance) + (Vector3.up * shootHeight);
        }

        return target.position + offset;
    }

    private Quaternion GetDesiredRotation()
    {
        if (isShooting && goal != null)
        {
            Vector3 lookTargetPosition = target.position + (Vector3.up * lookAtHeightOffset);
            Vector3 lookDirection = lookTargetPosition - transform.position;

            if (lookDirection.sqrMagnitude > 0.001f)
                return Quaternion.LookRotation(lookDirection);
        }

        return Quaternion.LookRotation(target.position - transform.position);
    }

    public void SetShooting(bool shooting)
    {
        isShooting = shooting;
    }

    public void LockCamera()
    {
        locked = true;
    }
}