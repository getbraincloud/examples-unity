using BrainCloudUNETExample.Connection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatCell : ImageDownloader
{
    public const string DEFAULT_NAME = "DEFAULT NAME";
    #region public Properties
    public GameObject NameDisplay;
    public GameObject NameDisplayYou;

    public GameObject Background;
    public GameObject BackgroundYou;
    public GameObject BackgroundAlternate;

    public Text Message;
    public Text UserName { get; private set; }

    public double MessageId { get; private set; }

    public string ProfileId { get; private set; }
    public string LastConnectionId { get; private set; }

    public bool IsYou { get { return BombersNetworkManager._BC.Client.ProfileId == ProfileId; } }
    #endregion

    public void Init(string in_userName, string in_message, string in_profileId, string in_imageURL, string in_lastConnectionId, ulong in_messageId = 0)
    {
        ProfileId = in_profileId;
        NameDisplay.SetActive(!IsYou);
        NameDisplayYou.SetActive(IsYou);

        Message.resizeTextMinSize = Message.fontSize;
        Message.resizeTextMaxSize = Message.fontSize;
        Message.text = in_message;

        LastConnectionId = in_lastConnectionId;
        MessageId = in_messageId;

        Canvas.ForceUpdateCanvases();

        UserName = NameDisplay.transform.GetChild(1).GetChild(0).GetComponent<Text>();
        if (IsYou)
        {
            this.RawImage = NameDisplayYou.transform.GetChild(0).GetComponent<RawImage>();
            UserName = NameDisplayYou.transform.GetChild(1).GetChild(0).GetComponent<Text>();
        }

        DownloadImage(in_imageURL);
        BackgroundYou.SetActive(IsYou);
        bool bDisplayBG = !IsYou && transform.GetSiblingIndex() % 2 == 0;
        BackgroundAlternate.SetActive(bDisplayBG);
        Background.SetActive(!bDisplayBG);

        UserName.text = in_userName == "" && !IsYou ? DEFAULT_NAME :
            IsYou ? "YOU" : in_userName;

        // change the size of the cell dynamically
        RectTransform rt = (RectTransform)transform;
        int lineHeightIncrement = 4;
        int lineHeighDelta = Message.cachedTextGenerator.lineCount == 1 ? Message.fontSize + (lineHeightIncrement + 2) : Message.fontSize + lineHeightIncrement;
        rt.sizeDelta = new Vector2(rt.rect.width, 30 + (lineHeighDelta * Message.cachedTextGenerator.lineCount));

        updateLobbyInviteDisplay();
    }
    
    private void updateLobbyInviteDisplay()
    {
        Transform tempTrans = NameDisplay.activeInHierarchy ? NameDisplay.transform.GetChild(NameDisplay.transform.childCount - 1) :
                                   NameDisplayYou.activeInHierarchy ? NameDisplayYou.transform.GetChild(NameDisplayYou.transform.childCount - 1) : null;
        if (tempTrans != null) m_inviteToLobbyObject = tempTrans.gameObject;
        if (m_inviteToLobbyObject != null) m_inviteToLobbyObject.SetActive(!IsYou && LastConnectionId != "");
    }

    public void OnInviteToLobby()
    {
        if (m_inviteToLobbyObject != null)
        {
            BombersNetworkManager.RefreshBCVariable();

            // send event to other person
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData["lastConnectionId"] = BombersNetworkManager._BC.Client.RTTConnectionID;
            jsonData["profileId"] = BombersNetworkManager._BC.Client.ProfileId;
            jsonData["userName"] = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName;

            BombersNetworkManager._BC.Client.EventService.SendEvent(ProfileId, "OFFER_JOIN_LOBBY", 
                BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonWriter.Serialize(jsonData));
        }
    }

    private GameObject m_inviteToLobbyObject;
}
