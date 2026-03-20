using System;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
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
    [SerializeField] private Button ClaimButton;
    [SerializeField] private Image BGImage;
    

    private QuestInfo _questInfo;

    private void OnStatChange(string statName, int incremented)
    {
        ProgressSlider.value = incremented;
    }

    private void OnDestroy()
    {
        StatTracker.OnStatChanged -= OnStatChange;
        ClaimButton.onClick.RemoveAllListeners();
    }
    
    private void OnClaimButton()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("questName", _questInfo.QuestTitle);
        scriptData.Add("questIndex", _questInfo.QuestLineIndex);
        scriptData.Add("questScore", StatTracker.Instance.GetStat(_questInfo.QuestStatToTrack));
        BrainCloudManager.Client.ScriptService.RunScript(BitBuddiesConsts.CLAIM_QUEST_SCRIPT_NAME, scriptData.Serialize(), BrainCloudManager.HandleSuccess("Claim Quest Success", OnClaimButtonSuccess));
    }
    
    private void OnClaimButtonSuccess(string jsonResponse)
    {
        /*
         * {"packetId":3,"responses":[{"data":{"runTimeData":{"hasIncludes":false,"compileTime":2202,"scriptSize":1520,"renderTime":5,"executeTime":43529},
         * "response":{"reward":{"coins":1000},"questLine":"generalQuestLine","questLineIndex":1.0},"success":true,"reasonCode":null},"status":200}]}
         */
        //Get index info
        //Get Reward Info
        Dictionary<string, object> data = jsonResponse.Deserialize("data");
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        
        var rewards = response["reward"] as Dictionary<string, object>;
        int coinReward = (int) rewards["coins"];
        UserInfo userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        userInfo.UpdateCoins(coinReward + userInfo.Coins);
        
        //FL TODO: Complete quest locally
        string questLineId = response["questLine"] as string;
        int questLineIndex = (int) response["questLineIndex"];
    }

    public void SetupCard(QuestInfo in_questInfo)
    {
        _questInfo = in_questInfo;
        QuestTitleText.text = _questInfo.QuestTitle;
        StatTracker.OnStatChanged += OnStatChange;
        int questValue = StatTracker.Instance.GetStat(_questInfo.QuestStatToTrack);
        ProgressText.enabled = false;
        if(questValue >= _questInfo.QuestRequiredProgress)
        {
            if(_questInfo.QuestStatus == QUEST_STATUS.UNLOCKED || _questInfo.QuestStatus == QUEST_STATUS.IN_PROGRESS)
            {
                ClaimButton.gameObject.SetActive(true);
                ProgressText.enabled = true;
                ClaimButton.onClick.AddListener(OnClaimButton);          
                //ProgressSlider.fillRect.GetComponent<Image>().color = Color.blue;
                
                ProgressSlider.value = questValue;
                ProgressSlider.maxValue = _questInfo.QuestRequiredProgress;
                ProgressText.text = $"{_questInfo.CurrentProgress}/{_questInfo.QuestRequiredProgress}";
            }
            else if(_questInfo.QuestStatus == QUEST_STATUS.SATISFIED)
            {
                ClaimButton.gameObject.SetActive(false);
                ProgressSlider.fillRect.GetComponent<Image>().color = Color.green;
                BGImage.color = Color.green;
            }
            else
            {
                ClaimButton.gameObject.SetActive(false);
                ProgressSlider.fillRect.GetComponent<Image>().color = Color.grey;
                BGImage.color = Color.grey;
            }
        }
        else
        {
            ClaimButton.gameObject.SetActive(false);
            if(_questInfo.QuestStatus == QUEST_STATUS.UNLOCKED || _questInfo.QuestStatus == QUEST_STATUS.IN_PROGRESS)
            {
                //ProgressSlider.fillRect.GetComponent<Image>().color = Color.blue;
                ProgressText.enabled = true;
                ProgressSlider.value = questValue;
                ProgressSlider.maxValue = _questInfo.QuestRequiredProgress;
                ProgressText.text = $"{_questInfo.CurrentProgress}/{_questInfo.QuestRequiredProgress}";
            }
            else
            {
                ProgressSlider.fillRect.GetComponent<Image>().color = Color.grey;
                BGImage.color = Color.grey;
            }
        }
        
        //FL ToDo: Remove this debug code before release
        if(GameManager.Instance.Debug)
        {
            ProgressText.enabled = true;
            ProgressSlider.value = questValue;
            ProgressSlider.maxValue = _questInfo.QuestRequiredProgress;
            ProgressText.text = $"{_questInfo.CurrentProgress}/{_questInfo.QuestRequiredProgress}";
        }

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
            case QUEST_STATUS.SATISFIED:
                LockImage.gameObject.SetActive(false);
                break;
        }
    }
}
