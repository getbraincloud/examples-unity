using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Server
{
    public string Host;
    public int WsPort      = -1;
    public int TcpPort     = -1;
    public int UdpPort     = -1;
    public int i3dPort     = -1;
    // GameLift only exposes a WebSocket port — BrainCloudManager forces WEBSOCKET when set.
    public int GameliftPort = -1;
    public string Passcode;
    public string LobbyId;

    public Server(Dictionary<string, object> serverJson)
    {
        var connectData = serverJson["connectData"] as Dictionary<string, object>;
        var ports = connectData["ports"] as Dictionary<string, object>;

        Host = connectData["address"] as string;
        TryGetPort(ports, "ws",       ref WsPort);
        TryGetPort(ports, "tcp",      ref TcpPort);
        TryGetPort(ports, "udp",      ref UdpPort);
        TryGetPort(ports, "i3d",      ref i3dPort);
        TryGetPort(ports, "gamelift", ref GameliftPort);

        Passcode = serverJson["passcode"] as string;
        LobbyId  = serverJson["lobbyId"]  as string;
    }

    public Server(Dictionary<string, object> serverJson, bool noServerSelected)
    {
        var connectData = serverJson["connectInfo"] as Dictionary<string, object>;
        var ports = connectData["ports"] as Dictionary<string, object>;

        Host = connectData["address"] as string;
        TryGetPort(ports, "ws",       ref WsPort);
        TryGetPort(ports, "tcp",      ref TcpPort);
        TryGetPort(ports, "udp",      ref UdpPort);
        TryGetPort(ports, "i3d",      ref i3dPort);
        TryGetPort(ports, "gamelift", ref GameliftPort);

        Passcode = serverJson["passcode"] as string;
    }

    private static void TryGetPort(Dictionary<string, object> ports, string key, ref int field)
    {
        if (!ports.ContainsKey(key) || ports[key] == null) return;
        int p = Convert.ToInt32(ports[key]);
        if (p > 0) field = p;
    }
}
