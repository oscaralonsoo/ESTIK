using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioMixerController : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] AudioMixer audioMixer;

    [Header("Toggles")]
    [SerializeField] Toggle musicToggle;
    [SerializeField] Toggle sfxToggle;

    [Header("Parámetros del Mixer")]
    [SerializeField] string musicParam = "MusicVolume";
    [SerializeField] string sfxParam = "SfxVolume";

    const float volumeOn = 0f;
    const float volumeOff = -80f;

    const string musicKey = "MusicSavedState";
    const string sfxKey = "SfxSavedState";

    void Start()
    {
        bool musicSavedState = PlayerPrefs.GetInt(musicKey, 1) == 1;
        bool sfxSavedState = PlayerPrefs.GetInt(sfxKey, 1) == 1;

        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(SetMusicVolume);

        if (sfxToggle != null)
            sfxToggle.onValueChanged.AddListener(SetSfxVolume);

        if (musicToggle != null)
            musicToggle.isOn = musicSavedState;

        if (sfxToggle != null)
            sfxToggle.isOn = sfxSavedState;

        SetMusicVolume(musicSavedState);
        SetSfxVolume(sfxSavedState);
    }

    void OnDestroy()
    {
        if (musicToggle != null)
            musicToggle.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveListener(SetSfxVolume);
    }

    public void SetMusicVolume(bool isOn)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(musicParam, isOn ? volumeOn : volumeOff);

        PlayerPrefs.SetInt(musicKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(bool isOn)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(sfxParam, isOn ? volumeOn : volumeOff);

        PlayerPrefs.SetInt(sfxKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}