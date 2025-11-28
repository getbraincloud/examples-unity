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
    private Action<Vector2> OnPickUp;
    private float _lifeSpan = 7f;
    private ToyBench _toyBench;
    private bool _isCollected;
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

    public void SetUpPickup(CurrencyTypes in_currencyType, int in_rewardAmount, Action<Vector2> in_onPickUp, ToyBench in_toyBench)
    {
        _currencyType = in_currencyType;
        _pickUpImage.sprite = RewardSprites[(int)in_currencyType];
        _rewardAmount = in_rewardAmount;
        _toyBench = in_toyBench;
        OnPickUp = in_onPickUp;
        StartCoroutine(DelayToDestroy());
    }
    
    public void OnPickUpPressed()
    {
        if (OnPickUp != null)
        {
            OnPickUp(transform.localPosition);
        }
    }
    
    public void PickUpCollected()
    {
        ToyManager.Instance.DecrementRewardSpawnCount();
        ToyManager.Instance.AddRewardPickup(this);
        StopAllCoroutines();
        Destroy(gameObject);        
    }
    
    private IEnumerator DelayToDestroy()
    {
        yield return new WaitForSeconds(_lifeSpan);
        Destroy(gameObject);
    }
}
