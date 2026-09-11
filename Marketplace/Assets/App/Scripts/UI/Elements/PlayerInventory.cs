using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField]
    private RectTransform itemContainer;

    [SerializeField]
    private UserItemCard userItemCardPrefab;

    private Dictionary<string, UserItemCard> _items;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _items = new();
        FetchUserItems();
    }

    private void OnEnable()
    {
        InventoryService.Instance.OnItemSold += OnItemSold;
        InventoryService.Instance.OnItemBought += FetchUserItemsPreserveEquipped;
        InventoryService.Instance.OnSingleItemBought += AddSingleItem;
        InventoryService.Instance.OnItemEquipChange += UpdateItemsEquipped;
        InventoryService.Instance.OnSubscriptionExpired += OnSubscriptionExpired;
    }

    private void OnDisable()
    {
        InventoryService.Instance.OnItemSold -= OnItemSold;
        InventoryService.Instance.OnItemBought -= FetchUserItemsPreserveEquipped;
        InventoryService.Instance.OnSingleItemBought -= AddSingleItem;
        InventoryService.Instance.OnItemEquipChange -= UpdateItemsEquipped;
        InventoryService.Instance.OnSubscriptionExpired -= OnSubscriptionExpired;
    }

    private void OnItemSold(UserItemData data)
    {
        //remove item card from inventory
        UserItemCard itemCard = GetItemCardByItemId(data.itemId);
        if (itemCard != null)
        {
            _items.Remove(data.itemId);
            Destroy(itemCard.gameObject);
        }
    }

    private void OnSubscriptionExpired()
    {
        UserItemCard card = GetItemCardByItemId("subscription_no_ads");
        if (card != null)
        {
            _items.Remove("subscription_no_ads");
            Destroy(card.gameObject);
        }
    }

    private UserItemCard GetItemCardByItemId(string itemId)
    {
        UserItemCard itemCard = null;
        if (_items.ContainsKey(itemId))
        {
            itemCard = _items[itemId];
        }

        return itemCard;
    }

    private List<UserItemCard> GetItemCardsByEquippableSlot(string equippableSlot)
    {
        List<UserItemCard> cards = new();
        foreach(var item in _items)
        {
            if (item.Value.data.equippableSlot.Equals(equippableSlot))
            {
                cards.Add(item.Value);
            }
        }

        return cards;
    }

    private void AddSingleItem(UserItemData item)
    {
        ProcessItems(new List<UserItemData> { item });
    }

    private void FetchUserItemsPreserveEquipped()
    {
        InventoryService.Instance.GetUserInventoryItems((List<UserItemData> items) =>
        {
            ProcessItems(items);
        }, (string error) =>
        {
            Debug.LogError("Could not get user items " + error);
        }, refreshEquipped: false);
    }

    private void FetchUserItems()
    {
        InventoryService.Instance.GetUserInventoryItems((List<UserItemData> items) =>
        {
            ProcessItems(items);
        }, (string error) =>
        {
            Debug.LogError("Could not get user items " + error);
        });
    }

    private void UpdateItemsEquipped(UserItemData item)
    {
        if (!item.isEquipped)
            return;

        List<UserItemCard> cards = GetItemCardsByEquippableSlot(item.equippableSlot);
        foreach (var itemCard in cards)
        {
            bool isEquippedCard = itemCard.data.itemId.Equals(item.itemId);
            itemCard.UpdateEquippedStatus(isEquippedCard);
        }
    }


    private void ProcessItems(List<UserItemData> items)
    {
        foreach(UserItemData item in items)
        {
            //if we already have this item just update it otherwise instantiate new card
            if (_items.ContainsKey(item.itemId))
            {
                _items[item.itemId].SetUserItemData(item);
            }
            else
            {
                UserItemCard itemCard = Instantiate(userItemCardPrefab, itemContainer);
                itemCard.transform.localScale = Vector3.one;

                itemCard.SetUserItemData(item);
                _items.Add(item.itemId, itemCard);
            }
        }
        ReorderCards();
    }

    private void ReorderCards()
    {
        var sorted = new List<UserItemCard>(_items.Values);
        sorted.Sort((a, b) => GetInventoryItemOrder(a.data).CompareTo(GetInventoryItemOrder(b.data)));
        for (int i = 0; i < sorted.Count; i++)
            sorted[i].transform.SetSiblingIndex(i);
    }

    private static int GetInventoryItemOrder(UserItemData item)
    {
        if (item.isBundle)      return 0;
        if (item.isActivatable) return 1;
        return 2;
    }

}
