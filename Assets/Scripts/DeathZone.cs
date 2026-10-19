using UnityEngine;
using System.Collections;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private float timeToChange = 1f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            StartCoroutine(TriggerChangeCoroutine());
        }
    }

    private IEnumerator TriggerChangeCoroutine()
    {
        yield return new WaitForSeconds(timeToChange);
        FastSceneLoader.Instance.RestartLevel();
    }
}
