using UnityEngine;
using UnityEngine.UI;

public class NotifyCustomBallController : MonoBehaviour
{
    [Header("Notificación")]
    [Tooltip("El GameObject hijo que se activa/desactiva.")]
    [SerializeField] private GameObject notificationObject;

    [Header("Catálogo")]
    [SerializeField] private SkinCatalog skinCatalog;

    // Clave donde tu tienda guarda que una skin está comprada: UNLOCK_PREFIX + skin.id
    // ADÁPTALA a la que use tu sistema real de compra.
    private const string UNLOCK_PREFIX = "SkinUnlocked_";
    private const string SEEN_PREFIX = "CustomBallNotifSeen_";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        if (notificationObject != null)
        {
            notificationObject.SetActive(HasNewAffordableSkin());
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    private bool IsUnlocked(SkinData skin)
    {
        return skin.unlockedByDefault || PlayerPrefs.GetInt(UNLOCK_PREFIX + skin.id, 0) == 1;
    }

    private bool IsSeen(SkinData skin)
    {
        return PlayerPrefs.GetInt(SEEN_PREFIX + skin.id, 0) == 1;
    }

    private bool IsAffordableAndLocked(SkinData skin, int score)
    {
        return skin != null && !IsUnlocked(skin) && score >= skin.price;
    }

    private bool HasNewAffordableSkin()
    {
        if (skinCatalog == null || skinCatalog.skins == null || PointsManager.Instance == null)
            return false;

        int score = PointsManager.Instance.GetCurrentScore();

        foreach (SkinData skin in skinCatalog.skins)
        {
            if (IsAffordableAndLocked(skin, score) && !IsSeen(skin))
                return true;
        }

        return false;
    }

    private void OnButtonClicked()
    {
        if (skinCatalog != null && skinCatalog.skins != null && PointsManager.Instance != null)
        {
            int score = PointsManager.Instance.GetCurrentScore();

            // Marca como vistas todas las que se podían comprar ahora mismo
            foreach (SkinData skin in skinCatalog.skins)
            {
                if (IsAffordableAndLocked(skin, score))
                {
                    PlayerPrefs.SetInt(SEEN_PREFIX + skin.id, 1);
                }
            }

            PlayerPrefs.Save();
        }

        if (notificationObject != null)
        {
            notificationObject.SetActive(false);
        }
    }
}