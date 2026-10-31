using UnityEngine;

public class StickBehaviour : MonoBehaviour
{
    [SerializeField] private float maxShootRotation;

    public void SetAimAndFill(Vector3 basePosition, Quaternion baseRotation, float amount)
    {
        float swingAngle = Mathf.LerpUnclamped(0, maxShootRotation, amount);

        Quaternion swingRotation = Quaternion.Euler(swingAngle, 0f, 0f);

        Quaternion finalRotation = baseRotation * swingRotation;

        transform.SetPositionAndRotation(basePosition, finalRotation);
    }
}
