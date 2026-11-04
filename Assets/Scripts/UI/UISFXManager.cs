using System;
using UnityEngine;

public enum UISoundType
{
    Normal,
    Confirmar,
    Atras,
    Error,
    Toggle,
    Play,
    Buy,
    Win,
    Point
}
public class UISFXManager : MonoBehaviour
{
    [Serializable]
    public class SoundEntry
    {
        public UISoundType type;
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
    public void PlayNormal() => Play(UISoundType.Normal);
    public void PlayConfirmar() => Play(UISoundType.Confirmar);
    public void PlayAtras() => Play(UISoundType.Atras);
    public void PlayError() => Play(UISoundType.Error);
    public void PlayToggle() => Play(UISoundType.Toggle);
    public void PlayBuy() => Play(UISoundType.Buy);
    public void PlayPlay() => Play(UISoundType.Play);

    public void PlayPoint() => Play(UISoundType.Point);
    public void PlayWin() => Play(UISoundType.Win);
    public void Play(UISoundType type)
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