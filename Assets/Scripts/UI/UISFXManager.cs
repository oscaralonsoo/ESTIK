using System;
using UnityEngine;

public enum ButtonSoundType
{
    Normal,
    Confirmar,
    Atras,
    Error,
    Toggle,
    SkinSelector,
    Play 
}
public class UISFXManager : MonoBehaviour
{
    [Serializable]
    public class SoundEntry
    {
        public ButtonSoundType type;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = Vector2.one;
    }

    [SerializeField] private AudioSource source;
    [SerializeField] private SoundEntry[] sounds;

    private void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        if (source == null)
        {
            Debug.LogError("[UISFXManager] Falta el AudioSource.", this);
            return;
        }

        source.playOnAwake = false;
        source.ignoreListenerPause = true;
    }
    public void PlayNormal() => Play(ButtonSoundType.Normal);
    public void PlayConfirmar() => Play(ButtonSoundType.Confirmar);
    public void PlayAtras() => Play(ButtonSoundType.Atras);
    public void PlayError() => Play(ButtonSoundType.Error);
    public void PlayToggle() => Play(ButtonSoundType.Toggle);

    public void PlayPlay() => Play(ButtonSoundType.Play);

    public void PlaySkinSelector() => Play(ButtonSoundType.SkinSelector);
    public void Play(ButtonSoundType type)
    {
        if (source == null || sounds == null)
        {
            Debug.LogWarning("[UISFXManager] Sin AudioSource o lista de sonidos vacía.", this);
            return;
        }

        foreach (SoundEntry entry in sounds)
        {
            if (entry.type != type) continue;

            if (entry.clips == null || entry.clips.Length == 0)
            {
                Debug.LogWarning($"[UISFXManager] El tipo {type} no tiene clips asignados.", this);
                return;
            }

            AudioClip clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
            if (clip == null)
            {
                Debug.LogWarning($"[UISFXManager] Clip vacío en el tipo {type}.", this);
                return;
            }

            float minPitch = entry.pitchRange.x > 0.01f ? entry.pitchRange.x : 1f;
            float maxPitch = entry.pitchRange.y > 0.01f ? entry.pitchRange.y : minPitch;
            float volume = entry.volume > 0.001f ? entry.volume : 1f;

            source.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            source.PlayOneShot(clip, volume);
            return;
        }

        Debug.LogWarning($"[UISFXManager] No hay ninguna entrada para el tipo {type}.", this);
    }
}