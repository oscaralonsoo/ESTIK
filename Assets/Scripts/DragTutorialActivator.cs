using UnityEngine;

public class DragTutorialActivator : MonoBehaviour
{
    [SerializeField] DragTutorial tutorial;

    private void OnTriggerEnter(Collider other)
    {
        tutorial.gameObject.SetActive(true);
    }
}
