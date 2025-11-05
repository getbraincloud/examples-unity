using UnityEngine;
using UnityEngine.UI;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;

public class FindingRoomUI : MonoBehaviour
{
    // Reference to the text UI displaying elapsed time
    public Text timeSearchingText;
    public UIScreen roomScreen;
    public UIScreen kartSelection;

    private float elapsedTime = 0f;
    private int lastDisplayedSeconds = 0;

    public void QuickFindLobby(GameLauncher launcher)
    {
        BCManager.LobbyManager.QuickFindLobby(launcher);
    }
    public void FindLobby(GameLauncher launcher)
    {
        BCManager.LobbyManager.FindLobby(launcher);
    }
    public void CancelFind()
    {
        BCManager.LobbyManager.CancelFind();
    }

    void OnEnable()
    {
        ResetTimer();
        BCManager.LobbyManager.OnLobbyEventReceived += HandleLobbyEvent;
    }

    void OnDisable()
    {

        BCManager.LobbyManager.OnLobbyEventReceived -= HandleLobbyEvent;
    }

    private void HandleLobbyEvent(string jsonMessage)
    {
        // Parse top-level dictionary
        var message = JsonReader.Deserialize<Dictionary<string, object>>(jsonMessage);

        // Extract the service and operation type
        string service = message.ContainsKey("service") ? message["service"] as string : null;
        string operation = message.ContainsKey("operation") ? message["operation"] as string : null;

        if (service != "lobby" || string.IsNullOrEmpty(operation))
            return;

        // Extract data payload
        if (!message.TryGetValue("data", out object dataObj))
            return;

        var data = dataObj as Dictionary<string, object>;
        switch (operation)
        {
            case "MEMBER_JOIN":
                {
                    if (data.TryGetValue("member", out object memberObj))
                    {
                        var member = memberObj as Dictionary<string, object>;
                        if (member != null && member.TryGetValue("profileId", out object profileIdObj))
                        {
                            string joinedProfileId = profileIdObj as string;

                            // Compare with our own player’s ID
                            if (!string.IsNullOrEmpty(joinedProfileId) && joinedProfileId == ClientInfo.LoginData.profileId)
                            {
                                timeSearchingText.text = "Joined lobby!";

                                // Focus room and kart selection
                                UIScreen.Focus(roomScreen);
                                UIScreen.Focus(kartSelection);
                            }
                            else
                            {
                                Debug.Log($"Other player joined lobby: {joinedProfileId}");
                            }
                        }
                    }
                }
                break;

            case "JOIN_FAIL":
                if (data.TryGetValue("reason", out object reasonObj))
                {
                    var reason = reasonObj as Dictionary<string, object>;
                    string desc = reason != null && reason.TryGetValue("desc", out object descObj) ? descObj as string : "Unknown reason";
                    int code = reason != null && reason.TryGetValue("code", out object codeObj) ? Convert.ToInt32(codeObj) : 0;

                    // quick find will create one as the last step
                    //if (!BCManager.LobbyManager.IsQuickFind &&
                    //    data.TryGetValue("lobbyType", out object lobbyType) && lobbyType as string == BCManager.LobbyManager.GetLobbyString(1))
                    {
                        timeSearchingText.text = $"Join failed: {desc}";
                        enableTimer = false;
                    }
                }
                break;

            default:
                Debug.Log("Unhandled lobby operation: " + operation);
                break;
        }
    }
    
    void Update()
    {
        if (enableTimer)
        {
            // Accumulate elapsed time
            elapsedTime += Time.deltaTime;

            // Convert to integer seconds
            int seconds = Mathf.FloorToInt(elapsedTime);

            // Only update the UI when a new second passes
            if (seconds != lastDisplayedSeconds)
            {
                lastDisplayedSeconds = seconds;
                timeSearchingText.text = seconds.ToString();
            }
        }
        
    }

    private bool enableTimer = true;
    private void ResetTimer()
    {
        enableTimer = true;
        lastDisplayedSeconds = 0;
        elapsedTime = 0f;
        if (timeSearchingText != null)
            timeSearchingText.text = "0";
    }
}