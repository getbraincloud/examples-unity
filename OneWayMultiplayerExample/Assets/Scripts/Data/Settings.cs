using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings
{
    public static string UsernameKey = "Username";

    public static string PasswordKey = "Password";

    public static string EntityIdKey = "EntityId";
    
    public static UserInfo LoadPlayerInfo()
    {
        UserInfo playerInfo = new UserInfo();
        playerInfo.Username = PlayerPrefs.GetString(UsernameKey);
        playerInfo.EntityId = PlayerPrefs.GetString(EntityIdKey);
        return playerInfo;
    }

    public static void SaveEntityId(string in_id) 
    {
        PlayerPrefs.SetString(EntityIdKey, in_id);
        PlayerPrefs.Save();
    }

    public static void SaveLogin(string in_username, string in_password)
    {
        PlayerPrefs.SetString(UsernameKey, in_username);
        PlayerPrefs.SetString(PasswordKey, in_password);
        GameManager.Instance.CurrentUserInfo.Username = in_username;
        PlayerPrefs.Save();
    }
}
