using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float radius = 40f;
    public float height = 15f;
    public float speed = 8f;         
    public float bobAmount = 2f;   
    public float bobSpeed = 0.3f;
    public float lookHeight = 2f;   

    float angle;

    void Start()
    {
        Vector3 offset = transform.position - target.position;
        angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        angle += speed * Time.unscaledDeltaTime;
        float rad = angle * Mathf.Deg2Rad;
        float y = height + Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmount;

        transform.position = target.position + new Vector3(Mathf.Cos(rad) * radius, y, Mathf.Sin(rad) * radius);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}