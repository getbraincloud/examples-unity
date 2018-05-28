#region

using System.Collections.Generic;
using BrainCloud;
using LitJson;
using UnityEngine;

#endregion

// Achievements are set on the brainCloud Dashboard, under Design | Gamification | Achievements

public class Achievements : GameScene
{
    private readonly List<AchievementInfo> achievements = new List<AchievementInfo>();
    private Vector2 _scrollPos;
    private string editablePlayerName = "";

    private bool isEditingPlayerName;

    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;


        App.Bc.GamificationService.ReadAchievements(true, OnReadAchievementData);
    }

    private void OnReadAchievementData(string responseData, object cbPostObject)
    {
        achievements.Clear();

        // Construct our matched players list using response data
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

    private void OnPlayerInfoWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();


        GUILayout.BeginHorizontal();

        if (!isEditingPlayerName)
        {
            GUILayout.Label(string.Format("PlayerName: {0}", App.PlayerName), GUILayout.MinWidth(200));
            if (GUILayout.Button("Edit", GUILayout.MinWidth(50)))
            {
                editablePlayerName = App.PlayerName;
                isEditingPlayerName = true;
            }
        }
        else
        {
            editablePlayerName = GUILayout.TextField(editablePlayerName, GUILayout.MinWidth(200));
            if (GUILayout.Button("Save", GUILayout.MinWidth(50)))
            {
                App.PlayerName = editablePlayerName;
                isEditingPlayerName = false;

                App.Bc.PlayerStateService.UpdateUserName(App.PlayerName,
                    (response, cbObject) => { },
                    (status, code, error, cbObject) => { Debug.Log("Failed to change Player Name"); });
            }
        }


        GUILayout.EndHorizontal();

        GUILayout.Label(string.Format("PlayerRating: {0}", App.PlayerRating), GUILayout.MinWidth(200));

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("MatchSelect", GUILayout.MinWidth(50))) App.GotoMatchSelectScene(gameObject);

        GUILayout.EndHorizontal();


        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();


        GUILayout.EndHorizontal();
    }

    private void OnPickGameWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);


        GUILayout.Space(10);
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

        GUILayout.EndScrollView();


        if (GUILayout.Button("REFRESH"))
            App.Bc.LeaderboardService.GetGlobalLeaderboardPage("Player_Rating",
                BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, 0, 10, OnReadAchievementData);

        if (GUILayout.Button("LOGOUT"))
            App.Bc.PlayerStateService.Logout((response, cbObject) => { App.GotoLoginScene(gameObject); });

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();


        GUILayout.EndHorizontal();
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