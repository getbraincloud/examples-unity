using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreWindow : MonoBehaviour
{
    [SerializeField]
    private ItemSection sectionPrefab;

    [SerializeField]
    private RectTransform storeContainer;

    [SerializeField]
    private ScrollRect _scrollView;

    [SerializeField]
    private ToggleGroup _toggleGroup;

    [SerializeField]
    private Toggle _toggleFreebies, _toggleBundles, _toggleItems, _toggleProducts;

    [SerializeField]
    private float _scrollOffset = -190f;

    [SerializeField]
    private float _scrollDuration = 0.3f;

    private Dictionary<string, ItemSection> _sections;
    private Coroutine _scrollCoroutine;

    void Start()
    {
        _sections = new Dictionary<string, ItemSection>();
        _toggleFreebies.SetIsOnWithoutNotify(true);
        FetchStoreItems();
    }

    private void OnEnable()
    {
        InventoryService.Instance.OnItemBought += FetchStoreItems;
        InventoryService.Instance.OnSingleItemBought += OnAnyItemBought;

        _toggleFreebies.onValueChanged.AddListener(on  => { if (on) ScrollToSection("Freebies"); });
        _toggleBundles.onValueChanged.AddListener(on   => { if (on) ScrollToSection("Bundles"); });
        _toggleItems.onValueChanged.AddListener(on     => { if (on) ScrollToSection("Items"); });
        _toggleProducts.onValueChanged.AddListener(on  => { if (on) ScrollToSection("Products"); });
    }

    private void OnDisable()
    {
        if (InventoryService.Instance != null)
        {
            InventoryService.Instance.OnItemBought -= FetchStoreItems;
            InventoryService.Instance.OnSingleItemBought -= OnAnyItemBought;
        }

        _toggleFreebies.onValueChanged.RemoveAllListeners();
        _toggleBundles.onValueChanged.RemoveAllListeners();
        _toggleItems.onValueChanged.RemoveAllListeners();
        _toggleProducts.onValueChanged.RemoveAllListeners();
    }

    private void OnAnyItemBought(UserItemData _) => FetchStoreItems();

    private void FetchStoreItems()
    {
        InventoryService.Instance.FetchStoreItems(InventoryService.GetPlatformStoreId(), (List<StoreItemData> items) =>
        {
            ProcessItems(items);
        }, (string error) =>
        {
            Debug.LogError("Couldn't get store items: " + error);
        });
    }

    private void ScrollToSection(string sectionName)
    {
        if (!_sections.ContainsKey(sectionName)) return;

        Canvas.ForceUpdateCanvases();

        RectTransform sectionRect = _sections[sectionName].GetComponent<RectTransform>();
        float contentHeight = storeContainer.rect.height;
        float viewportHeight = _scrollView.viewport.rect.height;
        float scrollableHeight = contentHeight - viewportHeight;

        if (scrollableHeight <= 0f) return;

        float distanceFromTop = -sectionRect.anchoredPosition.y + _scrollOffset;
        float normalizedPos = 1f - Mathf.Clamp01(distanceFromTop / scrollableHeight);

        if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
        _scrollCoroutine = StartCoroutine(SmoothScroll(normalizedPos));
    }

    private IEnumerator SmoothScroll(float targetPos)
    {
        float startPos = _scrollView.verticalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < _scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _scrollDuration);
            _scrollView.verticalNormalizedPosition = Mathf.Lerp(startPos, targetPos, t);
            yield return null;
        }

        _scrollView.verticalNormalizedPosition = targetPos;
        _scrollCoroutine = null;
    }

    private void ProcessItems(List<StoreItemData> items)
    {
        // Track which defIds the server returned, per section
        var returnedDefIds = new Dictionary<string, HashSet<string>>();

        foreach (StoreItemData item in items)
        {
            if (!returnedDefIds.ContainsKey(item.category))
                returnedDefIds[item.category] = new HashSet<string>();
            returnedDefIds[item.category].Add(item.defId);

            if (_sections.ContainsKey(item.category))
            {
                _sections[item.category].UpdateStoreItem(item);
            }
            else
            {
                ItemSection newSection = Instantiate(sectionPrefab, storeContainer);
                newSection.transform.localScale = Vector3.one;
                newSection.InitializeSection(item.category, ImageCacheService.Instance.GetSpriteForSection(item.category));
                _sections.Add(item.category, newSection);
                newSection.AddStoreItem(item);
            }
        }

        // Remove cards that are no longer returned (e.g. just-purchased non-consumables)
        var emptySections = new List<string>();
        foreach (var kvp in _sections)
        {
            HashSet<string> returned = returnedDefIds.TryGetValue(kvp.Key, out var set) ? set : new HashSet<string>();
            var toRemove = new List<string>(kvp.Value.cards.Keys);
            foreach (string defId in toRemove)
            {
                if (!returned.Contains(defId))
                    kvp.Value.RemoveStoreItem(defId);
            }

            if (kvp.Value.cards.Count == 0)
                emptySections.Add(kvp.Key);
        }

        foreach (string category in emptySections)
        {
            Destroy(_sections[category].gameObject);
            _sections.Remove(category);
        }
    }
}
