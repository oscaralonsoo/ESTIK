using UnityEngine;

public class Tutorial2Activator : MonoBehaviour
{
    [SerializeField] Tutorial1 tutorial;

    private void OnTriggerEnter(Collider other)
    {
        tutorial.gameObject.SetActive(true);
    }
}
