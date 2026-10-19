using UnityEngine;

public class WindBehaviour : MonoBehaviour
{
    [Header("WIND PROPERTIES")]
    [SerializeField] bool useCustomDirection = false;
    [SerializeField] Vector3 windDirection = Vector3.zero;
    [SerializeField] float windForce = 1;


    private void OnTriggerStay(Collider other)
    {
        if (!other.attachedRigidbody) return;

        Vector3 wind = Vector3.zero;

        if (useCustomDirection) wind = windDirection.normalized * windForce;
        else wind = transform.forward.normalized * windForce;

        other.attachedRigidbody.AddForce(wind, ForceMode.Impulse);
    }
}
