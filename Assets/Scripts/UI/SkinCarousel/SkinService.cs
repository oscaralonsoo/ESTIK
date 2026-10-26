using UnityEngine;

public static class SkinService
{
    const string EquippedKey = "skin.equipped";
    const string UnlockedPrefix = "skin.unlocked.";

    public static string EquippedId => PlayerPrefs.GetString(EquippedKey, "");

    public static bool IsUnlocked(SkinData s) =>
        s.unlockedByDefault || PlayerPrefs.GetInt(UnlockedPrefix + s.id, 0) == 1;

    public static void Unlock(SkinData s)
    {
        PlayerPrefs.SetInt(UnlockedPrefix + s.id, 1);
        PlayerPrefs.Save();
    }

    public static void Equip(SkinData s)
    {
        if (!IsUnlocked(s)) return;
        PlayerPrefs.SetString(EquippedKey, s.id);
        PlayerPrefs.Save();
    }
}