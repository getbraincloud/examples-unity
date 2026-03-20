using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParentShopItem : MonoBehaviour
{

    [SerializeField] private TMP_Text ItemNameText;
    [SerializeField] private TMP_Text ItemDescriptionText;
    [SerializeField] private TMP_Text ItemPriceText;
    [SerializeField] private Image ItemImage;


    private ParentShopInfo _parentShopInfo;
    
    
    public void Init(ParentShopInfo in_parentShopInfo)
    {
        _parentShopInfo = in_parentShopInfo;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
