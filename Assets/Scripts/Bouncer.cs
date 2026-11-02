using UnityEngine;

public class Bouncer : MonoBehaviour
{
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private float upwardBoost = 1f;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            Vector3 pushDirection = rb.transform.position - transform.position;

            if (pushDirection.y < upwardBoost)
            {
                pushDirection.y = upwardBoost;
            }

            pushDirection.Normalize();


            rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        }
    }
}