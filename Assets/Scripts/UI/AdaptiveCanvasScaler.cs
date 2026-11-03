using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(CanvasScaler))]
public class AdaptiveCanvasScaler : MonoBehaviour
{
    public Vector2 referenceResolution = new Vector2(1080f, 1920f);

    CanvasScaler scaler;
    int lastW, lastH;

    void OnEnable() => Apply();

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) Apply();
    }

    void Apply()
    {
        if (scaler == null) scaler = GetComponent<CanvasScaler>();
        lastW = Screen.width;
        lastH = Screen.height;
        if (lastW <= 0 || lastH <= 0) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = referenceResolution;

        float screenAspect = (float)lastW / lastH;
        float refAspect = referenceResolution.x / referenceResolution.y;
        scaler.matchWidthOrHeight = screenAspect >= refAspect ? 1f : 0f;
    }
}
