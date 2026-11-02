using System.Collections.Generic;
using UnityEngine;

public class PlayerGrabber : MonoBehaviour
{
    private Rigidbody rbPlatform;
    private Vector3 lastPosition;
    private Rigidbody passenger = null;

    private void Start()
    {
        rbPlatform = GetComponent<Rigidbody>();
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Vector3 platformMovement = transform.position - lastPosition;

        if (platformMovement != Vector3.zero)
        {
            if (passenger != null)
            {
                passenger.MovePosition(passenger.position + platformMovement);
            }
        }

        lastPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb != null && (collision.gameObject.CompareTag("Ball") || collision.gameObject.GetComponent<BallController>() != null))
        {
            passenger = rb;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb != null && (collision.gameObject.CompareTag("Ball") || collision.gameObject.GetComponent<BallController>() != null))
        {
            passenger = null;
        }
    }
}