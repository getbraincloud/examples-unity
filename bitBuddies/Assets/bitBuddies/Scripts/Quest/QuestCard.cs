using BrainCloud.JSONHelper;
using Gameframework;
using System;
using System.Collections.Generic;
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
    public QuestTypes QuestType;

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

    private QuestPanel _questPanel;
    private QuestInfo _questInfo;
    public QuestInfo QuestInfo
    {
        set { SetupCard(value); }
        get { return _questInfo; }
    }

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
        BrainCloudManager.Client.ScriptService.RunScript
        (
            BitBuddiesConsts.CLAIM_QUEST_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Claim Quest Success", _questPanel.OnClaimButtonSuccess)
        );
    }

    public void SetupCard(QuestInfo in_questInfo)
    {
        _questPanel = FindAnyObjectByType<QuestPanel>();
        _questInfo = in_questInfo;
        QuestTitleText.text = _questInfo.QuestTitle;
        StatTracker.OnStatChanged += OnStatChange;
        int currentQuestIndex = 0;
        switch (_questInfo.QuestType)
        {
            case QuestTypes.BitBuddies:
                currentQuestIndex = StatTracker.Instance.GetStat(BitBuddiesConsts.BITBUDDIES_QUESTLINEID);
                break;
            case QuestTypes.General:
                currentQuestIndex = StatTracker.Instance.GetStat(BitBuddiesConsts.GENERAL_QUESTLINEID);
                break;
            case QuestTypes.BitBling:
                currentQuestIndex = StatTracker.Instance.GetStat(BitBuddiesConsts.BITBLING_QUESTLINEID);
                break;
        }
        int questValue = StatTracker.Instance.GetStat(_questInfo.QuestStatToTrack);
        ProgressText.enabled = false;
        if (questValue >= _questInfo.QuestRequiredProgress)
        {
            if (currentQuestIndex == _questInfo.QuestLineIndex)
            {
                ProgressSlider.maxValue = _questInfo.QuestRequiredProgress;
                if (questValue > _questInfo.QuestRequiredProgress)
                {
                    ProgressSlider.value = _questInfo.QuestRequiredProgress;
                }
                else
                {
                    ProgressSlider.value = questValue;
                }

                ProgressText.text = $"{ProgressSlider.value}/{_questInfo.QuestRequiredProgress}";
                ClaimButton.gameObject.SetActive(true);
                ProgressText.enabled = true;
                ClaimButton.onClick.AddListener(OnClaimButton);
            }
            else if (currentQuestIndex > _questInfo.QuestLineIndex || _questInfo.QuestStatus == QUEST_STATUS.SATISFIED)
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
            if (_questInfo.QuestStatus == QUEST_STATUS.UNLOCKED || _questInfo.QuestStatus == QUEST_STATUS.IN_PROGRESS)
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

        switch (_questInfo.QuestStatus)
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
