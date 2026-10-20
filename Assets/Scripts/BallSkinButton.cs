using UnityEngine;

public class BallSkinButton : MonoBehaviour
{
    public void SelectSkin()
    {
        BallSkinController skinController =
            FindAnyObjectByType<BallSkinController>();

        if (skinController == null) return;

        MeshRenderer meshRenderer =
            GetComponentInChildren<MeshRenderer>(true);

        if (meshRenderer == null) return;
        

        skinController.SelectSkin(meshRenderer.sharedMaterial);
    }
}