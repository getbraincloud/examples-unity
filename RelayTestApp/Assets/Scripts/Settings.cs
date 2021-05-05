using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings
{
    public static string GameColorKey = "GameColor";

    public static string UsernameKey = "Username";

    public static string PasswordKey = "Password";

    public static string ReliableKey = "Reliable";

    public static string OrderedKey = "Ordered";
    
    //Getters
    public static GameColors GetPlayerPrefColor() => (GameColors)PlayerPrefs.GetInt(GameColorKey);
    public static bool GetPlayerPrefBool(string key) => PlayerPrefs.GetInt(key) != 0;
    
    //Setters
    public static void SetPlayerPrefColor(GameColors colorToSave) => PlayerPrefs.SetInt(GameColorKey,(int) colorToSave);
    public static void SetPlayerPrefBool(string key, bool value) => PlayerPrefs.SetInt(key, value == false ? 0 : 1);


    public static void LoadSettings()
    {
        
        var GameInstance = GameManager.Instance;
        //Initialize local user
        GameInstance.CurrentUserInfo = new UserInfo();
        GameInstance.CurrentUserInfo.UserGameColor = GetPlayerPrefColor();
        var username = PlayerPrefs.GetString(UsernameKey);
        GameInstance.CurrentUserInfo.Username = username;
        GameInstance.UsernameInputField.text = username;
        
        GameInstance.PasswordInputField.text = PlayerPrefs.GetString(PasswordKey);
    }
}
