using System.Collections;
using TMPro;
using UnityEngine;

public class GameOverPanel : MonoBehaviour
{
    [Header("PUNTOS")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private GameObject pointsRoot;

    [Header("NIVEL")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject newRecordObject;

    [Header("Cascada")]
    [SerializeField] private float startDelay = 0.15f;
    [SerializeField] private float stagger = 0.18f;
    [SerializeField] private float popDuration = 0.4f;
    [SerializeField] private float popOvershoot = 1.7f;

    [Header("New Record Pulse")]
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseSpeed = 5f;

    Coroutine _routine;
    Transform _pointsTr, _levelTr, _recordTr;

    void OnEnable()
    {
        int points = PointsManager.RunTotal;
        int levelReached = Mathf.Max(0, LevelLoader.GetCurrentLevel());

        if (pointsText) pointsText.text = points.ToString();
        if (levelText) levelText.text = levelReached.ToString();

        _pointsTr = pointsRoot ? pointsRoot.transform : (pointsText ? pointsText.transform : null);
        _levelTr = levelText ? levelText.transform : null;

        bool isRecord = LevelLoader.IsNewRecord;
        _recordTr = newRecordObject ? newRecordObject.transform : null;

        if (pointsRoot) pointsRoot.SetActive(true);
        if (newRecordObject) newRecordObject.SetActive(isRecord);

        SetScale(_pointsTr, 0f);
        SetScale(_levelTr, 0f);
        if (isRecord) SetScale(_recordTr, 0f);

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Sequence(isRecord));
    }

    void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        SetScale(_pointsTr, 1f);
        SetScale(_levelTr, 1f);
        SetScale(_recordTr, 1f);
    }

    IEnumerator Sequence(bool isRecord)
    {
        yield return Wait(startDelay);

        StartCoroutine(Pop(_pointsTr));
        yield return Wait(stagger);

        StartCoroutine(Pop(_levelTr));
        yield return Wait(stagger);

        if (isRecord && _recordTr != null)
        {
            yield return Pop(_recordTr);
            float t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * pulseSpeed)), 3f);
                SetScale(_recordTr, 1f + (pulseScale - 1f) * beat);
                yield return null;
            }
        }

        _routine = null;
    }

    IEnumerator Pop(Transform tr)
    {
        if (tr == null) yield break;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popDuration);

            float x = k - 1f;
            float s = 1f + (popOvershoot + 1f) * x * x * x + popOvershoot * x * x;

            SetScale(tr, Mathf.Max(0f, s));
            yield return null;
        }

        SetScale(tr, 1f);
    }

    static IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static void SetScale(Transform tr, float s)
    {
        if (tr != null) tr.localScale = Vector3.one * s;
    }
}