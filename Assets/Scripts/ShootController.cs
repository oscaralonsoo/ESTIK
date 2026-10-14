using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShootController : MonoBehaviour
{
    public List<GameObject> throwableObjects;

    [Header("SHOOT PROPERTIES")]
    [SerializeField] private float shootSpeed = 100;
    [SerializeField] private float maxDragDistance = 10;

    [Header("TRACE PROPERTIES")]
    [SerializeField] private GameObject traceCirclePrefab;
    [SerializeField] private int numberOfCircles = 15;
    [SerializeField] private float timeBetweenCircles = 0.1f;

    [Header("DEBUG")]
    [SerializeField] private bool canShoot = true;
    [SerializeField] private bool draging = false;
    [SerializeField] private float currentShootSpeed = 0;
    [SerializeField] private int thowableObjectsCounter = 0;
    [SerializeField] private Vector2 dragStartPosition = Vector2.zero;
    [SerializeField] private Vector2 dragEndPosition = Vector2.zero;
    [SerializeField] private Vector2 dragCurrentPosition = Vector2.zero;

    [SerializeField] private GameObject[] traceCircles;
    [SerializeField] private List<Rigidbody> throwableRigidbodys;

    void Start()
    {
        for (int i = 0; i < throwableObjects.Count; i++)
        {
            if (throwableObjects[i].TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                throwableRigidbodys.Add(rb);
            }
        }

        traceCircles = new GameObject[numberOfCircles];
        for (int i = 0; i < numberOfCircles; i++)
        {
            traceCircles[i] = Instantiate(traceCirclePrefab, transform.position, Quaternion.identity);
            traceCircles[i].SetActive(false);
        }
    }

    void Update()
    {
        if (canShoot && thowableObjectsCounter < throwableRigidbodys.Count)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                dragStartPosition = Mouse.current.position.ReadValue();
                draging = true;
            }

            if (draging)
            {
                dragCurrentPosition = Mouse.current.position.ReadValue();
                ShowTrace();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                dragEndPosition = dragCurrentPosition;
                Shoot();
                OnShoot();
            }
        }
    }

    void Shoot()
    {
        if(thowableObjectsCounter >= throwableRigidbodys.Count) return;

        Vector2 shootDir = dragStartPosition - dragEndPosition;
        currentShootSpeed = shootDir.magnitude > maxDragDistance ? shootSpeed : shootDir.magnitude / maxDragDistance * shootSpeed;
        shootDir.Normalize();

        throwableRigidbodys[thowableObjectsCounter].linearVelocity = shootDir * currentShootSpeed;
    }

    void ShowTrace()
    {
        if(thowableObjectsCounter >= throwableRigidbodys.Count) return;

        Vector2 shootDir = dragStartPosition - dragCurrentPosition;
        currentShootSpeed = shootDir.magnitude > maxDragDistance ? shootSpeed : shootDir.magnitude / maxDragDistance * shootSpeed;
        shootDir.Normalize();
        Vector3 initialVelocity = (Vector3)shootDir * currentShootSpeed;

        Vector3 startPosition = throwableRigidbodys[thowableObjectsCounter].position;

        for (int i = 0; i < numberOfCircles; i++)
        {
            float t = i * timeBetweenCircles;

            Vector3 pointPosition = startPosition + (initialVelocity * t) + (0.5f * Physics.gravity * (t * t));

            traceCircles[i].transform.position = pointPosition;
            traceCircles[i].SetActive(true);
        }
    }

    void HideTrace()
    {
        for (int i = 0; i < numberOfCircles; i++)
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
        currentShootSpeed = 0;
        canShoot = false;
    }
}
