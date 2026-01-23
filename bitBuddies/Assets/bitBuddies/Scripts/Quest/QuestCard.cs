using System;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct QuestInfo
{
    public string QuestTitle;
    public string QuestIconPath;
    public string QuestStatToTrack;
    public string QuestId;
    public int QuestRequiredProgress;
    public int QuestRewardAmount;
    public int CurrentQuestProgress;
    public QUEST_STATUS QuestStatus;
    public CurrencyTypes RewardCurrencyType;
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
        //ToDo: Figure out where to get the current stat for this quest
        ProgressSlider.maxValue = _questInfo.QuestRequiredProgress;

        switch (_questInfo.RewardCurrencyType)
        {
            case CurrencyTypes.Coins:
            
                break;
            
            case CurrencyTypes.Gems:
                break;
        }
    }

}
