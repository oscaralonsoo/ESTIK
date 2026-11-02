using UnityEngine;
using System.Collections.Generic;

public class ObstacleFader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float targetAlpha = 0.5f;


    private Dictionary<Renderer, float> rendererAlphas = new Dictionary<Renderer, float>();

    private void Start()
    {
        if (target == null)
        {
            BallController ball = FindAnyObjectByType<BallController>();
            if (ball != null) target = ball.transform;
        }
    }

    private void Update()
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, distance, obstacleLayer);

        HashSet<Renderer> frameHitRenderers = new HashSet<Renderer>();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                frameHitRenderers.Add(rend);

                if (!rendererAlphas.ContainsKey(rend))
                {
                    rendererAlphas[rend] = 1f;
                }

                rendererAlphas[rend] = Mathf.MoveTowards(rendererAlphas[rend], targetAlpha, fadeSpeed * Time.deltaTime);
                ApplyAlphaToMaterials(rend, rendererAlphas[rend]);
            }
        }

        List<Renderer> keys = new List<Renderer>(rendererAlphas.Keys);
        foreach (Renderer rend in keys)
        {
            if (rend == null) continue;

            if (!frameHitRenderers.Contains(rend))
            {
                rendererAlphas[rend] = Mathf.MoveTowards(rendererAlphas[rend], 1f, fadeSpeed * Time.deltaTime);
                ApplyAlphaToMaterials(rend, rendererAlphas[rend]);

                if (rendererAlphas[rend] >= 1f)
                {
                    rendererAlphas.Remove(rend);
                }
            }
        }
    }

    private void ApplyAlphaToMaterials(Renderer rend, float alphaValue)
    {
        foreach (Material mat in rend.materials)
        {
            if (mat.HasProperty("_Alpha"))
            {
                mat.SetFloat("_Alpha", alphaValue);
            }
        }
    }
}