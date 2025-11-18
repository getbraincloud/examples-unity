using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RewardPickup : MonoBehaviour
{
    [SerializeField] private Sprite[] RewardSprites;

    private Button _pickUpButton;
    private Image _pickUpImage;
    private int _rewardAmount;
    private CurrencyTypes _currencyType;
    
    public int RewardAmount { get { return _rewardAmount; } }
    public CurrencyTypes CurrencyType { get { return _currencyType; } }

    private void Awake()
    {
        _pickUpImage = GetComponent<Image>();
        _pickUpButton = GetComponent<Button>();
        _pickUpButton.onClick.AddListener(OnPickUpPressed);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void SetUpPickup(CurrencyTypes in_currencyType, int in_rewardAmount)
    {
        _currencyType = in_currencyType;
        _pickUpImage.sprite = RewardSprites[(int)in_currencyType];
        _rewardAmount = in_rewardAmount;
    }
    
    private void OnPickUpPressed()
    {
        // Notify Toy Manager of pick up and send relevant info 
        ToyManager.Instance.AddRewardPickup(this);
        StartCoroutine(DelayToDestroy());
    }
    
    private IEnumerator DelayToDestroy()
    {
        yield return new WaitForFixedUpdate();
        Destroy(gameObject);
    }
}
