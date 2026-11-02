using UnityEngine;

public class Bouncer : MonoBehaviour
{
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private float upwardBoost = 1f;
    [SerializeField] private bool trigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!trigger) return;
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
    
    private void OnCollisionEnter(Collision other)
    {
        if (trigger) return;
        Rigidbody rb = other.rigidbody;
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