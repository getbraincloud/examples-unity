using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JSONHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// <para>
/// This showcases the use of the Chat service on brainCloud
/// and utilizes RTT for a real-time chat application.
/// </para>
/// 
/// <seealso cref="BrainCloudChat"/>
/// <seealso cref="BrainCloudRTT"/>
/// </summary>
/// API Link: https://docs.braincloudservers.com/api/capi/chat
public class MainContentUI : ContentUIBehaviour
{
    private const string USER_ENTERED_MESSAGE = "<i><USER> has entered chat.</i>";

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

    [Header("Modal Prefab")]
    [SerializeField] private ModalUI ModalUIContent = default;

    private Message messageToEdit;
    private ChannelInfo currentChannelInfo, reconnectChannelInfo;
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

        rttService.RegisterRTTChatCallback(RTTChatCallback);
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
        rttService.DeregisterRTTChatCallback();

        if (rttService.IsRTTEnabled())
        {
            rttService.DisableRTT();
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
        StartCoroutine(HandleRTTAndSetup());
    }

    private void DeleteChatMessage(Message message)
    {
        chatService.DeleteChatMessage(message.chId,
                                      message.msgId,
                                      -1,
                                      failure:HandleFailures);

        ClearMessageToEdit();
    }

    private void EditChatMessage(Message message)
    {
        messageToEdit = message;
        ChatField.text = message.content.text;

        ChatField.Select();
        ChatField.ActivateInputField();
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

        if (!messageToEdit.msgId.IsEmpty() &&
            messageToEdit.chId == currentChannelInfo.id)
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

    private void SpawnModal(string title, string text, string yesText, string noText = null,
        Action onYesClicked = null, Action onNoClicked = null)
    {
        IsInteractable = true;
        ModalUI modal = Instantiate(ModalUIContent, this.transform);
        modal.InitModal(title, text, yesText, noText, onYesClicked, onNoClicked);
    }
    #endregion

    #region brainCloud
    /*
     * Debug function to test the RTT disconnected modal and reconnect feature
     * 
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            rttService.DisableRTT();
            BCManager.Wrapper.LogoutOnApplicationQuit(false);

            SpawnModal("ERROR", "You have been disconnected from RTT. Please reconnect to use BC Chat", "Reconnect", "Cancel",
                    OnReconnectClicked, OnReconnectCancelClicked);
        }
    }
    */

    private void OnReconnectClicked()
    {
        //re authenticate then re-initialize this UI view
        reconnectChannelInfo = currentChannelInfo;
        IsInteractable = false;
        FailureCallback onFailure = OnFailure("Automatic Login Failed", () =>
        {
            IsInteractable = true;
            SpawnModal("ERROR", "Could not re-authenticate", "Ok", null, () =>
            {
                //go back to login screen
                OnReconnectCancelClicked();
            });
            
        });

        void OnAuthenticationSuccess()
        {
            InitializeUI();
        }

        UserHandler.HandleUserReconnect(OnSuccess("Automatically Logging In...", OnAuthenticationSuccess), onFailure); 
    }

    private void OnReconnectCancelClicked()
    {
        //go back to login screen
        LoginContent.IsInteractable = true;
        LoginContent.gameObject.SetActive(true);

        gameObject.SetActive(false);
    }

    // Enable RTT and get the chat channels and subscribe to the first channel
    private IEnumerator HandleRTTAndSetup()
    {
        IsInteractable = false;

        ChatField.placeholder.GetComponent<TMP_Text>().text = string.Empty;

        yield return null;

        // Clear current channels
        if (channelButtons.Count > 0)
        {
            for (int i = 0; i < channelButtons.Count; i++)
            {
                Destroy(channelButtons[i].gameObject);
            }

            channelButtons.Clear();
        }

        // Enable RTT if it isn't
        if (!rttService.IsRTTEnabled())
        {
            bool isSuccess = false;
            void HandleRTTEnableSuccess(string jsonResponse, object cbObject)
            {
                isSuccess = true;
            }

            void HandleRTTEnableFailure(int status, int reasonCode, string jsonError, object cbObject)
            {
                Debug.LogError("Unable to enable RTT! Cannot use BC Chat.");

                SpawnModal("ERROR", "You were disconnected from RTT:" + reasonCode + ": " + jsonError, "Reconnect", "Cancel",
                        OnReconnectClicked, OnReconnectCancelClicked);

                StopCoroutine(HandleRTTAndSetup());
            }

            rttService.EnableRTT(HandleRTTEnableSuccess, HandleRTTEnableFailure);

            yield return new WaitUntil(() => isSuccess);
        }

        yield return null;

        // Get our Global channels
        bool getChannelsFinished = false;
        void HandleGetSubscribedChannels(string jsonResponse, object cbObject)
        {
            var channels = jsonResponse.Deserialize("data").GetJSONArray<ChannelInfo>("channels");

            foreach (var info in channels)
            {
                var channelToAdd = Instantiate(ChannelButtonTemplate, ChannelContent, false);
                channelToAdd.SetChannelInfo(info, SetCurrentChatChannel);
                channelButtons.Add(channelToAdd);

                Debug.Log($"Added Channel: {info.name}");
            }

            getChannelsFinished = true;
        }

        chatService.GetSubscribedChannels("gl",
                                          HandleGetSubscribedChannels,
                                          OnFailure("Cound not get subscribed channels!", () => getChannelsFinished = true));

        yield return new WaitUntil(() => getChannelsFinished);

        if (channelButtons.Count <= 0)
        {
            IsInteractable = true;
            Debug.LogWarning("No chat channels found.");

            yield break;
        }

        ChannelScroll.verticalNormalizedPosition = 0.0f;

        if(!string.IsNullOrEmpty(reconnectChannelInfo.code))
        {
            SetCurrentChatChannel(reconnectChannelInfo); // Open channel we want to reconnect to
        }
        else
        {
            SetCurrentChatChannel(channelButtons[0].ChannelInfo); // Open first chat room
        }
    }

    // This deserialized our RTT chat responses; this callback is integral to using the Chat service
    private void RTTChatCallback(string responseData)
    {
        const string SERVICE = "chat",
                     OPERATION_INCOMING = "INCOMING",
                     OPERATION_UPDATE = "UPDATE",
                     OPERATION_DELETE = "DELETE";

        var data = responseData.Deserialize();
        if (data.GetString("service") == SERVICE)
        {
            string op = data.GetString("operation");
            if (op == OPERATION_INCOMING)
            {
                Message incoming = data.GetJSONObject<Message>("data");
                if (incoming.chId != currentChannelInfo.id)
                {
                    return;
                }

                if (incoming.content.text == USER_ENTERED_MESSAGE)
                {
                    incoming.content.text = USER_ENTERED_MESSAGE.Replace("<USER>", incoming.from.name);
                    incoming.from.id = string.Empty;
                    incoming.date = DateTime.UnixEpoch;

                    AddChatMessages(new Message[] { incoming });

                    var chatMessage = channelChatMessages[^1];
                    chatMessage.DeleteAction = null;
                    chatMessage.EditAction = null;
                    chatMessage.DisplayHeader(false);
                    chatMessage.DisplayProfileImage(false);
                    chatMessage.DisplayFooter(false);
                }
                else
                {
                    AddChatMessages(new Message[] { incoming });
                }
            }
            else if (op == OPERATION_UPDATE)
            {
                Message updated = data.GetJSONObject<Message>("data");
                if (updated.chId != currentChannelInfo.id)
                {
                    return;
                }

                for (int i = 0; i < channelChatMessages.Count; i++)
                {
                    if (updated.msgId == channelChatMessages[i].Message.msgId)
                    {
                        channelChatMessages[i].SetChatContents(updated);
                        break;
                    }
                }
            }
            else if (op == OPERATION_DELETE)
            {
                data = data.GetJSONObject("data");
                if (data.GetString("chId") != currentChannelInfo.id)
                {
                    return;
                }

                string msgId = data.GetString("msgId");
                for (int i = 0; i < channelChatMessages.Count; i++)
                {
                    if (msgId == channelChatMessages[i].Message.msgId)
                    {
                        Destroy(channelChatMessages[i].gameObject);
                        channelChatMessages[i] = null;
                        channelChatMessages.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError($"Unknown chat operation: {op}");
            }

            StartCoroutine(ScrollChatToBottom());
        }
        else
        {
            Debug.LogError($"Unknown service: {data.GetString("service")}");
        }
    }

    // Adds chat messages to the chat scroll view
    private void AddChatMessages(Message[] messages)
    {
        Message previous;
        if (channelChatMessages.Count > 0)
        {
            previous = channelChatMessages[^1].Message;
        }
        else
        {
            previous.from.id = string.Empty;
            previous.date = DateTime.UnixEpoch;
        }

        foreach (Message message in messages)
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
    }

    // Handles channel connection and disconnections
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

        // Reset chat messages
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
            AddChatMessages(jsonResponse.Deserialize("data").GetJSONArray<Message>("messages"));

            chatService.PostChatMessageSimple(info.id, USER_ENTERED_MESSAGE, false);

            currentChannelInfo = info;
            ChatField.placeholder.GetComponent<TMP_Text>().text = $"Message #{info.name}";
            ChatField.interactable = true;

            IsInteractable = true;

            StartCoroutine(ScrollChatToBottom());
        }

        void HandleChatDisconnect(string jsonResponse, object cbObject)
        {
            chatService.ChannelConnect(cbObject.ToString(), MaxChatMessages, HandleGetRecentChatMessages, HandleFailures);
        }

        // Disconnect from current chat and connect to the new chat
        if (!currentChannelInfo.id.IsEmpty())
        {
            chatService.ChannelDisconnect(currentChannelInfo.id, HandleChatDisconnect, HandleFailures, info.id);
        }
        else
        {
            chatService.ChannelConnect(info.id, MaxChatMessages, HandleGetRecentChatMessages, HandleFailures);
        }
    }

    private void HandleUserLogoutSuccess(string jsonResponse, object cbObject)
    {
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
