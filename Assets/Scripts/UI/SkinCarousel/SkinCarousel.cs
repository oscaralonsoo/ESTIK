// SkinCarousel.cs
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkinCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] SkinCatalog catalog;
    [SerializeField] SkinCarouselItem itemPrefab;
    [SerializeField] RectTransform content;
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;

    [Header("Layout")]
    [SerializeField] float spacing = 260f;
    [SerializeField]
    AnimationCurve scaleByDistance = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(1f, 0.55f), new Keyframe(2f, 0.4f));
    [SerializeField]
    AnimationCurve alphaByDistance = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(1f, 1f), new Keyframe(2f, 0f));

    [Header("Feel")]
    [SerializeField] float smoothTime = 0.12f;
    [SerializeField] float inertia = 0.15f;
    [SerializeField] float overscroll = 0.35f;

    public event Action<SkinData> SelectionChanged;
    public SkinData Current => catalog.skins[_targetIndex];

    SkinCarouselItem[] _items;
    Canvas _canvas;
    float _pos, _vel, _dragVel;
    int _targetIndex;
    int _lastNearest = -1;
    bool _dragging;

    int Count => catalog.skins.Length;

    void Awake()
    {
        if (catalog == null || itemPrefab == null || content == null)
        {
            Debug.LogError("SkinCarousel: faltan referencias en el Inspector", this);
            enabled = false;
            return;
        }

        _canvas = GetComponentInParent<Canvas>().rootCanvas;

        // Las flechas primero, para que nada las deje sin registrar
        if (prevButton != null) prevButton.onClick.AddListener(() => SelectIndex(_targetIndex - 1));
        if (nextButton != null) nextButton.onClick.AddListener(() => SelectIndex(_targetIndex + 1));

        _items = new SkinCarouselItem[Count];
        for (int i = 0; i < Count; i++)
        {
            var item = Instantiate(itemPrefab, content);
            item.Init(catalog.skins[i], i);
            _items[i] = item;
        }
    }

    void OnEnable()
    {
        if (_items == null) return;

        int start = Array.FindIndex(catalog.skins, s => s.id == SkinService.EquippedId);
        _targetIndex = Mathf.Max(0, start);
        _pos = _targetIndex;
        _vel = 0;
        _lastNearest = -1;
        Layout();
        RefreshUI();
    }

    void Start()
    {
        // Sincroniza la bola de la partida con la skin con la que se abre el panel
        if (_items != null) SelectionChanged?.Invoke(Current);
    }

    void Update()
    {
        if (!_dragging)
            _pos = Mathf.SmoothDamp(_pos, _targetIndex, ref _vel, smoothTime,
                                    Mathf.Infinity, Time.unscaledDeltaTime);
        Layout();

        // Mientras arrastras, el item central pasa a ser el seleccionado en cuanto cruza la mitad
        if (_dragging)
        {
            int nearest = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, Count - 1);
            if (nearest != _targetIndex) SelectIndex(nearest);
        }
    }

    // ---------- Input ----------
    public void OnBeginDrag(PointerEventData e) { _dragging = true; _dragVel = 0; }

    public void OnDrag(PointerEventData e)
    {
        float deltaIdx = -e.delta.x / _canvas.scaleFactor / spacing;
        _pos = Mathf.Clamp(_pos + deltaIdx, -overscroll, Count - 1 + overscroll);

        float instant = deltaIdx / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _dragVel = Mathf.Lerp(_dragVel, instant, 0.5f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _dragging = false;
        _vel = 0;
        SelectIndex(Mathf.RoundToInt(_pos + _dragVel * inertia));
    }

    // ---------- Selección ----------
    public void SelectIndex(int index)
    {
        index = Mathf.Clamp(index, 0, Count - 1);
        if (index == _targetIndex) return;

        _targetIndex = index;
        RefreshUI();

        SkinService.Equip(Current);          // solo equipa si está desbloqueada
        SelectionChanged?.Invoke(Current);
    }

    void RefreshUI()
    {
        if (prevButton != null) prevButton.interactable = _targetIndex > 0;
        if (nextButton != null) nextButton.interactable = _targetIndex < Count - 1;
        for (int i = 0; i < _items.Length; i++)
            _items[i].SetSelected(i == _targetIndex);
    }

    // ---------- Layout ----------
    void Layout()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            float d = i - _pos;
            float abs = Mathf.Abs(d);

            var rt = _items[i].Rect;
            rt.anchoredPosition = new Vector2(d * spacing, 0);
            rt.localScale = Vector3.one * scaleByDistance.Evaluate(abs);
            _items[i].SetAlpha(alphaByDistance.Evaluate(abs));
        }

        int nearest = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, Count - 1);
        if (nearest != _lastNearest)
        {
            _lastNearest = nearest;
            foreach (var it in _items.OrderByDescending(x => Mathf.Abs(x.Index - nearest)))
                it.Rect.SetAsLastSibling();
        }
    }
}