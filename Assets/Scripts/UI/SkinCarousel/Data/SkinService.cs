using System.Collections.Generic;
using UnityEngine;

public static class SkinService
{
    const string EquippedKey = "skin.equipped";
    const string UnlockedPrefix = "skin.unlocked.";

    static readonly Dictionary<string, bool> unlockedCache = new Dictionary<string, bool>();
    static string equippedId;
    static bool equippedLoaded;
    static bool dirty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        unlockedCache.Clear();
        equippedId = null;
        equippedLoaded = false;
        dirty = false;
    }
    public static void ClearCache()
    {
        unlockedCache.Clear();
        equippedLoaded = false;
    }

    public static string EquippedId
    {
        get
        {
            if (!equippedLoaded)
            {
                equippedId = PlayerPrefs.GetString(EquippedKey, "");
                equippedLoaded = true;
            }
            return equippedId;
        }
    }

    public static bool IsUnlocked(SkinData s)
    {
        if (s.unlockedByDefault) return true;

        if (unlockedCache.TryGetValue(s.id, out bool unlocked))
            return unlocked;

        unlocked = PlayerPrefs.GetInt(UnlockedPrefix + s.id, 0) == 1;
        unlockedCache[s.id] = unlocked;
        return unlocked;
    }

    public static void Unlock(SkinData s)
    {
        unlockedCache[s.id] = true;
        PlayerPrefs.SetInt(UnlockedPrefix + s.id, 1);
        dirty = true;
        Flush();
    }

    public static void Equip(SkinData s)
    {
        if (!IsUnlocked(s)) return;
        if (EquippedId == s.id) return;

        equippedId = s.id;
        PlayerPrefs.SetString(EquippedKey, s.id);
        dirty = true;
    }

    public static void Flush()
    {
        if (!dirty) return;
        PlayerPrefs.Save();
        dirty = false;
    }
}