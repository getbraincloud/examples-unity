using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Leaderbaords are set on the brainCloud Dashboard, under Design | Leaderboard | Leaderboard Configs
/**
 {
    "configList": [
        {
            "gameId": "10228",
            "leaderboardId": "Player_Rating",
            "resetAt": 1526670180000,
            "leaderboardType": "LAST_VALUE",
            "rotationType": "NEVER",
            "retainedCount": 2,
            "pacerEnabled": true,
            "pacerConfig": [
                {
                    "score": 1400,
                    "pacerLeaderboardTag": {},
                    "pacerId": "1"
                },
                {
                    "score": 1000,
                    "pacerLeaderboardTag": {},
                    "pacerId": "2"
                },
                {
                    "score": 0,
                    "pacerLeaderboardTag": {},
                    "pacerId": "3"
                },
                {
                    "score": 1800,
                    "pacerLeaderboardTag": {},
                    "pacerId": "4"
                },
                {
                    "score": 9999,
                    "pacerLeaderboardTag": {},
                    "pacerId": "5"
                }
            ],
            "data": {},
            "numDaysToRotate": 0,
            "currentVersionId": 1,
            "retainedVersionsInfo": [
                {
                    "versionId": 1,
                    "startingAt": 1526666569839,
                    "endingAt": null,
                    "rotationType": "NEVER",
                    "numDaysToRotate": 0
                }
            ],
            "lastPurgedAt": 0
        }
    ],
    "maxRetainedCount": 8
}
 */

public class Leaderboard : ResourcesManager
{
    private readonly List<PlayerInfo> scores = new List<PlayerInfo>();
    private Vector2 _scrollPos;

    [SerializeField]
    private RectTransform LeaderboardScrollView = null;

    [SerializeField]
    private TextMeshProUGUI MyScores = null;

    private void Start()
    {
        App = MatchSelectObj.App;
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;

        // Get the Player_Rating leaderboard that would have have created on the brainCloud Dashboard
        App.Bc.LeaderboardService.GetGlobalLeaderboardPage("Player_Rating",
            BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, 0, 10, OnReadLeaderboardData);

        m_itemCell = new List<LeaderboardCell>();
    }

    public void OnReadLeaderboardData(string responseData, object cbPostObject)
    {
        scores.Clear();

        var leaderboardData = (JsonReader.Deserialize<Dictionary<string, object>>(responseData)
                                ["data"] as Dictionary<string, object>)
                                ["leaderboard"] as Dictionary<string, object>[];

        foreach (Dictionary<string, object> score in leaderboardData)
        {
            scores.Add(new PlayerInfo(score));
        }
    }

    public void OnUpdateUI()
    {
        PopulateScrollView(scores, m_itemCell, LeaderboardScrollView);
    }

    public void OnGotoLoginScene()
    {
        App.GotoLoginScene(gameObject);
    }

    private void PopulateScrollView(List<PlayerInfo> in_itemItems, List<LeaderboardCell> in_itemCell, RectTransform in_scrollView)
    {
        RemoveAllCellsInView(in_itemCell);
        if (in_itemItems.Count == 0)
        {
            return;
        }

        if (in_scrollView != null)
        {
            int i = 0;

            foreach (var leaderboardItem in in_itemItems)
            {
                LeaderboardCell newItem = CreateLeaderboardCell(in_scrollView, (i % 2) == 0);

                newItem.Init(leaderboardItem, i + 1);
                newItem.transform.localPosition = Vector3.zero;
                in_itemCell.Add(newItem);
                i++;
            }
        }
    }

    private static Color OPP_COLOR = new Color32(0xFF, 0xFF, 0x49, 0xFF);

    private LeaderboardCell CreateLeaderboardCell(Transform in_parent = null, bool in_even = false)
    {
        bool isSecondDisplay = MyScores.color == OPP_COLOR ? true : false;
        LeaderboardCell toReturn = null;
        toReturn = (CreateResourceAtPath(in_even ? "Prefabs/LeaderboardCell" + (isSecondDisplay ? "2" : "1") + "A" : "Prefabs/LeaderboardCell" + (isSecondDisplay ? "2" : "1") + "B", in_parent.transform)).GetComponent<LeaderboardCell>();

        toReturn.transform.SetParent(in_parent);
        toReturn.transform.localScale = Vector3.one;
        return toReturn;
    }

    private void RemoveAllCellsInView(List<LeaderboardCell> in_itemCell)
    {
        LeaderboardCell item;
        for (int i = 0; i < in_itemCell.Count; ++i)
        {
            item = in_itemCell[i];
            Destroy(item.gameObject);
        }
        in_itemCell.Clear();
    }

    private List<LeaderboardCell> m_itemCell = null;
}