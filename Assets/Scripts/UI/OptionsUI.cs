using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    public void ApplySaved()
    {
        float music = PlayerPrefs.GetFloat("music", 0.8f);
        float sfx = PlayerPrefs.GetFloat("sfx", 0.8f);
        SetMixer("Music", music);
        SetMixer("SFX", sfx);
        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);
    }

    public void SetMusic(float v)
    {
        PlayerPrefs.SetFloat("music", v);
        SetMixer("Music", v);
    }

    public void SetSfx(float v)
    {
        PlayerPrefs.SetFloat("sfx", v);
        SetMixer("SFX", v);
    }

    void SetMixer(string param, float v) =>
        mixer.SetFloat(param, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
}