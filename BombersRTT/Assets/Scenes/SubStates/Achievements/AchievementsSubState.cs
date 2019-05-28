using BrainCloudUNETExample.Connection;
using Gameframework;
using UnityEngine;
using System.Collections.Generic;

namespace BrainCloudUNETExample
{
    public class AchievementsSubState : BaseSubState
    {
        public static string STATE_NAME = "achievements";

        [SerializeField]
        private RectTransform AchievementsScrollView = null;

        #region BaseState
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            m_achievementCell = new List<AchievementCell>();
            m_achievementsItems = BrainCloudStats.Instance.m_achievements;

            PopulateAchievementScrollView(m_achievementsItems, m_achievementCell, AchievementsScrollView);
        }
        #endregion

        #region Private
        private void RemoveAllCellsInView(List<AchievementCell> in_leaderboardListItem)
        {
            AchievementCell item;
            for (int i = 0; i < in_leaderboardListItem.Count; ++i)
            {
                item = in_leaderboardListItem[i];
                Destroy(item.gameObject);
            }
            in_leaderboardListItem.Clear();
        }

        private AchievementCell CreateAchievementCell(Transform in_parent = null)
        {
            AchievementCell toReturn = null;
            toReturn = (GEntityFactory.Instance.CreateResourceAtPath("Prefabs/UI/achievementCell", in_parent.transform)).GetComponent<AchievementCell>();
            toReturn.transform.SetParent(in_parent);
            toReturn.transform.localScale = Vector3.one;
            return toReturn;
        }

        private void PopulateAchievementScrollView(List<AchievementData> in_achievementItems, List<AchievementCell> in_achievementCell, RectTransform in_scrollView)
        {
            RemoveAllCellsInView(in_achievementCell);
            if (in_achievementItems.Count == 0)
            {
                return;
            }

            if (in_scrollView != null)
            {
                List<AchievementData> activeListData = in_achievementItems;
                for (int i = 0; i < activeListData.Count; ++i)
                {
                    AchievementCell newItem = CreateAchievementCell(in_scrollView);
                    newItem.Init(activeListData[i]);
                    newItem.transform.localPosition = Vector3.zero;
                    in_achievementCell.Add(newItem);
                }
            }
        }

        private List<AchievementCell> m_achievementCell = null;
        private List<AchievementData> m_achievementsItems = null;
        #endregion
    }
}
