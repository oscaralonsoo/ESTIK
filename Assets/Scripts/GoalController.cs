using System.Collections;
using UnityEngine;

public class GoalController : MonoBehaviour
{
    [SerializeField] private float timeToChange = 1f;
    [SerializeField] private ParticleSystem goalParticles;

    private PointCollector pointCollector;
    private bool goalTriggered = false;

    private void Awake()
    {
        pointCollector = GetComponent<PointCollector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && !goalTriggered)
        {
            goalTriggered = true;

            Vector3 hitPosition = other.ClosestPoint(transform.position);

            if (goalParticles != null)
            {
                goalParticles.transform.position = hitPosition;
                goalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                goalParticles.Play();
            }

            // Llama a la secuencia en el PointCollector pasándole la posición del impacto
            if (pointCollector != null)
            {
                pointCollector.SpawnPointsEffect(hitPosition);
            }

            StartCoroutine(TriggerChangeCoroutine());
        }
    }

    private IEnumerator TriggerChangeCoroutine()
    {
        yield return new WaitForSeconds(timeToChange);

        if (FastSceneLoader.Instance != null)
        {
            FastSceneLoader.Instance.LoadNextLevel();
        }
    }
}