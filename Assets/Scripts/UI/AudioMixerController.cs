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

    [Header("Parámetros del Mixer (deben estar expuestos)")]
    [SerializeField] string musicParam = "MusicVolume";
    [SerializeField] string sfxParam = "SfxVolume";

    const float volumeOn = 0f;
    const float volumeOff = -80f;

    const string musicKey = "MusicSavedState";
    const string sfxKey = "SfxSavedState";

    bool musicOn = true;
    bool sfxOn = true;

    void Awake()
    {
        musicOn = PlayerPrefs.GetInt(musicKey, 1) == 1;
        sfxOn = PlayerPrefs.GetInt(sfxKey, 1) == 1;
    }

    void OnEnable()
    {
        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(musicOn);
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(sfxOn);
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }
    }

    void OnDisable()
    {
        if (musicToggle != null)
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);

        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);
    }

    void Start()
    {
        ApplyVolume(musicParam, musicOn);
        ApplyVolume(sfxParam, sfxOn);
    }

    void OnMusicToggleChanged(bool isOn)
    {
        musicOn = isOn;
        ApplyVolume(musicParam, isOn);
        Save(musicKey, isOn);
    }

    void OnSfxToggleChanged(bool isOn)
    {
        sfxOn = isOn;
        ApplyVolume(sfxParam, isOn);
        Save(sfxKey, isOn);
    }

    void ApplyVolume(string param, bool isOn)
    {
        if (audioMixer == null) return;

        if (!audioMixer.SetFloat(param, isOn ? volumeOn : volumeOff))
            Debug.LogWarning($"El parámetro '{param}' no existe o no está expuesto en el AudioMixer.");
    }

    void Save(string key, bool isOn)
    {
        PlayerPrefs.SetInt(key, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}