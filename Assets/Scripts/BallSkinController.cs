
using UnityEngine;

public class BallSkinController : MonoBehaviour
{
    private const string SkinKey = "SelectedBall";

    public void SelectSkin(string skinName)
    {
        if (string.IsNullOrEmpty(skinName)) return;

        PlayerPrefs.SetString(SkinKey, skinName);
        PlayerPrefs.Save();
    }

    public string GetSavedSkinName()
    {
        return PlayerPrefs.GetString(SkinKey, "");
    }

}
