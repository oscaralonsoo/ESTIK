using System;
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

    public event Action<SkinData> SelectionChanged;

    public SkinData Current => catalog.skins[_targetIndex];

    SkinCarouselItem[] _items;
    Canvas _canvas;

    float _pos, _vel, _dragVel;

    int _targetIndex;
    int _lastNearest = -1;
    int _lastSavedIndex = -1;

    bool _dragging;

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
        _lastNearest = -1;
        _lastSavedIndex = -1;

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

        if (_dragging)
        {
            int nearest = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, Count - 1);
            if (nearest != _targetIndex)
                SelectIndex(nearest);
        }
    }
    public void OnBeginDrag(PointerEventData e)
    {
        _dragging = true;
        _dragVel = 0f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (spacing <= 0f)
            return;

        float deltaIdx = -e.delta.x / _canvas.scaleFactor / spacing;

        _pos = Mathf.Clamp(_pos + deltaIdx, -overscroll, Count - 1 + overscroll);

        float instant = deltaIdx / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _dragVel = Mathf.Lerp(_dragVel, instant, 0.5f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _dragging = false;
        _vel = 0f;

        SelectIndex(Mathf.RoundToInt(_pos + _dragVel * inertia));
    }
    public void SelectIndex(int index)
    {
        if (Count == 0)
            return;

        index = Mathf.Clamp(index, 0, Count - 1);

        if (index != _targetIndex)
        {
            _targetIndex = index;
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
            prevButton.interactable = _targetIndex > 0;

        if (nextButton != null)
            nextButton.interactable = _targetIndex < Count - 1;

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
                buyButton.interactable = playerPoints >= currentSkin.price;
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
        if (buyButton == null)
            return;
        SkinData currentSkin = Current;
        if (currentSkin == null || SkinService.IsUnlocked(currentSkin))
            return;

        int price = currentSkin.price;
        int playerPoints = PointsManager.Instance != null ? PointsManager.Instance.GetCurrentScore() : 0;

        if (playerPoints < price)
        {
            Debug.Log("No tienes suficientes puntos para comprar esta skin.");
            return;
        }

        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.SpendPoints(price);
            PointsManager.Instance.AnimateScoreSpend(price);
        }

        int buyIndex = _targetIndex;
        SkinCarouselItem buyItem = _items[buyIndex];
        Vector3 targetBallPos = buyItem.GetBallPosition();

        if (buyButton != null)
            buyButton.interactable = false;

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

        RefreshUI();
        Debug.Log("¡Skin comprada y equipada: " + skin.displayName + "!");
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