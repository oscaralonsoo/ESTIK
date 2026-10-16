using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShootController : MonoBehaviour
{
    public List<GameObject> throwableObjects;

    [Header("SHOOT PROPERTIES")]
    [SerializeField] private float shootSpeed = 100f;
    [SerializeField] private float maxDragDistance = 10f;

    [Header("TRACE PROPERTIES")]
    [SerializeField] private GameObject traceCirclePrefab;
    [SerializeField] private int numberOfCircles = 15;
    [SerializeField] private float timeBetweenCircles = 0.1f;

    [Header("DEBUG")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool canShoot = true;
    [SerializeField] private bool draging = false;
    [SerializeField] private float currentShootSpeed = 0f;
    [SerializeField] private int thowableObjectsCounter = 0;
    [SerializeField] private Vector2 dragStartPosition = Vector2.zero;
    [SerializeField] private Vector2 dragEndPosition = Vector2.zero;
    [SerializeField] private Vector2 dragCurrentPosition = Vector2.zero;
    [SerializeField] private GameObject[] traceCircles;
    [SerializeField]
    private List<Rigidbody> throwableRigidbodys =
        new List<Rigidbody>();

    private List<Vector3> initialPositions = new List<Vector3>();
    private List<Quaternion> initialRotations = new List<Quaternion>();

    private bool hasShot = false;

    private DesaturationSlowmo postProcessSlowmo;
    private void Awake()
    {
        postProcessSlowmo = FindAnyObjectByType<DesaturationSlowmo>();
    }

    void Start()
    {
        for (int i = 0; i < throwableObjects.Count; i++)
        {
            if (throwableObjects[i].TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                throwableRigidbodys.Add(rb);

                initialPositions.Add(rb.position);
                initialRotations.Add(rb.rotation);
            }
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
        if (Mouse.current == null)
            return;

        if (debugMode && hasShot &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            ResetShot();
            return;
        }

        if (canShoot &&
            thowableObjectsCounter < throwableRigidbodys.Count)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                dragStartPosition = Mouse.current.position.ReadValue();
                dragCurrentPosition = dragStartPosition;
                draging = true;

                postProcessSlowmo.ActivatePostProcess();
            }

            if (draging)
            {
                dragCurrentPosition = Mouse.current.position.ReadValue();
                ShowTrace();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame && draging)
            {
                dragEndPosition = Mouse.current.position.ReadValue();

                Shoot();
                OnShoot();
            }
        }
    }

    void Shoot()
    {
        if (thowableObjectsCounter >= throwableRigidbodys.Count)
            return;

        Vector2 shootDir = dragStartPosition - dragEndPosition;

        currentShootSpeed = shootDir.magnitude > maxDragDistance
            ? shootSpeed
            : shootDir.magnitude / maxDragDistance * shootSpeed;

        shootDir.Normalize();

        Rigidbody rb = throwableRigidbodys[thowableObjectsCounter];

        rb.WakeUp();
        rb.linearVelocity = shootDir * currentShootSpeed;
    }

    void ShowTrace()
    {
        if (thowableObjectsCounter >= throwableRigidbodys.Count)
            return;

        Vector2 shootDir = dragStartPosition - dragCurrentPosition;

        currentShootSpeed = shootDir.magnitude > maxDragDistance
            ? shootSpeed
            : shootDir.magnitude / maxDragDistance * shootSpeed;

        shootDir.Normalize();

        Vector3 initialVelocity = (Vector3)shootDir * currentShootSpeed;

        Vector3 startPosition =
            throwableRigidbodys[thowableObjectsCounter].position;

        for (int i = 0; i < numberOfCircles; i++)
        {
            float t = i * timeBetweenCircles;

            Vector3 pointPosition =
                startPosition
                + initialVelocity * t
                + 0.5f * Physics.gravity * t * t;

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

        thowableObjectsCounter++;

        draging = false;
        dragStartPosition = Vector2.zero;
        dragCurrentPosition = Vector2.zero;
        dragEndPosition = Vector2.zero;
        currentShootSpeed = 0f;

        canShoot = false;
        hasShot = true;
    }

    void ResetShot()
    {
        int index = thowableObjectsCounter - 1;

        if (index < 0 || index >= throwableRigidbodys.Count)
            return;

        Rigidbody rb = throwableRigidbodys[index];

        // Detener la física del objeto.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Restaurar la posición y rotación iniciales.
        rb.position = initialPositions[index];
        rb.rotation = initialRotations[index];

        // Sincronizar el Transform con la física.
        rb.transform.position = initialPositions[index];
        rb.transform.rotation = initialRotations[index];

        rb.Sleep();

        // Permitir lanzar otra vez el mismo objeto.
        thowableObjectsCounter = index;
        canShoot = true;
        hasShot = false;

        draging = false;
        dragStartPosition = Vector2.zero;
        dragCurrentPosition = Vector2.zero;
        dragEndPosition = Vector2.zero;
        currentShootSpeed = 0f;

        HideTrace();
    }
}