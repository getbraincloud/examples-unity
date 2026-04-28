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
        InventoryService.Instance.OnItemBought += FetchUserItems;
        InventoryService.Instance.OnItemEquipChange += UpdateItemsEquipped;
        InventoryService.Instance.OnSubscriptionExpired += OnSubscriptionExpired;
    }

    private void OnDisable()
    {
        InventoryService.Instance.OnItemSold -= OnItemSold;
        InventoryService.Instance.OnItemBought -= FetchUserItems;
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
        {
            //if we are just unequipping an item, there is no need to unequip any other item
            return;
        }

        List<UserItemCard> cards = GetItemCardsByEquippableSlot(item.equippableSlot);
        Debug.Log("UpdateItemsEquipped itemID: " + item.itemId);
        //unequip any item that has the same equippableSlot
        foreach (var itemCard in cards)
        {
            if (!itemCard.data.itemId.Equals(item.itemId))
            {
                //if this is not the item we equipped, set it to unequipped
                itemCard.UpdateEquippedStatus(false);
                Debug.Log("Looping item " + itemCard.data.itemName + " ID: " + itemCard.data.itemId);
            }
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
    }

}
