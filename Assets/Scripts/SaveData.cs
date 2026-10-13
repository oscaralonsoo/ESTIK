using UnityEngine;

public static class SaveData
{
    public static int GetStars(int level) => PlayerPrefs.GetInt($"stars_{level}", 0);

    public static void SetStars(int level, int stars)
    {
        if (stars > GetStars(level))
        {
            PlayerPrefs.SetInt($"stars_{level}", stars);
            PlayerPrefs.Save();
        }
    }
    public static bool IsUnlocked(int level) => level == 1 || GetStars(level - 1) > 0;
}