using UnityEngine;

public class SnowFollowCamera : MonoBehaviour
{
    public Transform target; 
    public float heightAbove = 10f;

    void LateUpdate()
    {
        if (target == null)
        {
            if (Camera.main == null) return;
            target = Camera.main.transform;
        }

        Vector3 p = target.position;
        transform.position = new Vector3(p.x, p.y + heightAbove, p.z);
        transform.rotation = Quaternion.identity;
    }
}