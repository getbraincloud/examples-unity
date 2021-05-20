using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Server
{
    public string Host;
    public int WsPort = -1;
    public int TcpPort = -1;
    public int UdpPort = -1;
    public string Passcode;
    public string LobbyId;

    public Server(Dictionary<string, object> serverJson)
    {
        var connectData = serverJson["connectData"] as Dictionary<string, object>;
        var ports = connectData["ports"] as Dictionary<string, object>;

        Host = connectData["address"] as string;
        WsPort = (int)ports["ws"];
        TcpPort = (int)ports["tcp"];
        UdpPort = (int)ports["udp"];
        Passcode = serverJson["passcode"] as string;
        LobbyId = serverJson["lobbyId"] as string;
    }
}
