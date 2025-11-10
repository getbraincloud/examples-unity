using System;
using UnityEngine;
using UnityEngine.UI;

public class RewardPickup : MonoBehaviour
{
    [SerializeField] private Sprite[] RewardSprites;

    private Button _pickUpButton;
    private Image _pickUpImage;
    private int _rewardAmount;

    private void Awake()
    {
        _pickUpImage = GetComponent<Image>();
        _pickUpButton = GetComponent<Button>();
        _pickUpButton.onClick.AddListener(OnPickUpPressed);
    }

    public void SetUpPickup(CurrencyTypes in_currencyType, int in_rewardAmount)
    {
        _pickUpImage.sprite = RewardSprites[(int)in_currencyType];
        _rewardAmount = in_rewardAmount;
    }
    
    private void OnPickUpPressed()
    {
        // Notify Toy Manager of pick up and send relevant info 
        
        Destroy(gameObject);
    }
}
