using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using UnityEngine;

public class StoreWindow : MonoBehaviour
{
    [SerializeField]
    private ItemSection sectionPrefab;

    [SerializeField]
    private RectTransform storeContainer;

    private Dictionary<string, ItemSection> _sections;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _sections = new Dictionary<string, ItemSection>();
        FetchUserItems();
    }

    private void FetchUserItems()
    {
        InventoryService.Instance.GetAllUserItems((List<StoreItemData> items) =>
        {
            ProcessItems(items);
        }, (string error) =>
        {
            Debug.LogError("Couldn't get user items: " + error);
        });
    }


    private void ProcessItems(List<StoreItemData> items)
    {
        //first check if we have a section by items category name
        //if section exists, add item to section, otherwise create section and add item to it
        foreach (StoreItemData item in items)
        {
            if (_sections.ContainsKey(item.category))
            {
                _sections[item.category].AddStoreItem(item);
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
        
    }
}
