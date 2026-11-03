using UnityEngine;

public class WaterSplash : MonoBehaviour
{
    [SerializeField] private GameObject splashPrefab;
    [SerializeField] private AudioClip splashSound;
    [SerializeField][Range(0f, 1f)] private float splashVolume = 1f;
    [SerializeField] private float cooldown = 0.25f;

    private Collider waterCollider;
    private float lastSplashTime = -10f;

    private void Awake() => waterCollider = GetComponent<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<BallController>() == null) return;
        if (Time.unscaledTime - lastSplashTime < cooldown) return;
        lastSplashTime = Time.unscaledTime;

        Vector3 pos = other.attachedRigidbody != null
            ? other.attachedRigidbody.position
            : other.transform.position;
        pos.y = waterCollider.bounds.max.y;

        Instantiate(splashPrefab, pos, Quaternion.identity);

        if (splashSound != null) AudioSource.PlayClipAtPoint(splashSound, pos, splashVolume);
    }
}