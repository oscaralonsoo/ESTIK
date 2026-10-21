using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public OrbitCamera orbitCamera;
    public CameraFollow cameraController;
    public GameObject menuCanvas;
    public GameObject ball;

    void Start()
    {
        // Estado de menú
        orbitCamera.enabled = true;
        cameraController.enabled = false;
        if (ball) ball.SetActive(false);
    }

    public void OnPlayPressed()
    {
        orbitCamera.enabled = false;
        cameraController.enabled = true;

        menuCanvas.SetActive(false);
        if (ball) ball.SetActive(true);
    }
}