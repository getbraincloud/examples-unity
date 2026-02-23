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

        cards.Add(itemData.defId, card);
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
