using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BallSoundController : MonoBehaviour
{
    [Header("AUDIO SETTINGS")]
    [SerializeField] private AudioClip[] hitAudioClips;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayHitSound()
    {
        if (hitAudioClips == null || hitAudioClips.Length == 0) return;

        int randomIndex = Random.Range(0, hitAudioClips.Length);
        AudioClip clipToPlay = hitAudioClips[randomIndex];

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}