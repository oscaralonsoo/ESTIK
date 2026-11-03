using UnityEngine;

public class ClampToCanvas : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private bool clampJustPivot = true;

    private RectTransform rect;
    private readonly Vector3[] corners = new Vector3[4];
    private readonly Vector3[] canvasCorners = new Vector3[4];


    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        canvasRect.GetWorldCorners(canvasCorners);
        if (clampJustPivot)
        {

            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, canvasCorners[0].x, canvasCorners[2].x);
            p.y = Mathf.Clamp(p.y, canvasCorners[0].y, canvasCorners[2].y);
            transform.position = p;
        }
        else
        {
            rect.GetWorldCorners(corners);

            Vector3 offset = Vector3.zero;

            if (corners[0].x < canvasCorners[0].x)
                offset.x = canvasCorners[0].x - corners[0].x;
            else if (corners[2].x > canvasCorners[2].x)
                offset.x = canvasCorners[2].x - corners[2].x;

            if (corners[0].y < canvasCorners[0].y)
                offset.y = canvasCorners[0].y - corners[0].y;
            else if (corners[2].y > canvasCorners[2].y)
                offset.y = canvasCorners[2].y - corners[2].y;

            transform.position += offset;
        }
    }
}