using UnityEngine;

public class BallSkinController : MonoBehaviour
{
    private const string SkinKey = "SelectedBallSkin";

    private Material selectedSkin;

    public void SelectSkin(Material skin)
    {
        if (skin == null) return;

        selectedSkin = skin;

        PlayerPrefs.SetString(SkinKey, skin.name);
        PlayerPrefs.Save();
    }

    public string GetSavedSkinName()
    {
        return PlayerPrefs.GetString(SkinKey, "");
    }
}