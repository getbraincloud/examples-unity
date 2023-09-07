using System.Collections.Generic;
using UnityEngine;
using Gameframework;
using TMPro;
using System;

namespace BrainCloudUNETExample
{
    public class ChatCell : ImageDownloader
    {
        public static string SYSTEM_MESSAGE = "SYSTEM_MESSAGE";
        public const string DEFAULT_NAME = "DEFAULT NAME";
        #region public Properties

#pragma warning disable 649
        [SerializeField]
        private TextMeshProUGUI NameDisplay;
        [SerializeField]
        private TextMeshProUGUI TimeDisplay;
        [SerializeField]
        private PlayerRankIcon PlayerRankIcon = null;
#pragma warning restore 649

        public TextMeshProUGUI Message;
        public double MessageId { get; private set; }
        public int Version { get; private set; }

        public string ProfileId { get; private set; }
        public string LastConnectionId { get; private set; }

        public bool InLobbyView { get; private set; }
        public Dictionary<string, object> RawJson { get; private set; }

        public bool IsYou { get { return GCore.Wrapper.Client.ProfileId == ProfileId; } }
        #endregion

        public void Init(string in_userName, string in_message, string in_profileId, string in_imageURL,
                            string in_lastConnectionId, int in_playerRank, ulong in_messageId = 0, int in_version = 0, 
                            Dictionary<string, object> in_rawJson = null, bool in_lobbyView = false)
        {
            ProfileId = in_profileId;
            Message.text = in_message;

            LastConnectionId = in_lastConnectionId;
            MessageId = in_messageId;
            Version = in_version;
            RawJson = in_rawJson;

            InLobbyView = in_lobbyView;

            m_originalUserName = in_userName;
            if (NameDisplay != null)
                NameDisplay.text = in_userName == "" && !IsYou ? DEFAULT_NAME : IsYou ? "YOU" : GetPrettyUserNameDisplay(in_userName, in_profileId);

            UpdateTimeDisplay();

            if (PlayerRankIcon && in_userName != SYSTEM_MESSAGE)
                PlayerRankIcon.UpdateIcon(in_playerRank);
            else if (PlayerRankIcon)
                PlayerRankIcon.gameObject.SetActive(false);
        }

        private string GetPrettyUserNameDisplay(string in_userName, string in_profileId)
        {
            string toReturn = "";
            if (in_userName != SYSTEM_MESSAGE)
            {
                toReturn += in_userName;
                if (GFriendsManager.Instance.IsProfileIdInFriendsList(in_profileId))
                    toReturn += " <sprite name=\"Friend\">";
            }
            else
            {
                toReturn += "System Message";
            }
            return toReturn;
        }

        private void UpdateTimeDisplay()
        {
            if (TimeDisplay != null)
            {
                TimeDisplay.gameObject.SetActive(false);
                Dictionary<string, object> json = RawJson.ContainsKey("date") ? RawJson : (Dictionary<string, object>)RawJson["data"];

                if (json.ContainsKey("date"))
                {
                    ulong timeDisplay = Convert.ToUInt64(json["date"]);
                    TimeDisplay.text = GetDisplayTime(timeDisplay);
                    TimeDisplay.gameObject.SetActive(true);
                }

                // update every second
                if (TimeDisplay.text.Contains("s"))
                {
                    Invoke("UpdateTimeDisplay", 0.5f);
                }
                // update in a min, if left idle
                else if (TimeDisplay.text.Contains("m"))
                {
                    Invoke("UpdateTimeDisplay", 1.0f * 60.0f);
                }
                // update in an hr, if left idle
                else if (TimeDisplay.text.Contains("h"))
                {
                    Invoke("UpdateTimeDisplay", 1.0f * 3600.0f);
                }
                // not going to update these!
            }
        }

        private string GetDisplayTime(ulong in_date)
        {
            ulong delta = in_date < GPlayerMgr.Instance.CurrentServerTime ? GPlayerMgr.Instance.CurrentServerTime - in_date : 0;
            return HudHelper.ConvertUnixTimeToGUIString((uint)delta, false, 1);
        }

        private string m_originalUserName = "";
    }
}
