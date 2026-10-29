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

            if (goalParticles != null)
            {
                goalParticles.transform.position =
                    other.ClosestPoint(transform.position);

                goalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                goalParticles.Play();
            }

            // 1. Hacemos que el marcador aparezca un poco antes de que lleguen los puntos
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.ShowScore();
            }

            // 2. Lanzamos el efecto visual de los puntos desde la posición de la bola
            if (pointCollector != null)
            {
                pointCollector.SpawnPointsEffect(other.transform.position);
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