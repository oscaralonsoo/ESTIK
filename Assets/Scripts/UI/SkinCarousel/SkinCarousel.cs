using System;
using System.Collections;
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

    [Header("Confirm / Back")]
    [SerializeField] Button confirmButton;

    [Header("Buy Refs")]
    [SerializeField] Button buyButton;
    [SerializeField] TextMeshProUGUI buyButtonText;
    [SerializeField] string buyLabel = "BUY";
    [SerializeField] Color cantAffordTextColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Buy Button Animation")]
    [SerializeField] float buyAppearDelay = 0f;
    [SerializeField] Vector2 buyAppearOffset = new Vector2(0f, -40f);
    [SerializeField] bool buyInLayoutGroup = false;

    [Header("Price Label (encima de la bola)")]
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] float priceOffsetY = 170f;

    [Header("Not Enough Points Popup")]
    [SerializeField] TextMeshProUGUI notEnoughText;
    [SerializeField] float popDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] float popStartScale = 0.85f;
    [SerializeField, Range(0f, 1.5f)] float popOvershoot = 0.5f;
    [SerializeField] float popHoldTime = 1.2f;
    [SerializeField] float popFadeTime = 0.5f;
    [SerializeField] float popRiseDistance = 20f;

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

    const float MaxFrameTime = 1f / 30f;
    const float SettleEps = 0.0005f;

    static float FrameTime => Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);

    SkinCarouselItem[] _items;
    bool[] _itemVisible;
    Canvas _canvas;

    float _pos, _vel, _dragVel;

    int _targetIndex;
    int _lastNearest = -1;
    int _lastSavedIndex = -1;

    bool _dragging;
    bool _unlocking;
    Coroutine _unlockRoutine;

    bool _layoutDirty = true;
    float _lastPanelVis = -1f;
    int _lastPoints = int.MinValue;
    bool _warnedNoBall;

    CanvasGroup _popupGroup;
    RectTransform _popupRect;
    Vector2 _popupBasePos;
    Coroutine _popupRoutine;
    Color _buyTextDefaultColor = Color.white;
    Color _priceDefaultColor = Color.white;
    int _priceShown = -1;

    ButtonFeedback _buyFeedback;
    bool _buyVisible;

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
        {
            buyButton.onClick.AddListener(BuyCurrentSkin);
            _buyFeedback = buyButton.GetComponent<ButtonFeedback>();
        }

        if (buyButtonText != null)
            _buyTextDefaultColor = buyButtonText.color;

        if (priceText != null)
        {
            _priceDefaultColor = priceText.color;
            priceText.color = _priceDefaultColor;
            priceText.raycastTarget = false;
            priceText.gameObject.SetActive(false);
        }

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

            SortingLayer[] layers = SortingLayer.layers;
            if (layers != null && layers.Length > 0)
                popupCanvas.sortingLayerID = layers[layers.Length - 1].id;

            popupCanvas.sortingOrder = short.MaxValue;

            HidePopupImmediate();
        }

        _items = new SkinCarouselItem[Count];
        _itemVisible = new bool[Count];

        for (int i = 0; i < Count; i++)
        {
            SkinCarouselItem item = Instantiate(itemPrefab, content);
            item.Init(catalog.skins[i], i);
            item.Rect.localScale = Vector3.one;
            _items[i] = item;
        }

        if (EventSystem.current != null)
            EventSystem.current.pixelDragThreshold = Mathf.Max(10, Mathf.RoundToInt(Screen.dpi / 12f));
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
        _buyVisible = false;
        if (buyButton != null)
            buyButton.gameObject.SetActive(false);

        for (int i = 0; i < _itemVisible.Length; i++)
            _itemVisible[i] = true;
        _layoutDirty = true;
        _lastPanelVis = -1f;
        _lastPoints = int.MinValue;

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
        _buyVisible = false;

        if (confirmButton != null)
            confirmButton.interactable = true;

        HidePopupImmediate();

        CommitSelection();
        SkinService.Flush();
    }

    void Start()
    {
        if (_items == null || Count == 0)
            return;

        SelectionChanged?.Invoke(Current);
    }

    void Update()
    {
        float dt = FrameTime;

        int pts = PointsManager.Instance != null ? PointsManager.Instance.GetCurrentScore() : 0;
        if (pts != _lastPoints)
        {
            _lastPoints = pts;
            RefreshUI();
        }

        bool moving = _dragging || _pos != _targetIndex || _vel != 0f;

        if (!_dragging)
        {
            _pos = Mathf.SmoothDamp(_pos, _targetIndex, ref _vel, smoothTime, Mathf.Infinity, dt);

            if (Mathf.Abs(_pos - _targetIndex) < SettleEps && Mathf.Abs(_vel) < SettleEps)
            {
                _pos = _targetIndex;
                _vel = 0f;
            }
        }

        float panelVis = _panel != null ? _panel.Visibility : 1f;
        bool panelChanged = !Mathf.Approximately(panelVis, _lastPanelVis);

        if (moving || panelChanged || _layoutDirty)
        {
            Layout();
            DetectLargeSelectedBall();
            _lastPanelVis = panelVis;
            _layoutDirty = false;
        }

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

        SkinService.Flush();
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
            if (!_warnedNoBall)
            {
                _warnedNoBall = true;
                Debug.LogWarning("SkinCarousel: BallSkinController no esta asignado en el Inspector.", this);
            }
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
        _layoutDirty = true;
        if (confirmButton != null)
            confirmButton.interactable = !_unlocking;

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
                SetBuyButtonVisible(false);
            }
            else
            {
                SetBuyButtonVisible(true);

                bool priceOnBall = priceText != null;

                if (buyButtonText != null)
                {
                    buyButtonText.text = priceOnBall ? buyLabel : currentSkin.price.ToString();
                }

                int playerPoints = PointsManager.Instance != null ? PointsManager.Instance.GetCurrentScore() : 0;
                bool canAfford = playerPoints >= currentSkin.price;

                buyButton.interactable = !_unlocking;

                if (buyButtonText != null)
                    buyButtonText.color = canAfford ? _buyTextDefaultColor : cantAffordTextColor;
            }
        }
    }

    void SetBuyButtonVisible(bool visible)
    {
        if (buyButton == null)
            return;

        if (visible == _buyVisible && buyButton.gameObject.activeSelf == visible)
            return;

        _buyVisible = visible;

        if (!visible)
        {
            buyButton.gameObject.SetActive(false);
            return;
        }

        buyButton.gameObject.SetActive(true);

        if (_buyFeedback != null)
            _buyFeedback.Appear(buyAppearDelay, buyAppearOffset, buyInLayoutGroup);
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
        SkinService.Flush();
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
            t += FrameTime;
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
            hold += FrameTime;
            yield return null;
        }

        float f = 0f;
        while (f < popFadeTime)
        {
            f += FrameTime;
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
            bool visible = abs < fadeEnd && scale > 0.01f;

            if (!visible && !_itemVisible[i])
                continue;

            _itemVisible[i] = visible;

            float brightness = Mathf.Lerp(1f, sideBrightness, t);

            _items[i].Rect.anchoredPosition = new Vector2(d * spacing, 0f);
            _items[i].SetScale(scale);
            _items[i].SetVisual(brightness, visible);
        }

        int nearest = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, Count - 1);

        if (nearest != _lastNearest)
        {
            _lastNearest = nearest;
            ReorderSiblings(nearest);

            if (priceText != null)
                priceText.rectTransform.SetAsLastSibling();
        }

        UpdatePriceLabel(panelVis);
    }

    void ReorderSiblings(int nearest)
    {
        int maxDist = Mathf.Max(nearest, Count - 1 - nearest);

        for (int dist = maxDist; dist >= 1; dist--)
        {
            int l = nearest - dist;
            int r = nearest + dist;
            if (l >= 0) _items[l].Rect.SetAsLastSibling();
            if (r < Count) _items[r].Rect.SetAsLastSibling();
        }

        _items[nearest].Rect.SetAsLastSibling();
    }

    void UpdatePriceLabel(float panelVis)
    {
        if (priceText == null)
            return;

        SkinData skin = Current;
        bool show = skin != null && !_unlocking && !SkinService.IsUnlocked(skin);

        if (!show)
        {
            if (priceText.gameObject.activeSelf)
                priceText.gameObject.SetActive(false);
            _priceShown = -1;
            return;
        }

        if (!priceText.gameObject.activeSelf)
            priceText.gameObject.SetActive(true);

        RectTransform rt = priceText.rectTransform;
        rt.anchoredPosition = new Vector2((_targetIndex - _pos) * spacing, priceOffsetY);
        rt.localScale = Vector3.one * panelVis;

        if (_priceShown != skin.price)
        {
            priceText.text = skin.price.ToString();
            _priceShown = skin.price;
        }
    }
}