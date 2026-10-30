using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skin")]
public class SkinData : ScriptableObject
{
    public string id;
    public string displayName;
    public Material ballMaterial;
    public GameObject effectPrefab;
    public GameObject ballPrefab;
    public bool unlockedByDefault = true;
    public int price = 100;
}