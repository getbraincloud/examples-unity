using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quest Panel shows Quest cards and shows their progress.
/// There are 3 quest lines: General, BitBuddies, and BitBling.
/// Each quest line has 6 quests.
/// Each quest page displays 2 Quest Cards.
///
/// Script handles the claim quest rewards button logic when a quest is completed and claimed.
/// Stats for quests will be tracked in the StatTracker and won't be gated by quests to increment.
/// Panel will have 3 pages of quest cards.
/// </summary>
public class QuestPanel : MonoBehaviour
{
    [SerializeField] private QuestCard QuestCardPrefab;
    [SerializeField] private Transform BitBuddiesSpawnPoint;
    [SerializeField] private Transform BitBlingSpawnPoint;
    [SerializeField] private Transform GeneralSpawnPoint;
    [SerializeField] private Button NextPageButton;
    [SerializeField] private Button PreviousPageButton;
    [SerializeField] private Button CloseButton;
    [SerializeField] private TextMeshProUGUI PageNumberText;
    [SerializeField] private TextMeshProUGUI GeneralQuestCompletedText;
    [SerializeField] private TextMeshProUGUI BitBuddiesQuestCompletedText;
    [SerializeField] private TextMeshProUGUI BitBlingQuestCompletedText;

    private QuestCard _firstRowBitBuddyCard;
    private QuestCard _firstRowBitBlingCard;
    private QuestCard _firstRowGeneralCard;

    private QuestCard _secondRowBitBuddyCard;
    private QuestCard _secondRowBitBlingCard;
    private QuestCard _secondRowGeneralCard;

    private int _pageIndex;

    //Response logic for claiming a quest
    public void OnClaimButtonSuccess(string jsonResponse)
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
        if (rewards == null) return;
        UserInfo userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        if (rewards.ContainsKey("coins"))
        {
            int coinReward = (int)rewards["coins"];
            userInfo.UpdateCoins(coinReward + userInfo.Coins);
            PopUpUI.Show("Quest Rewards")
                   .AddBodyText($"You have received {coinReward:N0} coins!");
        }
        else if (rewards.ContainsKey("gems"))
        {
            int gemReward = (int)rewards["gems"];
            userInfo.UpdateGems(gemReward + userInfo.Gems);
            PopUpUI.Show("Quest Rewards")
                   .AddBodyText($"You have received {gemReward:N0} gems!");

        }
        string questLineId = response["questLine"] as string;
        int questLineIndex = Convert.ToInt32(response["questLineIndex"]);
        GameManager gameManager = GameManager.Instance;
        List<QuestInfo> listOfQuests = new List<QuestInfo>();
        switch (questLineId)
        {
            case BitBuddiesConsts.GENERAL_QUESTLINEID:
                listOfQuests = gameManager.GeneralQuests;
                StatTracker.Instance.SetStat(BitBuddiesConsts.GENERAL_QUESTLINEID, questLineIndex);
                break;
            case BitBuddiesConsts.BITBLING_QUESTLINEID:
                listOfQuests = gameManager.BitBlingQuests;
                StatTracker.Instance.SetStat(BitBuddiesConsts.BITBLING_QUESTLINEID, questLineIndex);
                break;
            case BitBuddiesConsts.BITBUDDIES_QUESTLINEID:
                listOfQuests = gameManager.BitBuddiesQuests;
                StatTracker.Instance.SetStat(BitBuddiesConsts.BITBUDDIES_QUESTLINEID, questLineIndex);
                break;
        }
        if (questLineIndex <= listOfQuests.Count - 1)
        {
            for (int i = 0; i < listOfQuests.Count; i++)
            {
                if (listOfQuests[i].QuestLineIndex == questLineIndex)
                {
                    QuestInfo completedQuest = listOfQuests[i - 1];
                    completedQuest.QuestStatus = QUEST_STATUS.SATISFIED;
                    QuestInfo unlockedQuest = listOfQuests[i];
                    unlockedQuest.QuestStatus = QUEST_STATUS.IN_PROGRESS;

                    listOfQuests[i - 1] = completedQuest;
                    listOfQuests[i] = unlockedQuest;
                    break;
                }
            }
        }
        else
        {
            QuestInfo lastQuest = listOfQuests[listOfQuests.Count - 1];
            lastQuest.QuestStatus = QUEST_STATUS.SATISFIED;
            listOfQuests[listOfQuests.Count - 1] = lastQuest;
        }

        switch (questLineId)
        {
            case BitBuddiesConsts.GENERAL_QUESTLINEID:
                gameManager.GeneralQuests = listOfQuests;
                break;
            case BitBuddiesConsts.BITBLING_QUESTLINEID:
                gameManager.BitBlingQuests = listOfQuests;
                break;
            case BitBuddiesConsts.BITBUDDIES_QUESTLINEID:
                gameManager.BitBuddiesQuests = listOfQuests;
                break;
        }

        UpdatePanel();
        StateManager.Instance.RefreshScreen();
    }

    public void SetUpPanel()
    {
        GameManager gameManager = GameManager.Instance;
        NextPageButton.onClick.AddListener(OnNextPageButton);
        PreviousPageButton.onClick.AddListener(OnPreviousPageButton);
        CloseButton.onClick.AddListener(OnCloseButtonPressed);
        _pageIndex = 0;
        SetPageText();
        SetUpQuestCard(gameManager.GeneralQuests, GeneralSpawnPoint);
        SetUpQuestCard(gameManager.BitBuddiesQuests, BitBuddiesSpawnPoint);
        SetUpQuestCard(gameManager.BitBlingQuests, BitBlingSpawnPoint);

        SetCompletedQuestText(GeneralQuestCompletedText, gameManager.GeneralQuests);
        SetCompletedQuestText(BitBuddiesQuestCompletedText, gameManager.BitBuddiesQuests);
        SetCompletedQuestText(BitBlingQuestCompletedText, gameManager.BitBlingQuests);
    }

    private void SetCompletedQuestText(TextMeshProUGUI questCompletedText, List<QuestInfo> listOfQuests)
    {
        int completedQuests = 0;
        for (int i = 0; i < listOfQuests.Count; i++)
        {
            if (listOfQuests[i].QuestStatus == QUEST_STATUS.SATISFIED)
            {
                completedQuests++;
            }
        }
        questCompletedText.text = $"{completedQuests}/{listOfQuests.Count}";
    }

    private void SetPageText()
    {
        PageNumberText.text = (_pageIndex + 1).ToString();
    }

    private void UpdatePanel()
    {
        foreach (Transform child in GeneralSpawnPoint)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in BitBuddiesSpawnPoint)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in BitBlingSpawnPoint)
        {
            Destroy(child.gameObject);
        }

        GameManager gameManager = GameManager.Instance;
        SetUpQuestCard(gameManager.GeneralQuests, GeneralSpawnPoint);
        SetUpQuestCard(gameManager.BitBuddiesQuests, BitBuddiesSpawnPoint);
        SetUpQuestCard(gameManager.BitBlingQuests, BitBlingSpawnPoint);
        SetPageText();
        SetCompletedQuestText(GeneralQuestCompletedText, gameManager.GeneralQuests);
        SetCompletedQuestText(BitBuddiesQuestCompletedText, gameManager.BitBuddiesQuests);
        SetCompletedQuestText(BitBlingQuestCompletedText, gameManager.BitBlingQuests);
    }

    private void OnDestroy()
    {
        NextPageButton.onClick.RemoveAllListeners();
        PreviousPageButton.onClick.RemoveAllListeners();
        CloseButton.onClick.RemoveAllListeners();
    }

    private void OnNextPageButton()
    {
        _pageIndex++;
        if (_pageIndex > 2)
        {
            _pageIndex = 2;
        }
        UpdatePanel();
    }

    private void OnPreviousPageButton()
    {
        _pageIndex--;
        if (_pageIndex < 0)
        {
            _pageIndex = 0;
        }
        UpdatePanel();
    }

    /// <summary>
    /// Setting up a Quest Line Row with 2 Quest Cards per row.
    /// </summary>
    /// <param name="listOfQuests">list of quests for the current quest line</param>
    /// <param name="spawnParent">transform reference to spawn quest cards</param>
    private void SetUpQuestCard(List<QuestInfo> listOfQuests, Transform spawnParent)
    {
        QuestInfo firstRowQuest = GetFirstRow(listOfQuests);
        QuestInfo secondRowQuest = GetSecondRow(listOfQuests);

        if (secondRowQuest.QuestStatToTrack.IsNullOrEmpty())
        {
            Debug.Log("Quest stat to track is null or empty");
            return;
        }
        QuestCard firstRowQuestCard = Instantiate(QuestCardPrefab, spawnParent);
        firstRowQuestCard.SetupCard(firstRowQuest);

        if (firstRowQuest.QuestStatToTrack.IsNullOrEmpty())
        {
            Debug.Log("Next quest in line is null or empty");
            return;
        }
        QuestCard secondRowQuestCard = Instantiate(QuestCardPrefab, spawnParent);
        secondRowQuestCard.SetupCard(secondRowQuest);
    }

    //Getting quest info for the first row in the current quest line
    private QuestInfo GetFirstRow(List<QuestInfo> listOfQuests)
    {
        int questIndex;
        if (_pageIndex == 1)
        {
            questIndex = 2;
        }
        else if (_pageIndex == 2)
        {
            questIndex = 4;
        }
        else
        {
            questIndex = 0;
        }

        for (int i = 0; i < listOfQuests.Count; i++)
        {
            if (listOfQuests[i].QuestLineIndex == questIndex)
            {
                return listOfQuests[i];
            }
        }

        return new QuestInfo();
    }

    //Getting quest for the second row in the current quest line
    private QuestInfo GetSecondRow(List<QuestInfo> listOfQuests)
    {
        int questIndex;
        if (_pageIndex == 1)
        {
            questIndex = 3;
        }
        else if (_pageIndex == 2)
        {
            questIndex = 5;
        }
        else
        {
            questIndex = 1;
        }

        for (int i = 0; i < listOfQuests.Count; i++)
        {
            if (listOfQuests[i].QuestLineIndex == questIndex)
            {
                return listOfQuests[i];
            }
        }

        return new QuestInfo();
    }

    private void OnCloseButtonPressed()
    {
        Destroy(gameObject);
    }
}
