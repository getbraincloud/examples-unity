using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSection : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _headerTitle;
    [SerializeField]
    private Image _icon;
    [SerializeField]
    private StoreItemCard _cardPrefab;


    [SerializeField]
    private RectTransform _cardsSection;

    public Dictionary<string, StoreItemCard> cards { get; private set; }

    public void InitializeSection(string title, Sprite sectionIcon = null)
    {
        if(cards == null) 
            cards = new Dictionary<string, StoreItemCard>();

        _headerTitle.text = title;
        _icon.sprite = sectionIcon;
    }

    public void AddStoreItem(StoreItemData itemData)
    {
        StoreItemCard card = Instantiate(_cardPrefab, _cardsSection);
        card.transform.localScale = Vector3.one;

        card.SetStoreItemData(itemData);

        cards.Add(itemData.Key, card);
    }

    public void UpdateStoreItem(StoreItemData itemData)
    {
        if (cards.ContainsKey(itemData.Key))
            cards[itemData.Key].SetStoreItemData(itemData);
        else
            AddStoreItem(itemData);
    }

    public void RemoveStoreItem(string key)
    {
        if (cards.TryGetValue(key, out StoreItemCard card))
        {
            Destroy(card.gameObject);
            cards.Remove(key);
        }
    }

    public void ClearItems()
    {
        //remove all cards within this section

        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
