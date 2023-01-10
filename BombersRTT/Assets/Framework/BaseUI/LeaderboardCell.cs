using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace BrainCloudUNETExample
{
    public class LeaderboardCell : ImageDownloader
    {
        public const string DEFAULT_NAME = "DEFAULT NAME";

        #region private Properties
        [SerializeField]
        private TextMeshProUGUI Pos = null;
        [SerializeField]
        private TextMeshProUGUI UserName = null;
        [SerializeField]
        private TextMeshProUGUI Rank = null;
        [SerializeField]
        private TextMeshProUGUI Kills = null;
        [SerializeField]
        private PlayerRankIcon PlayerRankIcon = null;
        #endregion

        public string ProfileId { get; private set; }

        #region public
        public virtual void Init(PlayerData in_pData)
        {
            m_pData = in_pData;
            UpdateUI();
        }

        public virtual void UpdateUI()
        {
            if (m_pData != null)
            {
                ProfileId = m_pData.ProfileId;
                Pos.text = m_pData.PlayerRank.ToString();
                UserName.text = m_pData.PlayerName == "" ? DEFAULT_NAME : m_pData.PlayerName;

                Dictionary<string, object> extraData = m_pData.GetCurrentLeaderboardExtraData();
                if (extraData != null && extraData.ContainsKey(BrainCloudConsts.JSON_RANK) && extraData.ContainsKey(BrainCloudConsts.JSON_LEVEL))
                    Rank.text = extraData[BrainCloudConsts.JSON_RANK] as string + " " + "(" + extraData[BrainCloudConsts.JSON_LEVEL] as string + ")";
                //TODO: Remove the Rank's value "(3)" before shipping.

                Kills.text = HudHelper.ToGUIString((Mathf.Floor(float.Parse(m_pData.PlayerRating.ToString()) / 10000) + 1));

                Color color = IsYou ? Color.white : notYou;
                Pos.color = color;
                UserName.color = color;
                Rank.color = color;
                Kills.color = color;

                PlayerRankIcon.UpdateIcon(int.Parse(extraData[BrainCloudConsts.JSON_LEVEL] as string));
            }
        }
        #endregion

        #region private
        private bool IsYou { get { return GCore.Wrapper.Client.ProfileId == ProfileId; } }
        private Color notYou = new Color(0.275f, 0.5f, 0.56f, 1.0f);
        private PlayerData m_pData = null;
        #endregion
    }
}
