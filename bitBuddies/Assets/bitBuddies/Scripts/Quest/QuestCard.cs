using System;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct QuestInfo
{
    public int QuestLineIndex;
    public string QuestTitle;
    public string QuestIconPath;
    public string QuestStatToTrack;
    public string QuestId;
    public int QuestRequiredProgress;
    public int QuestRewardAmount;
    public QUEST_STATUS QuestStatus;
    public CurrencyTypes RewardCurrencyType;
    
    public int CurrentProgress => StatTracker.Instance.GetStat(QuestStatToTrack);
    public float ProgressPercent => (float)CurrentProgress / QuestRequiredProgress;
}

public class QuestCard : MonoBehaviour
{
    [SerializeField] private Image LockImage;
    [SerializeField] private Image QuestIcon;
    [SerializeField] private TextMeshProUGUI QuestTitleText;
    [SerializeField] private TextMeshProUGUI ProgressText;
    [SerializeField] private Slider ProgressSlider;
    [SerializeField] private Image RewardIcon;
    [SerializeField] private TextMeshProUGUI RewardText;
    

    private QuestInfo _questInfo;
    
    public void SetupCard(QuestInfo in_questInfo)
    {
        _questInfo = in_questInfo;
        QuestTitleText.text = _questInfo.QuestTitle;
        ProgressSlider.value = StatTracker.Instance.GetStat(_questInfo.QuestStatToTrack);
        ProgressSlider.maxValue = _questInfo.QuestRequiredProgress;
        ProgressText.text = $"{_questInfo.CurrentProgress}/{_questInfo.QuestRequiredProgress}";
        RewardText.text = $"{_questInfo.QuestRewardAmount}";

        switch (_questInfo.RewardCurrencyType)
        {
            case CurrencyTypes.Coins:
                RewardIcon.sprite = AssetLoader.LoadSprite(BitBuddiesConsts.COIN_SPRITE_PATH);
                break;
            
            case CurrencyTypes.Gems:
                RewardIcon.sprite = AssetLoader.LoadSprite(BitBuddiesConsts.GEM_SPRITE_PATH);
                break;
        }
        
        switch(_questInfo.QuestStatus)
        {
            case QUEST_STATUS.LOCKED:
                LockImage.gameObject.SetActive(true);
                break;
            case QUEST_STATUS.IN_PROGRESS:
            case QUEST_STATUS.UNLOCKED:
                LockImage.gameObject.SetActive(false);
                break;
        }
    }
}
