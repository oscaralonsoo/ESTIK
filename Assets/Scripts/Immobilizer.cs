using UnityEngine;

public class Immobilizer : MonoBehaviour
{
    [SerializeField] private float dampingSpeed = 1f;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * dampingSpeed);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * dampingSpeed);
        }
    }
}
