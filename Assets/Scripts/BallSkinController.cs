using System;
using UnityEngine;

public class BallSkinController : MonoBehaviour
{
    private const string SkinKey = "SelectedBall";

    public static event Action<string> SkinChanged;

    public void SelectSkin(string skinName)
    {
        if (string.IsNullOrEmpty(skinName)) return;

        PlayerPrefs.SetString(SkinKey, skinName);
        PlayerPrefs.Save();

        SkinChanged?.Invoke(skinName);
    }

    public string GetSavedSkinName()
    {
        return PlayerPrefs.GetString(SkinKey, "");
    }

}