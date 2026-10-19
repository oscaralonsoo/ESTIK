
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DribblingTrainingController : MonoBehaviour
{
    private class LimitData
    {
        public GameObject limit;
        public Collider trigger;
        public DecalProjector decal;
        public bool ballInside;
    }

    private class GateData
    {
        public GameObject gate;
        public Collider trigger;
        public DecalProjector decal;
        public bool ballInside;
        public List<LimitData> limits = new List<LimitData>();
    }

    private List<GateData> dribblingGates = new List<GateData>();
    private int currentGateIndex = 0;

    private GameObject ball;
    private Collider[] ballColliders;

    private void Start()
    {
        Debug.Log("[Dribbling] Iniciando entrenamiento...");

        ball = GameObject.FindGameObjectWithTag("Ball");

        if (ball == null)
        {
            Debug.LogError("[Dribbling] No se encontró ningún objeto con tag Ball.");
            return;
        }

        ballColliders = ball.GetComponentsInChildren<Collider>();

        foreach (Transform child in transform)
        {
            Collider gateTrigger = null;

            foreach (Collider col in child.GetComponentsInChildren<Collider>(true))
            {
                if (col.isTrigger && !col.transform.IsChildOf(child.Find("DribblingLimit")))
                {
                    gateTrigger = col;
                    break;
                }
            }

            DecalProjector decal = null;

            foreach (DecalProjector dp in child.GetComponentsInChildren<DecalProjector>(true))
            {
                if (!dp.transform.IsChildOf(child.Find("DribblingLimit")))
                {
                    decal = dp;
                    break;
                }
            }

            GateData data = new GateData
            {
                gate = child.gameObject,
                trigger = gateTrigger,
                decal = decal,
                ballInside = false
            };

            // Buscar los límites dentro de DribblingLimit
            Transform limitsParent = child.Find("DribblingLimit");

            if (limitsParent != null)
            {
                foreach (Transform limitTransform in limitsParent)
                {
                    Collider limitTrigger = null;

                    foreach (Collider col in limitTransform.GetComponentsInChildren<Collider>(true))
                    {
                        if (col.isTrigger)
                        {
                            limitTrigger = col;
                            break;
                        }
                    }

                    DecalProjector limitDecal =
                        limitTransform.GetComponentInChildren<DecalProjector>(true);

                    LimitData limitData = new LimitData
                    {
                        limit = limitTransform.gameObject,
                        trigger = limitTrigger,
                        decal = limitDecal,
                        ballInside = false
                    };

                    data.limits.Add(limitData);

                    if (limitDecal != null)
                        limitDecal.gameObject.SetActive(false);

                    Debug.Log("[Dribbling] Límite encontrado: " + limitTransform.name);
                }
            }
            else
            {
                Debug.LogWarning("[Dribbling] No se encontró DribblingLimit en " + child.name);
            }

            dribblingGates.Add(data);

            if (decal != null)
                decal.gameObject.SetActive(false);

            Debug.Log("[Dribbling] Gate encontrado: " + child.name);
        }

        UpdateCurrentDecal();
    }

    private void Update()
    {
        if (ball == null || currentGateIndex >= dribblingGates.Count)
            return;

        GateData currentGate = dribblingGates[currentGateIndex];

        // Detectar entrada y salida del gate principal
        bool isInsideGate = IsBallInside(currentGate.trigger);

        if (isInsideGate && !currentGate.ballInside)
        {
            currentGate.ballInside = true;
            Debug.Log("[Dribbling] OnTriggerEnter: " + currentGate.gate.name);
        }

        if (!isInsideGate && currentGate.ballInside)
        {
            currentGate.ballInside = false;

            Debug.Log("[Dribbling] OnTriggerExit: " + currentGate.gate.name);

            if (currentGate.decal != null)
                currentGate.decal.gameObject.SetActive(false);

            // Ocultar los dos límites del gate completado
            foreach (LimitData limit in currentGate.limits)
            {
                if (limit.decal != null)
                    limit.decal.gameObject.SetActive(false);

                limit.ballInside = false;
            }

            currentGateIndex++;

            UpdateCurrentDecal();
            return;
        }

        // Detectar entrada y salida de los límites laterales
        foreach (LimitData limit in currentGate.limits)
        {
            if (limit.trigger == null)
                continue;

            bool isInsideLimit = IsBallInside(limit.trigger);

            if (isInsideLimit && !limit.ballInside)
            {
                limit.ballInside = true;

                Debug.LogWarning(
                    "[Dribbling] OnTriggerEnter en límite: "
                    + limit.limit.name + " del gate "
                    + currentGate.gate.name);
            }

            if (!isInsideLimit && limit.ballInside)
            {
                limit.ballInside = false;

                FastSceneLoader.Instance.RestartLevel();
            }
        }
    }

    private bool IsBallInside(Collider target)
    {
        if (target == null || !target.enabled)
            return false;

        foreach (Collider ballCollider in ballColliders)
        {
            if (ballCollider == null || !ballCollider.enabled)
                continue;

            if (target.bounds.Intersects(ballCollider.bounds))
                return true;
        }

        return false;
    }

    private void UpdateCurrentDecal()
    {
        if (currentGateIndex >= dribblingGates.Count)
        {
            Debug.Log("[Dribbling] ¡Entrenamiento completado!");
            return;
        }

        GateData gate = dribblingGates[currentGateIndex];

        if (gate.decal != null)
        {
            gate.decal.gameObject.SetActive(true);
            Debug.Log("[Dribbling] Decal activado: " + gate.decal.name);
        }

        foreach (LimitData limit in gate.limits)
        {
            if (limit.decal != null)
            {
                limit.decal.gameObject.SetActive(true);
                Debug.Log("[Dribbling] Límite activado: " + limit.decal.name);
            }
        }
    }
}
