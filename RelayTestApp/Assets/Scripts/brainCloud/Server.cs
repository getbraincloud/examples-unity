using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Server
{
    public string host;
    public int wsPort = -1;
    public int tcpPort = -1;
    public int udpPort = -1;
    public string passcode;
    public string lobbyId;

    public Server(Dictionary<string, object> serverJson)
    {
        var connectData = serverJson["connectData"] as Dictionary<string, object>;
        var ports = connectData["ports"] as Dictionary<string, object>;

        host = connectData["address"] as string;
        wsPort = (int)ports["ws"];
        tcpPort = (int)ports["tcp"];
        udpPort = (int)ports["udp"];
        passcode = serverJson["passcode"] as string;
        lobbyId = serverJson["lobbyId"] as string;
    }
}
