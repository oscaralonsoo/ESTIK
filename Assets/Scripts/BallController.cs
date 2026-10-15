using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [Header("SHOOT PROPERTIES")]
    [SerializeField] private float shootSpeed = 30f;
    [SerializeField] private float maxDragDistance = 0.2f;

    [Header("TRACE PROPERTIES")]
    [SerializeField] private GameObject traceCirclePrefab;
    [SerializeField] private int numberOfCircles = 15;
    [SerializeField] private float timeBetweenCircles = 0.1f;

    [Header("SLOWMO PROPERTIES")]
    [SerializeField] private bool slowmo = true;
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowmoJoinTransitionDuration = 0.5f;
    [SerializeField] private float slowmoLeaveTransitionDuration = 0f;

    [Header("DEBUG")]
    [SerializeField] private bool draging = false;
    [SerializeField] private float currentShootSpeed = 0f;
    [SerializeField] private Vector2 dragStartPosition = Vector2.zero;
    [SerializeField] private Vector2 dragEndPosition = Vector2.zero;
    [SerializeField] private Vector2 dragCurrentPosition = Vector2.zero;
    [SerializeField] private GameObject[] traceCircles;
    [SerializeField] private Rigidbody ballRigidbody = null;


    void Start()
    {
        if(this.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            ballRigidbody = rb;
        }

        traceCircles = new GameObject[numberOfCircles];

        for (int i = 0; i < numberOfCircles; i++)
        {
            traceCircles[i] = Instantiate(
                traceCirclePrefab,
                transform.position,
                Quaternion.identity
            );

            traceCircles[i].SetActive(false);
        }
    }

    void Update()
    {
        if (Mouse.current == null || ballRigidbody == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            draging = true;
        }

        if (draging)
        {
            Vector3 ballScreenPos3D = Camera.main.WorldToScreenPoint(ballRigidbody.position);
            Vector2 ballScreenPos = new Vector2(ballScreenPos3D.x, ballScreenPos3D.y);
            dragStartPosition = ballScreenPos / Screen.height;

            Vector2 rawPos = Mouse.current.position.ReadValue();
            dragCurrentPosition = rawPos / Screen.height;
            ShowTrace();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && draging)
        {
            Vector3 ballScreenPos3D = Camera.main.WorldToScreenPoint(ballRigidbody.position);
            Vector2 ballScreenPos = new Vector2(ballScreenPos3D.x, ballScreenPos3D.y);
            dragStartPosition = ballScreenPos / Screen.height;

            Vector2 rawPos = Mouse.current.position.ReadValue();
            dragEndPosition = rawPos / Screen.height;

            Shoot();
            OnShoot();
        }

        if (slowmo)
        {
            float targetTimeScale = draging ? slowMotionScale : 1.0f;
            float currentTransitionDuration = draging ? slowmoJoinTransitionDuration : slowmoLeaveTransitionDuration;
            float changeSpeed = currentTransitionDuration > 0f ? (1f / currentTransitionDuration) * Time.unscaledDeltaTime : 1000f;
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetTimeScale, changeSpeed);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector2 dragDirection)
    {
        Transform camTransform = Camera.main.transform;

        Vector3 camForward = camTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = camTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 worldDirection = (camForward * dragDirection.y) + (camRight * dragDirection.x);

        return worldDirection.normalized;
    }

    void Shoot()
    {
        if (!ballRigidbody) return;

        Vector2 dragDir = dragStartPosition - dragEndPosition;

        currentShootSpeed = dragDir.magnitude > maxDragDistance
            ? shootSpeed
            : dragDir.magnitude / maxDragDistance * shootSpeed;

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = shootDir * currentShootSpeed;
    }

    void ShowTrace()
    {
        Vector2 dragDir = dragStartPosition - dragCurrentPosition;

        currentShootSpeed = dragDir.magnitude > maxDragDistance
            ? shootSpeed
            : dragDir.magnitude / maxDragDistance * shootSpeed;

        Vector3 shootDir = GetCameraRelativeDirection(dragDir);

        Vector3 startPosition = ballRigidbody.transform.position;
        Vector3 initialVelocity = shootDir * currentShootSpeed;

        for (int i = 0; i < numberOfCircles; i++)
        {
            float t = i * timeBetweenCircles;

            Vector3 pointPosition = startPosition + (initialVelocity * t);

            traceCircles[i].transform.position = pointPosition;
            traceCircles[i].SetActive(true);
        }
    }

    void HideTrace()
    {
        for (int i = 0; i < traceCircles.Length; i++)
        {
            traceCircles[i].SetActive(false);
        }
    }

    void OnShoot()
    {
        HideTrace();

        draging = false;
        dragStartPosition = Vector2.zero;
        dragCurrentPosition = Vector2.zero;
        dragEndPosition = Vector2.zero;
        currentShootSpeed = 0f;
    }
}