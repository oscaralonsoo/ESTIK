using System.Collections;
using UnityEngine;

public class GoalController : MonoBehaviour
{
    private FastSceneLoader loader;

    private void Awake()
    {
        loader = FindAnyObjectByType<FastSceneLoader>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            StartCoroutine(TriggerChangeCoroutine());
        }
    }

    private IEnumerator TriggerChangeCoroutine()
    {
        yield return new WaitForSeconds(1f);
        loader.TriggerInstantChange();
    }
}