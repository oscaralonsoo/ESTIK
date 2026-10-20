using UnityEngine;

public class BallSkinButton : MonoBehaviour
{
    public void SelectSkin()
    {
        BallSkinController skinController =
            FindAnyObjectByType<BallSkinController>();

        if (skinController == null) return;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.CompareTag("Ball"))
            {
                skinController.SelectSkin(child.name);
                return;
            }
        }
    }
}