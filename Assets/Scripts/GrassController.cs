using UnityEngine;
using System.Collections.Generic;

public class GrassController : MonoBehaviour
{
    Material grassMat;
    MeshRenderer meshRenderer;
    List<Transform> entities = new List<Transform>();
    bool emptyList = true;
    float pos = 0f;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        grassMat = meshRenderer.material;
    }

    private void FixedUpdate()
    {
        if (entities.Count <= 0)
        {
            if (!emptyList)
            {
                Vector3 currentGrassMatPos = grassMat.GetVector("_Pos");
                grassMat.SetVector("_Pos", currentGrassMatPos + new Vector3(0f, pos, 0f));
                if (pos >= 50f) emptyList = true;
                pos += 0.3f;
            }
            return;
        }

        if (entities[entities.Count - 1] == null) return;
        grassMat.SetVector("_Pos", entities[entities.Count - 1].position);
    }

    private void OnTriggerEnter(Collider other)
    {
        entities.Add(other.transform);
        emptyList = false;
        pos = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        entities.Remove(other.transform);
    }
}
