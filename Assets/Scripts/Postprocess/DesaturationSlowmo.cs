
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class DesaturationSlowmo : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 0.2f;

    private Volume postProcessVolume;
    private Coroutine transitionCoroutine;
    private bool isActive;

    private void Awake()
    {
        postProcessVolume = GetComponent<Volume>();

        if (postProcessVolume == null)
            postProcessVolume = GetComponentInChildren<Volume>(true);

        isActive = postProcessVolume != null &&
                   postProcessVolume.weight > 0f;
    }

    public void ActivatePostProcess()
    {
        if (isActive)
            return;

        isActive = true;
        gameObject.SetActive(true);
        StartTransition(0.4f);
    }

    public void DeactivatePostProcess()
    {
        if (!isActive)
            return;

        isActive = false;
        StartTransition(0f);
    }

    private void StartTransition(float targetWeight)
    {
        if (postProcessVolume == null)
            return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(
            ChangeWeight(targetWeight)
        );
    }

    private IEnumerator ChangeWeight(float targetWeight)
    {
        float startWeight = postProcessVolume.weight;
        float elapsed = 0f;

        if (transitionDuration <= 0f)
        {
            postProcessVolume.weight = targetWeight;
        }
        else
        {
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / transitionDuration
                );

                postProcessVolume.weight = Mathf.Lerp(
                    startWeight,
                    targetWeight,
                    t
                );

                yield return null;
            }

            postProcessVolume.weight = targetWeight;
        }

        transitionCoroutine = null;

        if (targetWeight == 0f)
            gameObject.SetActive(false);
    }
}
