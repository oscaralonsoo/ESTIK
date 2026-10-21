
using System.Collections;
using UnityEngine;

public class GoalController : MonoBehaviour
{
    [SerializeField] private float timeToChange = 1f;
    [SerializeField] private ParticleSystem goalParticles;

    private bool goalTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && !goalTriggered)
        {
            goalTriggered = true;

            if (goalParticles != null)
            {
                goalParticles.transform.position =
                    other.ClosestPoint(transform.position);

                goalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                goalParticles.Play();
            }

            StartCoroutine(TriggerChangeCoroutine());
        }
    }

    private IEnumerator TriggerChangeCoroutine()
    {
        yield return new WaitForSeconds(timeToChange);
        FastSceneLoader.Instance.LoadNextLevel();
    }
}
