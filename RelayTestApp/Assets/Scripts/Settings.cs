using UnityEngine;
/// <summary>
/// Features:
/// - Holding string keys for easy access
/// - Get/Set local player settings to PlayerPrefs
/// - Loads in player info
/// </summary>
public static class Settings
{
    public static string GameColorKey = "GameColor";

    public static string UsernameKey = "Username";

    public static string PasswordKey = "Password";

    public static string ReliableKey = "Reliable";

    public static string OrderedKey = "Ordered";

    public static string ChannelKey = "Channel";

    public static int GetChannel() => PlayerPrefs.GetInt(ChannelKey); 
    
    //Getters
    public static int GetPlayerPrefColor() => PlayerPrefs.GetInt(GameColorKey);
    public static bool GetPlayerPrefBool(string key) => PlayerPrefs.GetInt(key) != 0;
    
    //Setters
    public static void SetPlayerPrefColor(int colorToSave) => PlayerPrefs.SetInt(GameColorKey, colorToSave);
    public static void SetPlayerPrefBool(string key, bool value) => PlayerPrefs.SetInt(key, value == false ? 0 : 1);


    public static UserInfo LoadPlayerInfo()
    {
        UserInfo playerInfo = new UserInfo();
        playerInfo.Username = PlayerPrefs.GetString(UsernameKey);
        playerInfo.UserGameColor = GetPlayerPrefColor();
        return playerInfo;
    }
}
