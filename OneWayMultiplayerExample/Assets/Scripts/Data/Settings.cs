using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings
{
    public static string UsernameKey = "Username";

    public static string PasswordKey = "Password";
    
    public static UserInfo LoadPlayerInfo()
    {
        UserInfo playerInfo = new UserInfo();
        playerInfo.Username = PlayerPrefs.GetString(UsernameKey);
        
        return playerInfo;
    }
}
