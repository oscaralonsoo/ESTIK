using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BallSoundController : MonoBehaviour
{
    [Header("AUDIO SETTINGS")]
    [SerializeField] private AudioClip[] hitAudioClips;
    [SerializeField, Range(0f, 1f)] private float minVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

    [Header("POST AUDIO SETTINGS")]
    [SerializeField] private AudioClip[] postAudioClips;
    [SerializeField, Range(0f, 1f)] private float postMinVolume = 0.1f;
    [SerializeField, Range(0f, 1f)] private float postMaxVolume = 1f;

    [Header("WOOD AUDIO SETTINGS")]
    [SerializeField] private AudioClip[] woodAudioClips;
    [SerializeField, Range(0f, 1f)] private float woodMinVolume = 0.1f;
    [SerializeField, Range(0f, 1f)] private float woodMaxVolume = 1f;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayHitSound(float power01 = 1f)
    {
        if (hitAudioClips == null || hitAudioClips.Length == 0) return;

        int randomIndex = Random.Range(0, hitAudioClips.Length);
        AudioClip clipToPlay = hitAudioClips[randomIndex];

        if (clipToPlay != null)
        {
            float volume = Mathf.Lerp(minVolume, maxVolume, Mathf.Clamp01(power01));
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }

    public void PlayPostSound(float speed01 = 1f)
    {
        if (postAudioClips == null || postAudioClips.Length == 0) return;

        int randomIndex = Random.Range(0, postAudioClips.Length);
        AudioClip clipToPlay = postAudioClips[randomIndex];

        if (clipToPlay != null)
        {
            float volume = Mathf.Lerp(postMinVolume, postMaxVolume, Mathf.Clamp01(speed01));
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }

    public void PlayWoodSound(float speed01 = 1f)
    {
        if (woodAudioClips == null || woodAudioClips.Length == 0) return;

        int randomIndex = Random.Range(0, woodAudioClips.Length);
        AudioClip clipToPlay = woodAudioClips[randomIndex];

        if (clipToPlay != null)
        {
            float volume = Mathf.Lerp(woodMinVolume, woodMaxVolume, Mathf.Clamp01(speed01));
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }
}