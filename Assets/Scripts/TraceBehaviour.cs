using UnityEngine;
using UnityEngine.UI;

public class TraceBehaviour : MonoBehaviour
{
    [SerializeField] private Image fillImage;


    public void SetFillAmount(float amount)
    {
        fillImage.fillAmount = amount;
    }
}
