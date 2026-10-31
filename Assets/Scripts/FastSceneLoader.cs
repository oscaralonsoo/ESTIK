using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FastSceneLoader : MonoBehaviour
{
    public static FastSceneLoader Instance { get; private set; }

    [Header("SETTINGS")]
    [SerializeField] private float transitionTime = 0.5f;
    [SerializeField] private float maxScale = 30f;

    private Camera mainCam;
    private Image image;
    private BallController ballController;
    private bool initialized = true;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        image = GetComponentInChildren<Image>(true);
        ballController = FindAnyObjectByType<BallController>();
        mainCam = Camera.main;

        if (!image || !ballController || !mainCam)
        {
            initialized = false;
            Debug.LogError($"[FastSceneLoader] Missing reference on {gameObject.name}!");
            return;
        }

        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);

        image.gameObject.SetActive(true);
        imageRect.localScale = Vector3.one * maxScale;

    }

    private IEnumerator Start()
    {
        if (initialized)
        {
            yield return StartCoroutine(EnterSceneRoutine());
        }
    }

    public void StartPlaying()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitSceneRoutine(() => LevelLoader.StartGame()));
    }

    public void LoadNextLevel()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitSceneRoutine(LevelLoader.LoadNext));
    }

    public void RestartLevel()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitSceneRoutine(LevelLoader.Reload));
    }

    public void GoToMenu()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitSceneRoutine(LevelLoader.LoadMenu));
    }

    public void SetTutorialPlayed()
    {
        PlayerPrefs.SetInt("HasCompletedTutorial", 1);
        PlayerPrefs.Save();
    }

    private IEnumerator EnterSceneRoutine()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();

        RectTransform imageRect = image.rectTransform;

        if (ballController != null && mainCam != null)
        {
            imageRect.position = mainCam.WorldToScreenPoint(ballController.transform.position);
        }

        float elapsed = 0f;

        while (elapsed < transitionTime)
        {
            if (ballController != null && mainCam != null)
            {
                imageRect.position = mainCam.WorldToScreenPoint(ballController.transform.position);
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            elapsed += dt;

            float progress = Mathf.Clamp01(elapsed / transitionTime);
            float currentScale = Mathf.Lerp(maxScale, 0f, Mathf.SmoothStep(0f, 1f, progress));
            imageRect.localScale = Vector3.one * currentScale;

            yield return null;
        }

        imageRect.localScale = Vector3.zero;
        image.gameObject.SetActive(false);
    }

    private IEnumerator ExitSceneRoutine(System.Func<AsyncOperation> loadSceneAsync)
    {
        isTransitioning = true;

        if (initialized)
        {
            RectTransform imageRect = image.rectTransform;
            image.gameObject.SetActive(true);

            float elapsed = 0f;

            while (elapsed < transitionTime)
            {
                if (ballController != null && mainCam != null)
                {
                    imageRect.position = mainCam.WorldToScreenPoint(ballController.transform.position);
                }

                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
                elapsed += dt;

                float progress = Mathf.Clamp01(elapsed / transitionTime);
                float currentScale = Mathf.Lerp(0f, maxScale, Mathf.SmoothStep(0f, 1f, progress));
                imageRect.localScale = Vector3.one * currentScale;

                yield return null;
            }

            imageRect.localScale = Vector3.one * maxScale;
        }

        AsyncOperation op = loadSceneAsync();
        if (op != null)
        {
            while (!op.isDone)
            {
                yield return null;
            }
        }
    }
}