using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FastSceneLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private float transitionTime = 0.5f;
    [SerializeField] private float maxScale = 30f;

    private AsyncOperation asyncOperation;
    private Camera mainCam;
    private Image image;
    private BallController ballController;
    private bool initialized = true;

    private void Awake()
    {
        image = GetComponentInChildren<Image>(true);
        ballController = FindAnyObjectByType<BallController>();
        mainCam = Camera.main;

        if (!image || !ballController || !mainCam)
        {
            initialized = false;
            Debug.LogError($"[FastSceneLoader] Missing reference on {gameObject.name}!");
            return;
        }

        image.gameObject.SetActive(true);
        image.rectTransform.localScale = Vector3.one * maxScale;
    }

    private IEnumerator Start()
    {
        if (initialized)
        {
            yield return StartCoroutine(EnterSceneRoutine());
        }

        StartCoroutine(PreloadNextScene());
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            TriggerInstantChange();
        }
    }

    private IEnumerator PreloadNextScene()
    {
        asyncOperation = SceneManager.LoadSceneAsync(nextSceneName);
        asyncOperation.allowSceneActivation = false;

        while (asyncOperation.progress < 0.9f)
        {
            yield return null;
        }
    }

    public void TriggerInstantChange()
    {
        StartCoroutine(ExitSceneRoutine());
    }

    private IEnumerator EnterSceneRoutine()
    {
        yield return new WaitForEndOfFrame();
        yield return null;

        RectTransform imageRect = image.rectTransform;
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

    private IEnumerator ExitSceneRoutine()
    {
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

        if (asyncOperation != null)
        {
            asyncOperation.allowSceneActivation = true;
        }
    }
}