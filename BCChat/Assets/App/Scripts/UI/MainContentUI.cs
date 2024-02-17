using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainContentUI : ContentUIBehaviour
{
    [Header("Channel Content")]
    [SerializeField] private ScrollRect ChannelScroll = default;
    [SerializeField] private RectTransform ChannelContent = default;
    [SerializeField] private Button ShowLogButton = default;
    [SerializeField] private Button LogoutButton = default;
    [SerializeField] private Button SettingsButton = default;

    [Header("Chat Content")]
    [SerializeField] private int MaxChatMessages = 30;
    [SerializeField] private ScrollRect ChatScroll = default;
    [SerializeField] private RectTransform ChatContent = default;
    [SerializeField] private TMP_InputField ChatField = default;
    [SerializeField] private Button SendButton = default;

    [Header("Navigation")]
    [SerializeField] private LoginContentUI LoginContent = default;
    [SerializeField] private UserContentUI UserContent = default;
    [SerializeField] private LogContentUI LogContent = default;

    [Header("Templates")]
    [SerializeField] private ChannelButton ChannelButtonTemplate = default;
    [SerializeField] private ChatMessage ChatMessageTemplate = default;
    [SerializeField] private GameObject MessageSpacer = default;

    private Message messageToEdit;
    private ChannelInfo currentChannelInfo;
    private BrainCloudRTT rttService = null;
    private BrainCloudChat chatService = null;
    private List<ChannelButton> channelButtons = null;
    private List<ChatMessage> channelChatMessages = null;

    #region Unity Messages

    protected override void Awake()
    {
        ChatField.text = string.Empty;
        ChatField.interactable = false;
        SendButton.interactable = false;

        base.Awake();
    }

    private void OnEnable()
    {
        ShowLogButton.onClick.AddListener(OnShowLogButton);
        LogoutButton.onClick.AddListener(OnLogoutButton);
        SettingsButton.onClick.AddListener(OnSettingsButton);
        ChatField.onValueChanged.AddListener(OnChatValidation);
        SendButton.onClick.AddListener(OnSendButton);
    }

    protected override void Start()
    {
        rttService = BCManager.RTTService;
        chatService = BCManager.ChatService;

        channelButtons = new();
        channelChatMessages = new();

        base.Start();

        IsInteractable = false;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        ShowLogButton.onClick.RemoveAllListeners();
        LogoutButton.onClick.RemoveAllListeners();
        SettingsButton.onClick.RemoveAllListeners();
        ChatField.onValueChanged.RemoveAllListeners();
        SendButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        if (rttService.IsRTTEnabled())
        {
            rttService.DisableRTT();
            rttService.DeregisterRTTChatCallback();
        }

        if (!currentChannelInfo.id.IsEmpty())
        {
            chatService.ChannelDisconnect(currentChannelInfo.id);
        }

        rttService = null;
        chatService = null;

        if (channelButtons != null)
        {
            for (int i = 0; i < channelButtons.Count; i++)
            {
                Destroy(channelButtons[i]);
            }

            channelButtons.Clear();
            channelButtons = null;
        }

        if (channelChatMessages != null)
        {
            for (int i = 0; i < channelChatMessages.Count; i++)
            {
                Destroy(channelChatMessages[i]);
            }

            channelChatMessages.Clear();
            channelChatMessages = null;
        }

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        IsInteractable = false;

        ChatField.placeholder.GetComponent<TMP_Text>().text = string.Empty;

        string[] channelIds = new string[] // Need to get this from a global entity
        {
            "general",
            "gaming",
            "bitheads",
            "braincloud"
        };

        if (channelButtons.Count > 0)
        {
            for (int i = 0; i < channelButtons.Count; i++)
            {
                Destroy(channelButtons[i].gameObject);
            }

            channelButtons.Clear();
        }

        int channelCount = 0;
        void HandleGetChannelsFinished()
        {
            if(++channelCount >= channelIds.Length)
            {
                if (channelButtons.Count <= 0)
                {
                    IsInteractable = true;
                    Debug.LogWarning("No chat channels found.");

                    return;
                }

                const string defaultChannel = "general";

                int activeChannel = 0;
                for (int i = 0; i < channelButtons.Count; i++)
                {
                    if (channelButtons[i].ChannelInfo.name == defaultChannel)
                    {
                        activeChannel = i;
                        break;
                    }
                }

                channelButtons[activeChannel].ButtonAction?.Invoke(channelButtons[activeChannel].ChannelInfo);
            }
        }

        void HandleGetChannelsFailure(int status, int reasonCode, string jsonError, object cbObject)
        {
            HandleGetChannelsFinished();
            Debug.LogError($"Could not get Channel ID for: {cbObject}");
        }

        void HandleGetChannelInfo(string jsonResponse, object cbObject)
        {
            var info = jsonResponse.Deserialize<ChannelInfo>("data");

            var channelToAdd = Instantiate(ChannelButtonTemplate, ChannelContent, false);
            channelToAdd.SetChannelInfo(info, SetCurrentChatChannel);
            channelButtons.Add(channelToAdd);

            HandleGetChannelsFinished();
            Debug.Log($"Added Channel: {info.name}");
        }

        void HandleGetChannelIDSuccess(string jsonResponse, object cbObject)
        {
            chatService.GetChannelInfo(jsonResponse.Deserialize("data").GetString("channelId"),
                                       HandleGetChannelInfo,
                                       HandleGetChannelsFailure);
        }

        for (int i = 0; i < channelIds.Length; i++)
        {
            chatService.GetChannelId("gl",
                                     channelIds[i],
                                     HandleGetChannelIDSuccess,
                                     HandleGetChannelsFailure);
        }
    }

    private void SetCurrentChatChannel(ChannelInfo info)
    {
        // Check to make sure channel isn't already open
        for (int i = 0; i < channelButtons.Count; i++)
        {
            if (channelButtons[i].IsActiveChannel &&
                channelButtons[i].ChannelInfo.id == info.id)
            {
                return;
            }
        }

        IsInteractable = false;
        ChatField.text = string.Empty;

        for (int i = 0; i < channelButtons.Count; i++)
        {
            if (channelButtons[i].ChannelInfo.id == info.id)
            {
                channelButtons[i].IsActiveChannel = true;
            }
            else
            {
                channelButtons[i].IsActiveChannel = false;
            }
        }

        // Reset Chat Messages
        for (int i = 0; i < ChatContent.childCount; i++)
        {
            GameObject child = ChatContent.GetChild(i).gameObject;
            if (child.GetComponent<ChatMessage>() is ChatMessage chatMessage &&
                channelChatMessages.Contains(chatMessage))
            {
                channelChatMessages[channelChatMessages.IndexOf(chatMessage)] = null;
            }

            Destroy(ChatContent.GetChild(i).gameObject);
        }

        channelChatMessages.Clear();

        void HandleGetRecentChatMessages(string jsonResponse, object cbObject)
        {
            var messages = jsonResponse.Deserialize("data").GetJSONArray<Message>("messages");

            Message previous;
            previous.from.id = string.Empty;
            previous.date = DateTime.UnixEpoch;
            foreach(Message message in messages)
            {
                var chatMessageToAdd = Instantiate(ChatMessageTemplate, ChatContent, false);
                chatMessageToAdd.SetChatContents(message);
                chatMessageToAdd.DeleteAction = DeleteChatMessage;
                chatMessageToAdd.EditAction = EditChatMessage;

                if (previous.from.id == message.from.id &&
                    (message.date - previous.date).TotalMinutes < 1.0)
                {
                    chatMessageToAdd.DisplayHeader(false);
                }

                if (!previous.from.id.IsEmpty() &&
                    (previous.from.id != message.from.id ||
                    (message.date - previous.date).TotalMinutes >= 1.0))
                {
                    Instantiate(MessageSpacer, ChatContent, false);
                    chatMessageToAdd.transform.SetAsLastSibling();
                }

                previous = message;
                channelChatMessages.Add(chatMessageToAdd);
            }

            //if (!rttService.IsRTTEnabled())
            //{
            //    rttService.EnableRTT(RTTConnectionType.WEBSOCKET);
            //    rttService.RegisterRTTChatCallback(RTTCallback);
            //}

            ChatField.placeholder.GetComponent<TMP_Text>().text = $"Message #{info.name}";
            ChatField.interactable = true;

            StartCoroutine(ScrollChatToBottom());
            chatService.PostChatMessageSimple(info.id, "<i>User has entered chat.</i>", false); // RTT will need to get this...

            currentChannelInfo = info;
            IsInteractable = true;
        }

        if (!currentChannelInfo.id.IsEmpty())
        {
            chatService.ChannelDisconnect(currentChannelInfo.id,
                                          (_, _) =>
                                          {
                                              chatService.ChannelConnect(info.id, MaxChatMessages, HandleGetRecentChatMessages, HandleFailures);
                                          },
                                          HandleFailures);
        }
        else
        {
            chatService.ChannelConnect(info.id, MaxChatMessages, HandleGetRecentChatMessages, HandleFailures);
        }
    }

    private void DeleteChatMessage(Message message)
    {
        // Need to handle chat deletion via RTT

        chatService.DeleteChatMessage(message.chId,
                                      message.msgId,
                                      -1,
                                      null,
                                      HandleFailures);

        ClearMessageToEdit();
    }

    private void EditChatMessage(Message message)
    {
        // Need to handle chat edit via RTT

        messageToEdit = message;
        ChatField.text = message.content.text;

        ChatField.Select();
        ChatField.ActivateInputField();
    }

    private void RTTCallback(string responseData)
    {
        Debug.Log(responseData);
    }

    private void OnShowLogButton()
    {
        IsInteractable = false;
        gameObject.SetActive(false);

        LogContent.ShowLog();
    }

    private void OnLogoutButton()
    {
        IsInteractable = false;

        UserHandler.HandleUserLogout(HandleUserLogoutSuccess,
                                     HandleFailures);
    }

    private void OnSettingsButton()
    {
        UserContent.ResetUI();
        UserContent.gameObject.SetActive(true);

        IsInteractable = false;
        gameObject.SetActive(false);
    }

    private void OnChatValidation(string value)
    {
        SendButton.interactable = !value.IsEmpty();

        if (value.IsEmpty())
        {
            ClearMessageToEdit();
        }
    }

    private void OnSendButton()
    {
        var contentJson = new Dictionary<string, object>()
        {
            {"rich", null },
            {"text", ChatField.text }
        };

        if (!messageToEdit.msgId.IsEmpty())
        {
            chatService.UpdateChatMessage(messageToEdit.chId,
                                          messageToEdit.msgId,
                                          messageToEdit.ver,
                                          contentJson.Serialize(),
                                          failure:HandleFailures);
        }
        else
        {
            chatService.PostChatMessage(currentChannelInfo.id,
                                        contentJson.Serialize(),
                                        true,
                                        failure:HandleFailures);
        }

        ChatField.text = string.Empty;
        ClearMessageToEdit();
    }

    private void ClearMessageToEdit()
    {
        messageToEdit.ver = 0;
        messageToEdit.chId = string.Empty;
        messageToEdit.msgId = string.Empty;
    }

    private IEnumerator ScrollChatToBottom()
    {
        yield return null;

        ChatScroll.verticalNormalizedPosition = 0.0f;
    }

    #endregion

    #region brainCloud

    private void HandleUserLogoutSuccess(string jsonResponse, object cbObject)
    {
        LoginContent.SetRememberMePref(false);
        LoginContent.IsInteractable = true;
        LoginContent.gameObject.SetActive(true);

        gameObject.SetActive(false);

        Debug.Log("User has logged out.");
    }

    private void HandleFailures(int status, int reasonCode, string jsonError, object cbObject)
    {
        IsInteractable = true;

        Debug.LogError("An error occurred. Please try again.");
    }

    #endregion
}
