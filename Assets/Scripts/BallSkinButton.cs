
using UnityEngine;

public class BallSkinApplier : MonoBehaviour
{
    [SerializeField] SkinCatalog catalog;
    [SerializeField] Renderer ballRenderer;
    [SerializeField] SkinCarousel carousel;

    void OnEnable()
    {
        Apply();
        if (carousel != null) carousel.SelectionChanged += OnSelectionChanged;
    }

    void OnDisable()
    {
        if (carousel != null) carousel.SelectionChanged -= OnSelectionChanged;
    }

    void OnSelectionChanged(SkinData skin) => Apply();

    public void Apply()
    {
        if (catalog == null || ballRenderer == null)
        {
            Debug.LogError("BallSkinApplier: faltan referencias", this);
            return;
        }

        var skin = System.Array.Find(catalog.skins, s => s.id == SkinService.EquippedId);

        if (skin == null || !SkinService.IsUnlocked(skin))
            skin = catalog.skins[0];

        if (skin.ballMaterial != null)
            ballRenderer.sharedMaterial = skin.ballMaterial;
    }
}