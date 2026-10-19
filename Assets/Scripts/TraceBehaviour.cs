using UnityEngine;

public class TraceBehaviour : MonoBehaviour
{
    [SerializeField] private Transform fillMeshTransform;
    [SerializeField] private MeshRenderer borderMesh;
    [SerializeField] private Material filledMaterial;
    [SerializeField] private Material normalMaterial;

    int currentColor = 0;

    private Vector3 maxScale;

    private void Awake()
    {
        if (fillMeshTransform != null)
        {
            maxScale = fillMeshTransform.localScale;
        }
    }

    public void SetFillAmount(float amount)
    {
        if (fillMeshTransform == null) return;

        amount = Mathf.Clamp01(amount);

        Vector3 newScale = maxScale;
        newScale.z = maxScale.z * amount;

        fillMeshTransform.localScale = newScale;
    }

    public void SetColor(int color)
    {
        if (color == currentColor) return;

        if (color >= 1) borderMesh.material = filledMaterial;
        else borderMesh.material = normalMaterial;
        currentColor = color;
    }
}