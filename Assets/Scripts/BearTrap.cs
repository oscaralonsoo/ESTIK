using UnityEngine;
using System.Collections;

public class BearTrap : MonoBehaviour
{
    [SerializeField] GameObject trap1;
    [SerializeField] GameObject trap2;
    [SerializeField] private Transform trapCenter;

    [Header("TRAP ANIMATION")]
    [SerializeField] private float closeAngle = 65f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private float timeToChange = 1f;

    private BallController ballController;

    bool initialized = true;

    private void Start()
    {
        initialized = true;
        ballController = FindAnyObjectByType<BallController>();
        if (!ballController) initialized = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;
        if (other.CompareTag("Ball"))
        {
            ballController.Die(true);
            StartCoroutine(TrapAnimation());
        }
    }

    private IEnumerator TrapAnimation()
    {
        Quaternion trap1Start = trap1.transform.localRotation;
        Quaternion trap2Start = trap2.transform.localRotation;

        Quaternion trap1End = Quaternion.Euler(closeAngle, 0, 0f);
        Quaternion trap2End = Quaternion.Euler(closeAngle, -180f, 0f);

        Vector3 ballStart = ballController.transform.position;
        Vector3 ballEnd = trapCenter.position;

        float elapsed = 0f;
        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);

            trap1.transform.localRotation = Quaternion.Lerp(trap1Start, trap1End, t);
            trap2.transform.localRotation = Quaternion.Lerp(trap2Start, trap2End, t);
            ballController.transform.position = Vector3.Lerp(ballStart, ballEnd, t * 2);

            yield return null;
        }

        trap1.transform.localRotation = trap1End;
        trap2.transform.localRotation = trap2End;
        ballController.transform.position = ballEnd;

        yield return new WaitForSeconds(timeToChange);
        FastSceneLoader.Instance.RestartLevel();
    }
}
