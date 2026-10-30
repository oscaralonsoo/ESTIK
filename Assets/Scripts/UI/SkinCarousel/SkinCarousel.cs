using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SkinCarousel : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] SkinCatalog catalog;
    [SerializeField] SkinCarouselItem itemPrefab;
    [SerializeField] RectTransform content;
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;

    [Header("Buy Refs")]
    [SerializeField] Button buyButton;
    [SerializeField] TextMeshProUGUI buyButtonText;
    [SerializeField] Color cantAffordTextColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Not Enough Points Popup")]
    [SerializeField] TextMeshProUGUI notEnoughText;
    [SerializeField] float popDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] float popStartScale = 0.85f;
    [Tooltip("0 = sin rebote, 1 = rebote marcado.")]
    [SerializeField, Range(0f, 1.5f)] float popOvershoot = 0.5f;
    [SerializeField] float popHoldTime = 1.2f;
    [SerializeField] float popFadeTime = 0.5f;
    [SerializeField] float popRiseDistance = 20f;
    [SerializeField] int popupSortingOrder = 1000;

    BallSkinController ballSkinController;
    PanelAnimator _panel;

    [Header("Layout")]
    [SerializeField] float spacing = 260f;
    [SerializeField] float centerScale = 1.6f;
    [SerializeField] float sideScale = 0.6f;
    [SerializeField, Range(0f, 1f)] float sideBrightness = 0.3f;
    [SerializeField] float fadeEnd = 1.4f;

    [Header("Feel")]
    [SerializeField] float smoothTime = 0.12f;
    [SerializeField] float inertia = 0.15f;
    [SerializeField] float overscroll = 0.35f;

    [Header("Selection")]
    [SerializeField] float centerTolerance = 0.01f;

    [Header("Purchase")]
    [SerializeField] float unlockAnimationLock = 0.6f;

    public event Action<SkinData> SelectionChanged;

    public SkinData Current => catalog.skins[_targetIndex];
    public bool IsUnlocking => _unlocking;

    SkinCarouselItem[] _items;
    Canvas _canvas;

    float _pos, _vel, _dragVel;

    int _targetIndex;
    int _lastNearest = -1;
    int _lastSavedIndex = -1;

    bool _dragging;
    bool _unlocking;
    Coroutine _unlockRoutine;

    CanvasGroup _popupGroup;
    RectTransform _popupRect;
    Vector2 _popupBasePos;
    Coroutine _popupRoutine;
    Color _buyTextDefaultColor = Color.white;

    int Count => catalog.skins.Length;

    void Awake()
    {
        ballSkinController = FindAnyObjectByType<BallSkinController>();
        _panel = GetComponentInParent<PanelAnimator>(true);

        if (catalog == null || itemPrefab == null || content == null)
        {
            Debug.LogError("SkinCarousel: faltan referencias en el Inspector", this);
            enabled = false;
            return;
        }

        if (Count == 0)
        {
            Debug.LogError("SkinCarousel: el catalogo esta vacio", this);
            enabled = false;
            return;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("SkinCarousel: no se encuentra un Canvas padre", this);
            enabled = false;
            return;
        }

        _canvas = parentCanvas.rootCanvas;

        if (prevButton != null)
            prevButton.onClick.AddListener(() => SelectIndex(_targetIndex - 1));

        if (nextButton != null)
            nextButton.onClick.AddListener(() => SelectIndex(_targetIndex + 1));

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyCurrentSkin);

        if (buyButtonText != null)
            _buyTextDefaultColor = buyButtonText.color;

        if (notEnoughText != null)
        {
            _popupRect = notEnoughText.rectTransform;
            _popupBasePos = _popupRect.anchoredPosition;

            _popupGroup = notEnoughText.GetComponent<CanvasGroup>();
            if (_popupGroup == null)
                _popupGroup = notEnoughText.gameObject.AddComponent<CanvasGroup>();

            _popupGroup.interactable = false;
            _popupGroup.blocksRaycasts = false;

            Canvas popupCanvas = notEnoughText.GetComponent<Canvas>();
            if (popupCanvas == null)
                popupCanvas = notEnoughText.gameObject.AddComponent<Canvas>();

            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = popupSortingOrder;

            HidePopupImmediate();
        }

        _items = new SkinCarouselItem[Count];

        for (int i = 0; i < Count; i++)
        {
            SkinCarouselItem item = Instantiate(itemPrefab, content);
            item.Init(catalog.skins[i], i);
            _items[i] = item;
        }
    }

    void OnEnable()
    {
        if (_items == null || Count == 0)
            return;

        int start = -1;

        if (ballSkinController != null)
        {
            string savedName = ballSkinController.GetSavedSkinName();
            if (!string.IsNullOrEmpty(savedName))
                start = Array.FindIndex(catalog.skins, s => s != null && s.displayName == savedName);
        }

        if (start < 0)
            start = Array.FindIndex(catalog.skins, s => s != null && s.id == SkinService.EquippedId);

        _targetIndex = Mathf.Max(0, start);

        _pos = _targetIndex;
        _vel = 0f;
        _dragVel = 0f;
        _dragging = false;
        _unlocking = false;
        _lastNearest = -1;
        _lastSavedIndex = -1;

        HidePopupImmediate();

        Layout();
        RefreshUI();

        SkinData currentSkin = Current;
        if (currentSkin != null && SkinService.IsUnlocked(currentSkin))
        {
            SaveSkinForIndex(_targetIndex);
        }
    }

    void OnDisable()
    {
        if (_items == null || Count == 0)
            return;

        _dragging = false;

        if (_unlockRoutine != null)
        {
            StopCoroutine(_unlockRoutine);
            _unlockRoutine = null;
        }

        _unlocking = false;

        HidePopupImmediate();

        CommitSelection();
    }

    void Start()
    {
        if (_items == null || Count == 0)
            return;

        SelectionChanged?.Invoke(Current);
    }

    void Update()
    {
        if (!_dragging)
        {
            _pos = Mathf.SmoothDamp(_pos, _targetIndex, ref _vel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        Layout();
        DetectLargeSelectedBall();

        if (_dragging && !_unlocking)
        {
            int nearest = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, Count - 1);
            if (nearest != _targetIndex)
                SelectIndex(nearest);
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (_unlocking)
            return;

        _dragging = true;
        _dragVel = 0f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (_unlocking || !_dragging)
            return;

        if (spacing <= 0f)
            return;

        float deltaIdx = -e.delta.x / _canvas.scaleFactor / spacing;

        _pos = Mathf.Clamp(_pos + deltaIdx, -overscroll, Count - 1 + overscroll);

        float instant = deltaIdx / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _dragVel = Mathf.Lerp(_dragVel, instant, 0.5f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_dragging)
            return;

        _dragging = false;
        _vel = 0f;

        if (_unlocking)
            return;

        SelectIndex(Mathf.RoundToInt(_pos + _dragVel * inertia));
    }

    public void SelectIndex(int index)
    {
        if (Count == 0)
            return;

        if (_unlocking)
            return;

        index = Mathf.Clamp(index, 0, Count - 1);

        if (index != _targetIndex)
        {
            _targetIndex = index;

            HidePopupImmediate();

            RefreshUI();

            SkinData selectedSkin = Current;

            if (selectedSkin != null && SkinService.IsUnlocked(selectedSkin))
            {
                SkinService.Equip(selectedSkin);
                SelectionChanged?.Invoke(selectedSkin);
            }
        }

        DetectLargeSelectedBall();
    }

    void CommitSelection()
    {
        _targetIndex = Mathf.Clamp(_targetIndex, 0, Count - 1);

        SkinData skin = catalog.skins[_targetIndex];
        if (skin == null)
            return;

        if (SkinService.IsUnlocked(skin))
        {
            SkinService.Equip(skin);
            SaveSkinForIndex(_targetIndex);
        }
    }

    void DetectLargeSelectedBall()
    {
        if (_items == null || _items.Length == 0)
            return;

        int centerIndex = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, Count - 1);
        float distance = Mathf.Abs(_pos - centerIndex);

        if (distance > centerTolerance)
            return;

        float actualScale = Mathf.Lerp(centerScale, sideScale, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance)));

        if (actualScale < centerScale - centerTolerance)
            return;

        SkinData skin = catalog.skins[centerIndex];
        if (skin != null && SkinService.IsUnlocked(skin))
        {
            SaveSkinForIndex(centerIndex);
        }
    }

    void SaveSkinForIndex(int index)
    {
        if (index < 0 || index >= Count)
            return;

        if (ballSkinController == null)
        {
            Debug.LogWarning("SkinCarousel: BallSkinController no esta asignado en el Inspector.", this);
            return;
        }

        if (_lastSavedIndex == index)
            return;

        SkinData skin = catalog.skins[index];
        if (skin == null)
            return;

        ballSkinController.SelectSkin(skin.displayName);
        _lastSavedIndex = index;
    }

    void RefreshUI()
    {
        if (prevButton != null)
            prevButton.interactable = !_unlocking && _targetIndex > 0;

        if (nextButton != null)
            nextButton.interactable = !_unlocking && _targetIndex < Count - 1;

        for (int i = 0; i < _items.Length; i++)
            _items[i].SetSelected(i == _targetIndex);

        SkinData currentSkin = Current;
        if (currentSkin != null && buyButton != null)
        {
            bool isUnlocked = SkinService.IsUnlocked(currentSkin);

            if (isUnlocked)
            {
                buyButton.gameObject.SetActive(false);
            }
            else
            {
                buyButton.gameObject.SetActive(true);

                if (buyButtonText != null)
                {
                    buyButtonText.text = currentSkin.price.ToString();
                }

                int playerPoints = PointsManager.Instance != null ? PointsManager.Instance.GetCurrentScore() : 0;
                bool canAfford = playerPoints >= currentSkin.price;

                buyButton.interactable = !_unlocking;

                if (buyButtonText != null)
                    buyButtonText.color = canAfford ? _buyTextDefaultColor : cantAffordTextColor;
            }
        }
    }

    public void PulseCurrentItem()
    {
        if (_items != null && _targetIndex >= 0 && _targetIndex < _items.Length)
            _items[_targetIndex].PlayReceivePulse();
    }

    public void BuyCurrentSkin()
    {
        if (buyButton == null || _unlocking)
            return;

        SkinData currentSkin = Current;
        if (currentSkin == null || SkinService.IsUnlocked(currentSkin))
            return;

        int price = currentSkin.price;
        int playerPoints = PointsManager.Instance != null ? PointsManager.Instance.GetCurrentScore() : 0;

        if (playerPoints < price)
        {
            ShowNotEnoughPoints();
            return;
        }

        _unlocking = true;
        _dragging = false;
        _dragVel = 0f;
        _vel = 0f;

        HidePopupImmediate();

        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.SpendPoints(price);
            PointsManager.Instance.AnimateScoreSpend(price);
        }

        int buyIndex = _targetIndex;
        SkinCarouselItem buyItem = _items[buyIndex];
        Vector3 targetBallPos = buyItem.GetBallPosition();

        RefreshUI();

        if (PurchaseCollector.Instance != null)
        {
            PurchaseCollector.Instance.SpawnPurchaseEffect(
                targetBallPos,
                price,
                () => CompletePurchase(currentSkin, buyIndex),
                () => buyItem.PlayReceivePulse());
        }
        else
        {
            CompletePurchase(currentSkin, buyIndex);
        }
    }

    void CompletePurchase(SkinData skin, int index)
    {
        SkinService.Unlock(skin);
        SkinService.Equip(skin);
        SaveSkinForIndex(index);
        SelectionChanged?.Invoke(skin);

        _items[index].PlayUnlockAnimation();

        Debug.Log("¡Skin comprada y equipada: " + skin.displayName + "!");

        if (_unlockRoutine != null)
            StopCoroutine(_unlockRoutine);

        if (unlockAnimationLock > 0f && isActiveAndEnabled)
        {
            _unlockRoutine = StartCoroutine(ReleaseUnlockLock());
        }
        else
        {
            _unlocking = false;
            RefreshUI();
        }
    }

    IEnumerator ReleaseUnlockLock()
    {
        yield return new WaitForSecondsRealtime(unlockAnimationLock);

        _unlocking = false;
        _unlockRoutine = null;
        RefreshUI();
    }

    void ShowNotEnoughPoints()
    {
        if (notEnoughText == null)
        {
            Debug.Log("No tienes suficientes puntos para comprar esta skin.");
            return;
        }

        if (!isActiveAndEnabled)
            return;

        if (_popupRoutine != null)
            StopCoroutine(_popupRoutine);

        _popupRoutine = StartCoroutine(NotEnoughPointsRoutine());
    }

    IEnumerator NotEnoughPointsRoutine()
    {
        notEnoughText.gameObject.SetActive(true);

        _popupRect.SetAsLastSibling();

        _popupRect.anchoredPosition = _popupBasePos;
        _popupGroup.alpha = 0f;
        _popupRect.localScale = Vector3.one * popStartScale;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(popDuration, 0.0001f));
            float s = Mathf.LerpUnclamped(popStartScale, 1f, EaseOutBack(k, popOvershoot));
            _popupRect.localScale = Vector3.one * s;
            _popupGroup.alpha = Mathf.SmoothStep(0f, 1f, k);
            yield return null;
        }

        _popupRect.localScale = Vector3.one;
        _popupGroup.alpha = 1f;

        float hold = 0f;
        while (hold < popHoldTime)
        {
            hold += Time.unscaledDeltaTime;
            yield return null;
        }

        float f = 0f;
        while (f < popFadeTime)
        {
            f += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(f / Mathf.Max(popFadeTime, 0.0001f));
            float e = Mathf.SmoothStep(0f, 1f, k);
            _popupGroup.alpha = 1f - e;
            _popupRect.anchoredPosition = _popupBasePos + Vector2.up * (popRiseDistance * e);
            yield return null;
        }

        HidePopupImmediate();
    }

    static float EaseOutBack(float x, float overshoot)
    {
        float c1 = overshoot;
        float c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    void HidePopupImmediate()
    {
        if (_popupRoutine != null)
        {
            StopCoroutine(_popupRoutine);
            _popupRoutine = null;
        }

        if (notEnoughText == null)
            return;

        if (_popupGroup != null)
            _popupGroup.alpha = 0f;

        if (_popupRect != null)
        {
            _popupRect.anchoredPosition = _popupBasePos;
            _popupRect.localScale = Vector3.one;
        }

        notEnoughText.gameObject.SetActive(false);
    }

    void Layout()
    {
        if (_items == null)
            return;

        float panelVis = _panel != null ? _panel.Visibility : 1f;

        for (int i = 0; i < _items.Length; i++)
        {
            float d = i - _pos;
            float abs = Mathf.Abs(d);

            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(abs));
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, fadeEnd, abs));

            float scale = Mathf.Lerp(centerScale, sideScale, t) * fade * panelVis;
            float brightness = Mathf.Lerp(1f, sideBrightness, t);

            bool visible = abs < fadeEnd && scale > 0.01f;

            var rt = _items[i].Rect;
            rt.anchoredPosition = new Vector2(d * spacing, 0f);
            rt.localScale = Vector3.one;

            _items[i].SetScale(scale);
            _items[i].SetVisual(brightness, visible);
        }

        int nearest = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, Count - 1);

        if (nearest != _lastNearest)
        {
            _lastNearest = nearest;

            foreach (var item in _items.OrderByDescending(x => Mathf.Abs(x.Index - nearest)))
            {
                item.Rect.SetAsLastSibling();
            }
        }
    }
}