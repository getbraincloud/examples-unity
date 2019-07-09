#region

using System.Collections.Generic;
using BrainCloud;
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

public class Achievements : GameScene
{
    private readonly List<AchievementInfo> achievements = new List<AchievementInfo>();
    private Vector2 _scrollPos;

    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;

        App.Bc.GamificationService.ReadAchievements(true, OnReadAchievementData);
    }

    public void OnReadAchievementData(string responseData, object cbPostObject)
    {
        achievements.Clear();

        var achievementData = JsonMapper.ToObject(responseData)["data"]["achievements"];

        foreach (JsonData achievement in achievementData) achievements.Add(new AchievementInfo(achievement));
    }

    private void OnGUI()
    {
        var verticalMargin = 10;

        var profileWindowHeight = Screen.height * 0.20f - verticalMargin * 1.3f;
        var selectorWindowHeight = Screen.height * 0.80f - verticalMargin * 1.3f;

        GUILayout.Window(App.WindowId + 100,
            new Rect(Screen.width / 2 - 150 + App.Offset, verticalMargin, 300, profileWindowHeight),
            OnPlayerInfoWindow, "Profile");

        GUILayout.Window(App.WindowId,
            new Rect(Screen.width / 2 - 150 + App.Offset, Screen.height - selectorWindowHeight - verticalMargin, 300,
                selectorWindowHeight),
            OnPickGameWindow, "Pick Game");
    }

    private void OnPickGameWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);

        GUILayout.Space(10);
        DisplayAchievements();

        GUILayout.EndScrollView();

        if (GUILayout.Button("REFRESH"))
            App.Bc.LeaderboardService.GetGlobalLeaderboardPage("Player_Rating",
                BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, 0, 10, OnReadAchievementData);

        if (GUILayout.Button("LOGOUT"))
            App.Bc.PlayerStateService.Logout((response, cbObject) => { OnGotoLoginScene();});

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
    }

    public void OnGotoLoginScene()
    {
        App.GotoLoginScene(gameObject);
    }

    private void DisplayAchievements()
    {
        foreach (var achievement in achievements)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            if (achievement.Status.Equals("NOT_AWARDED"))
                GUILayout.Label(string.Format("{0}", achievement.UnlockText));
            else
                GUILayout.Label(string.Format("[{0}]", achievement.UnlockedText), GUI.skin.button,
                    GUILayout.MinWidth(200));

            GUILayout.EndHorizontal();
        }
    }

    public class AchievementInfo
    {
        public string Status;
        public string UnlockedText;
        public string UnlockText;

        public AchievementInfo(JsonData jsonData)
        {
            UnlockText = jsonData["extraData"]["TODO"].ToString();
            UnlockedText = jsonData["extraData"]["DONE"].ToString();
            Status = jsonData["status"].ToString();
        }
    }
}