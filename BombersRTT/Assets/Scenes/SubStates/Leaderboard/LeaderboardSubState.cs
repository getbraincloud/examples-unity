using BrainCloudUNETExample.Connection;
using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace BrainCloudUNETExample
{
    public class LeaderboardSubState : BaseSubState
    {
        public static string STATE_NAME = "leaderboard";
        public const string JSON_KDR = "KDR";
        public const string JSON_BDR = "BDR";

        [SerializeField]
        private GameObject ScoreText = null;
        [SerializeField]
        private RectTransform LeaderboardScrollView = null;
        [SerializeField]
        private Image BombersButton = null;
        [SerializeField]
        private Image AcesButton = null;

        #region BaseState
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            m_leaderboardCell = new List<LeaderboardCell>();
            ShowKDRLeaderboard();
        }
        #endregion

        #region Public
        public void ShowKDRLeaderboard()
        {
            GetLeaderboard(JSON_KDR);
        }

        public void ShowBDRLeaderboard()
        {
            GetLeaderboard(JSON_BDR);
        }
        #endregion

        #region Private
        private void GetLeaderboard(string in_leaderboardID)
        {
            if (m_currentLeaderboardID != in_leaderboardID)
            {
                m_currentLeaderboardID = in_leaderboardID;
                SetTabColors();
                GStateManager.Instance.EnableLoadingSpinner(true);
                BrainCloudStats.Instance.GetLeaderboard(m_currentLeaderboardID, GetLeaderboardSuccess, GetLeaderboardFailure);
            }
        }

        private void GetLeaderboardSuccess(string in_json, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            OnRefreshLeaderboardView();
        }

        private void GetLeaderboardFailure(int status, int reasonCode, string jsonError, object cbObject)
        {
            Debug.LogError(jsonError);
            GStateManager.Instance.EnableLoadingSpinner(false);
        }

        private void OnRefreshLeaderboardView()
        {
            ScoreText.GetComponent<Text>().text = m_currentLeaderboardID == JSON_KDR ? "KILLS" : "TARGETS HIT";

            if (BrainCloudStats.Instance.m_leaderboardData != null)
            {
                RemoveAllItems();
                ParseLeaderboardData(BrainCloudStats.Instance.m_leaderboardData);
                PopulateLeaderboardScrollView(m_leaderboardItems, m_leaderboardCell, LeaderboardScrollView);
            }
        }

        private void RemoveAllItems()
        {
            m_leaderboardItems.Clear();
        }

        private void RemoveAllCellsInView(List<LeaderboardCell> in_leaderboardCell)
        {
            LeaderboardCell item;
            for (int i = 0; i < in_leaderboardCell.Count; ++i)
            {
                item = in_leaderboardCell[i];
                Destroy(item.gameObject);
            }
            in_leaderboardCell.Clear();
        }

        private LeaderboardCell CreateLeaderboardCell(Transform in_parent = null)
        {
            LeaderboardCell toReturn = null;
            toReturn = (GEntityFactory.Instance.CreateResourceAtPath("Prefabs/UI/leaderboardCell", in_parent.transform)).GetComponent<LeaderboardCell>();
            toReturn.transform.SetParent(in_parent);
            toReturn.transform.localScale = Vector3.one;
            return toReturn;
        }

        private void ParseLeaderboardData(Dictionary<String, object> in_leaderboardData)
        {
            object[] leaderboard = null;
            if (in_leaderboardData.ContainsKey(BrainCloudConsts.JSON_LEADERBOARD))
                leaderboard = ((object[])in_leaderboardData[BrainCloudConsts.JSON_LEADERBOARD]);

            for (int i = 0; i < leaderboard.Length; i++)
            {
                PlayerData pData = new PlayerData();
                pData.ReadLeaderboardPlayerData(leaderboard[i]);
                m_leaderboardItems.Add(pData);
            }
        }

        private void PopulateLeaderboardScrollView(List<PlayerData> in_leaderboardItems, List<LeaderboardCell> in_leaderboardCell, RectTransform in_scrollView)
        {
            RemoveAllCellsInView(in_leaderboardCell);
            if (in_leaderboardItems.Count == 0)
            {
                return;
            }

            if (in_scrollView != null)
            {
                List<PlayerData> activeListData = in_leaderboardItems;
                for (int i = 0; i < activeListData.Count; ++i)
                {
                    LeaderboardCell newItem = CreateLeaderboardCell(in_scrollView);
                    newItem.Init(activeListData[i]);
                    newItem.transform.localPosition = Vector3.zero;
                    in_leaderboardCell.Add(newItem);
                }
            }
        }

        private void SetTabColors()
        {
            if (m_currentLeaderboardID.Contains(JSON_KDR))
            {
                AcesButton.color = Color.white;
                BombersButton.color = notSelected;
            }
            else
            {
                AcesButton.color = notSelected;
                BombersButton.color = Color.white;
            }
        }

        private Color notSelected = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        private string m_currentLeaderboardID = "";
        private List<LeaderboardCell> m_leaderboardCell = null;
        private List<PlayerData> m_leaderboardItems = new List<PlayerData>();
        #endregion
    }
}
