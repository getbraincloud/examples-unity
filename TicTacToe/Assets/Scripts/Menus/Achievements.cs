#region

using System.Collections.Generic;
using BrainCloud.LitJson;
using UnityEngine;

#endregion

// Achievements are set on the brainCloud Dashboard, under Design | Gamification | Achievements
/**
[
    {
        "gameId": "10228",
        "achievementId": "WON_100_RANKED_MATCHES",
        "title": "Won 100 ranked matches.",
        "description": "Won 100 ranked matches.",
        "extraData": {
            "TODO": "Win 100 ranked matches.",
            "DONE": "Won 100 ranked matches."
        },
        "invisibleUntilEarned": false,
        "fbEnabled": false,
        "imageUrl": "/metadata/achievements/WON_100_RANKED_MATCHES.png",
        "fbGamePoints": null,
        "appleEnabled": false,
        "appleAchievementId": null,
        "steamEnabled": false,
        "steamAchievementId": null,
        "googleEnabled": false,
        "googleAchievementId": null,
        "absoluteImageUrl": "https://portal.braincloudservers.com/files/portal/g/10228/metadata/achievements/WON_100_RANKED_MATCHES.png"
    },
    {
        "gameId": "10228",
        "achievementId": "WON_10_RANKED_MATCHES",
        "title": "Won 10 ranked matches.",
        "description": "Won 10 ranked matches.",
        "extraData": {
            "TODO": "Win 10 ranked matches.",
            "DONE": "Won 10 ranked matches."
        },
        "invisibleUntilEarned": false,
        "fbEnabled": false,
        "imageUrl": "/metadata/achievements/WON_10_RANKED_MATCHES.png",
        "fbGamePoints": null,
        "appleEnabled": false,
        "appleAchievementId": null,
        "steamEnabled": false,
        "steamAchievementId": null,
        "googleEnabled": false,
        "googleAchievementId": null,
        "absoluteImageUrl": "https://portal.braincloudservers.com/files/portal/g/10228/metadata/achievements/WON_10_RANKED_MATCHES.png"
    },
    {
        "gameId": "10228",
        "achievementId": "WON_A_RANKED_MATCH",
        "title": "Won a ranked match.",
        "description": "Won a ranked match.",
        "extraData": {
            "TODO": "Win a ranked match.",
            "DONE": "Won a ranked match."
        },
        "invisibleUntilEarned": false,
        "fbEnabled": false,
        "imageUrl": "/metadata/achievements/WON_A_RANKED_MATCH.png",
        "fbGamePoints": null,
        "appleEnabled": false,
        "appleAchievementId": null,
        "steamEnabled": false,
        "steamAchievementId": null,
        "googleEnabled": false,
        "googleAchievementId": null,
        "absoluteImageUrl": "https://portal.braincloudservers.com/files/portal/g/10228/metadata/achievements/WON_A_RANKED_MATCH.png"
    }
]
 */

public class Achievements : ResourcesManager
{
    private readonly List<AchievementInfo> achievements = new List<AchievementInfo>();
    private Vector2 _scrollPos;

    [SerializeField]
    private RectTransform AchievementsScrollView = null;

    [SerializeField]
    private Spinner Spinner = null;

    private void Start()
    {
        App = MatchSelectObj.App;

        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;

        App.Bc.GamificationService.ReadAchievements(true, OnReadAchievementData);

        m_itemCell = new List<AchievementCell>();
    }

    public void OnReadAchievementData(string responseData, object cbPostObject)
    {
        achievements.Clear();

        var achievementData = JsonMapper.ToObject(responseData)["data"]["achievements"];

        foreach (JsonData achievement in achievementData)
            achievements.Add(new AchievementInfo(achievement));
    }

    public void OnUpdateUI()
    {
        PopulateScrollView(achievements, m_itemCell, AchievementsScrollView);
    }

    public void OnGotoLoginScene()
    {
        App.GotoLoginScene(gameObject);
    }

    private void PopulateScrollView(List<AchievementInfo> in_itemItems, List<AchievementCell> in_itemCell, RectTransform in_scrollView)
    {
        RemoveAllCellsInView(in_itemCell);
        if (in_itemItems.Count == 0)
        {
            return;
        }

        if (in_scrollView != null)
        {
            foreach (var achievement in in_itemItems)
            {
                AchievementCell newItem = CreateAchievementCell(in_scrollView, "Prefabs/AchievementCell");
                newItem.Init(achievement, this);
                newItem.transform.localPosition = Vector3.zero;
                in_itemCell.Add(newItem);

                if (achievement.Status.Equals("NOT_AWARDED"))
                    newItem.SetAchievementName(achievement.UnlockText);
                else
                    newItem.SetAchievementName(achievement.UnlockedText);
            }
        }
    }

    private AchievementCell CreateAchievementCell(Transform in_parent = null, string in_cellName = "")
    {
        AchievementCell toReturn = null;
        toReturn = CreateResourceAtPath(in_cellName, in_parent.transform).GetComponent<AchievementCell>();
        toReturn.transform.SetParent(in_parent);
        toReturn.transform.localScale = Vector3.one;
        return toReturn;
    }

    private void RemoveAllCellsInView(List<AchievementCell> in_itemCell)
    {
        AchievementCell item;
        for (int i = 0; i < in_itemCell.Count; ++i)
        {
            item = in_itemCell[i];
            Destroy(item.gameObject);
        }
        in_itemCell.Clear();
    }

    private List<AchievementCell> m_itemCell = null;

    public class AchievementInfo
    {
        public const string HOSTED_WEB_ROOT = "http://apps.braincloudservers.com/tictactoe-internal";
        public string Status;
        public string UnlockedText;
        public string UnlockText;
        public string ImageURL;

        public AchievementInfo(JsonData jsonData)
        {
            UnlockText = jsonData["extraData"]["TODO"].ToString();
            UnlockedText = jsonData["extraData"]["DONE"].ToString();
            Status = jsonData["status"].ToString();
#if UNITY_WEBGL
            ImageURL = HOSTED_WEB_ROOT + jsonData["extraData"]["webRelativeURL"].ToString();
#else
            ImageURL = jsonData["imageUrl"].ToString();
#endif
        }
    }
}