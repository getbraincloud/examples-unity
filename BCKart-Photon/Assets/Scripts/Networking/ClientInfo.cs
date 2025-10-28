using UnityEngine;

public static class ClientInfo {
    public static string Username {
        get => LoginData.playerName;
        set => LoginData.playerName = value;
    }

    public static int KartId {
        get => PlayerPrefs.GetInt("C_KartId", 0);
        set => PlayerPrefs.SetInt("C_KartId", value);
    }
    
    public static BrainCloudLoginData LoginData;
}