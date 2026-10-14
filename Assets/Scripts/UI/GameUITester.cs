using UnityEngine;
using UnityEngine.InputSystem;

public class GameUITester : MonoBehaviour
{
    [SerializeField] GameUI ui;

    void Start()
    {
        ui.SetScore(0);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) ui.ShowWin(12000, 1);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) ui.ShowWin(25000, 2);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ui.ShowWin(40000, 3);
        if (Keyboard.current.lKey.wasPressedThisFrame) ui.ShowLose();
    }
}