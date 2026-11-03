using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbientMusicManager : MonoBehaviour
{
    [Header("Música")]
    [SerializeField] float fadeInDuration = 1f;
    [SerializeField] float fadeOutDuration = 0.5f;
    [SerializeField] float minDistanceFromLastStart = 10f;
    [SerializeField] float endMargin = 5f;

    static AmbientMusicManager instance;
    static float lastStartTime = -1f;

    AudioSource source;
    float baseVolume;
    Coroutine fadeRoutine;

    void Awake()
    {
        instance = this;
        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
    }

    void Start()
    {
        PlayFromRandomSecond();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void PlayFromRandomSecond()
    {
        if (source.clip == null) return;

        float maxStart = Mathf.Max(0f, source.clip.length - endMargin);
        float start = Random.Range(0f, maxStart);

        for (int i = 0; i < 10 && lastStartTime >= 0f && Mathf.Abs(start - lastStartTime) < minDistanceFromLastStart; i++)
        {
            start = Random.Range(0f, maxStart);
        }

        lastStartTime = start;

        source.volume = 0f;
        source.time = start;
        source.Play();

        StartFade(0f, baseVolume, fadeInDuration);
    }

    public static void FadeOut(float duration = -1f)
    {
        if (instance == null) return;
        instance.StartFade(instance.source.volume, 0f, duration < 0f ? instance.fadeOutDuration : duration);
    }

    public static float GetFadeOutDuration()
    {
        return instance != null ? instance.fadeOutDuration : 0f;
    }

    public static void RunAfterDelay(float seconds, System.Action action)
    {
        if (instance == null) { action?.Invoke(); return; }
        instance.StartCoroutine(instance.DelayRoutine(seconds, action));
    }

    IEnumerator DelayRoutine(float seconds, System.Action action)
    {
        yield return new WaitForSecondsRealtime(seconds);
        action?.Invoke();
    }

    void StartFade(float from, float to, float duration)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(from, to, duration));
    }

    IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        source.volume = to;
        fadeRoutine = null;
    }
}