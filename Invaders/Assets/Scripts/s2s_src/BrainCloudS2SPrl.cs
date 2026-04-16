//----------------------------------------------------
// brainCloud client source code
// Copyright 2026 bitHeads, inc.
//----------------------------------------------------

using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;

/// <summary>
/// Handles the Pre-Ready Launch (PRL) flow for custom servers launched by brainCloud.
///
/// When the PRE_READY_LAUNCH environment variable is "true", the server must wait
/// for the assigned lobby to reach the "starting" state before proceeding with launch.
///
/// Usage:
///   1. Read env vars with IsPreReadyLaunch() and GetTimeoutSecs().
///   2. Call Start() after your S2S session is authenticated.
///   3. Call Update() each tick (alongside s2s.RunCallbacks()) to handle timeouts.
///   4. Your PRLCompleteCallback receives true to proceed, false to exit.
/// </summary>
public class BrainCloudS2SPrl
{
    public delegate void PRLCompleteCallback(bool proceedWithLaunch);

    private enum PrlState
    {
        Idle,
        ConnectingRTT,
        SubscribingChannel,
        NotifyingSessionStarted,
        QueryingLobbyState,
        WaitingForLobbyReady,
        Complete
    }

    private BrainCloudS2S _s2s;
    private string _lobbyId;
    private int _timeoutSecs;
    private PRLCompleteCallback _callback;
    private PrlState _state = PrlState.Idle;
    private DateTime _startTime;
    private bool _complete = false;

    /// <summary>
    /// Returns true if the PRE_READY_LAUNCH environment variable is set to "true".
    /// </summary>
    public static bool IsPreReadyLaunch()
    {
        string val = Environment.GetEnvironmentVariable("PRE_READY_LAUNCH");
        return val != null && val.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the PRE_READY_LAUNCH_TIMEOUT_SECS environment variable value, or 0 if not set.
    /// </summary>
    public static int GetTimeoutSecs()
    {
        string val = Environment.GetEnvironmentVariable("PRL_TIMEOUT_SECS");
        if (int.TryParse(val, out int secs))
            return secs;

        val = Environment.GetEnvironmentVariable("PRE_READY_LAUNCH_TIMEOUT_SECS");
        if (int.TryParse(val, out int secs2))
            return secs2;

        return 60;
    }

    /// <summary>
    /// Begins the PRL flow. Assumes the S2S session is already authenticated.
    /// The callback receives true to proceed with launch, false to exit the server.
    /// </summary>
    public void Start(BrainCloudS2S s2s, string lobbyId, PRLCompleteCallback callback)
    {
        _s2s = s2s;
        _lobbyId = lobbyId;

        _timeoutSecs = GetTimeoutSecs();
        _callback = callback;
        _state = PrlState.ConnectingRTT;
        _startTime = DateTime.UtcNow;
        _complete = false;

        _s2s.LogString("[s2s prl] Starting PRL flow with lobbyId=" + lobbyId + ", timeoutSecs=" + _timeoutSecs);
        _s2s.EnableRTT(OnRTTConnected);
        _s2s.RegisterRTTRawCallback(OnRTTMessage);
    }

    /// <summary>
    /// Call each tick alongside s2s.RunCallbacks(). Handles timeout.
    /// </summary>
    public void Update()
    {
        if (_complete || _state == PrlState.Idle) return;
        if (_timeoutSecs > 0 && DateTime.UtcNow.Subtract(_startTime).TotalSeconds >= _timeoutSecs)
        {
            _s2s.LogString("[PRL] Timeout elapsed — exiting.");
            Complete(false);
        }
    }

    private void Complete(bool proceed)
    {
        if (_complete) return;
        _complete = true;
        _state = PrlState.Complete;
        _s2s.DeregisterRTTRawCallback();
        _callback?.Invoke(proceed);
    }

    private string BuildChannelId()
    {
        // Lobby ID format: <appId>:<instanceId>
        // Channel format:  <appId>:sy:_lobby_<instanceId>
        string instanceId = _lobbyId;
        int colonPos = _lobbyId.IndexOf(':');
        if (colonPos >= 0)
            instanceId = _lobbyId.Substring(colonPos + 1);
        return _s2s.AppId + ":sy:_lobby_" + instanceId;
    }

    // Step 1: RTT connected — subscribe to the lobby status channel
    private void OnRTTConnected(string responseString)
    {
        _s2s.LogString("[PRL] RTT connected. " + responseString + " Subscribing to channel: " + BuildChannelId());
        _state = PrlState.SubscribingChannel;
        _s2s.ConnectToChannel(BuildChannelId(), OnChannelSubscribed);
    }

    // Step 2: Channel subscribed — notify brainCloud the room session has started
    private void OnChannelSubscribed(string responseString)
    {
        _s2s.LogString("[PRL] Channel subscribed. Notifying session started." + responseString);
        _state = PrlState.NotifyingSessionStarted;
        _s2s.Request(new Dictionary<string, object>
        {
            { "service", "roomServer" },
            { "operation", "SYS_ROOM_SESSION_STARTED" },
            { "data", new Dictionary<string, object>
                {
                    { "serverId", Environment.GetEnvironmentVariable("SERVER_ID") },
                    { "serverContext", ParseServerContext() }
                }
            }
        }, OnSessionStartedNotified);
    }

    // Step 3: Session started notified — query the current lobby state
    private void OnSessionStartedNotified(string responseString)
    {
        _s2s.LogString("[PRL] Session started notified. Querying lobby state.");
        _state = PrlState.QueryingLobbyState;
        _s2s.Request(new Dictionary<string, object>
        {
            { "service", "lobby" },
            { "operation", "GET_LOBBY_DATA" },
            { "data", new Dictionary<string, object>
                {
                    { "lobbyId", _lobbyId }
                }
            }
        }, OnLobbyStateQueried);
    }

    // Step 4: Lobby state received — evaluate and act
    private void OnLobbyStateQueried(string responseString)
    {
        string lobbyState = ParseLobbyState(responseString);
        _s2s.LogString("[PRL] Initial lobby state: " + (lobbyState ?? "null"));
        HandleLobbyState(lobbyState);
    }

    // RTT push — only evaluated when waiting for lobby to transition to "starting"
    private void OnRTTMessage(string responseString)
    {
        _s2s.LogString("[PRL] RTT responseString " + responseString);
        if (_complete || _state != PrlState.WaitingForLobbyReady) return;
        string lobbyState = ParseLobbyStateFromRTT(responseString);
        if (lobbyState != null)
        {
            _s2s.LogString("[PRL] RTT lobby state update: " + lobbyState);
            HandleLobbyState(lobbyState);
        }
    }

    private void HandleLobbyState(string lobbyState)
    {
        if (lobbyState == null)
        {
            _s2s.LogString("[PRL] Lobby not found — exiting.");
            Complete(false);
        }
        else if (lobbyState == "disbanded")
        {
            _s2s.LogString("[PRL] Lobby disbanded — exiting.");
            Complete(false);
        }
        else if (lobbyState == "starting")
        {
            _s2s.LogString("[PRL] Lobby is starting — proceeding with launch.");
            Complete(true);
        }
        else
        {
            _s2s.LogString("[PRL] Lobby state is '" + lobbyState + "' — waiting for RTT update.");
            _state = PrlState.WaitingForLobbyReady;
        }
    }

    // Parses GET_LOBBY_DATA response: { "status": 200, "data": { "state": "..." } }
    private string ParseLobbyState(string responseString)
    {
        try
        {
            var response = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
            if ((int)response["status"] != 200) return null;
            var data = (Dictionary<string, object>)response["data"];
            return data["state"] as string;
        }
        catch { return null; }
    }

    // Parses RTT push: { "service": "chat", "operation": "INCOMING", "data": { "content": { "data": { "lobby": { "state": "..." } } } } }
    private string ParseLobbyStateFromRTT(string responseString)
    {
        try
        {
            var msg = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
            if (!(msg["service"] as string).Equals("chat")) return null;
            if (!(msg["operation"] as string).Equals("INCOMING")) return null;
            var data = (Dictionary<string, object>)msg["data"];
            var content = (Dictionary<string, object>)data["content"];
            var contentData = (Dictionary<string, object>)content["data"];
            var lobby = (Dictionary<string, object>)contentData["lobby"];
            return lobby["state"] as string;
        }
        catch { return null; }
    }

    public static Dictionary<string, object> ParseServerContext()
    {
        try
        {
            string val = Environment.GetEnvironmentVariable("SERVER_CONTEXT") ?? "{}";
            val = val.Trim().Trim('\'').Replace("\\\"", "\"");
            return JsonReader.Deserialize(val) as Dictionary<string, object> ?? new Dictionary<string, object>();
        }
        catch { return new Dictionary<string, object>(); }
    }
}
