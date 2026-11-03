using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class GoalController : MonoBehaviour
{
    [SerializeField] private float timeToChange = 1f;
    [SerializeField] private ParticleSystem goalParticles;

    [Header("Sonido")]
    [SerializeField] private AudioClip goalSound;
    [Tooltip("Grupo del AudioMixer por el que sale el sonido (p. ej. SFX).")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [Range(0f, 1f)]
    [SerializeField] private float goalSoundVolume = 1f;

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

            if (FastSceneLoader.Instance != null)
            {
                FastSceneLoader.Instance.NotifyGoalScored();
            }

            Vector3 hitPosition = other.ClosestPoint(transform.position);

            PlayGoalSound(hitPosition);

            if (goalParticles != null)
            {
                goalParticles.transform.position = hitPosition;
                goalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                goalParticles.Play();
            }

            if (pointCollector != null)
            {
                pointCollector.SpawnPointsEffect(hitPosition);
            }

            StartCoroutine(TriggerChangeCoroutine());
        }
    }

    private void PlayGoalSound(Vector3 fallbackPosition)
    {
        if (goalSound == null) return;

        Vector3 pos = Camera.main != null ? Camera.main.transform.position : fallbackPosition;

        GameObject sfxObj = new GameObject("GoalSFX");
        sfxObj.transform.position = pos;
        DontDestroyOnLoad(sfxObj);

        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.clip = goalSound;
        source.volume = goalSoundVolume;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.Play();

        Destroy(sfxObj, goalSound.length + 0.1f);
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