using System;
using System.IO;
using BrainCloud;
using BrainCloud.LitJson;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class User
{
    public UserData m_userData = new UserData();

    public static string getScreeName()
    {
        return ErrorHandlingApp.getInstance().m_user.m_userData.m_screenName;
    }

    public static string getIdentities()
    {
        return ErrorHandlingApp.getInstance().m_user.m_userData.m_identities;
    }

    public void OnLoginResponse(string responseData)
    {
        var root = JsonMapper.ToObject(responseData);
        var data = root["data"];

        try
        {
            var newUser = bool.Parse(data["newUser"].ToString());
            if (newUser)
            {
                // Perform any logic needed for new users
                App.Bc.Client
                    .PlayerStateService.UpdateName(m_userData.m_screenName, OnSuccess_UpdatePlayerName,
                        OnFailed_UpdatePlayerName);
                ErrorHandlingApp.getInstance().m_user.m_userData.m_screenName = "";

                App.Bc.Client.IdentityService.GetIdentities(OnSuccess_GetIdentities, OnFailed_GetIdentities);
            }
            else
            {
                // Load any content on brainCloud that is needed for preexisting users
                App.Bc.Client
                    .PlayerStateService.ReadUserState(OnSuccess_ReadPlayerState, OnFailed_ReadPlayerState);
                App.Bc.Client
                    .PlayerStateService.GetAttributes(OnSuccess_GetAttributes, OnFailed_GetAttributes);
                App.Bc.Client.IdentityService.GetIdentities(OnSuccess_GetIdentities, OnFailed_GetIdentities);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void OnIdentitiesChangedResponse(string responseData)
    {
        App.Bc.Client
            .PlayerStateService.ReadUserState(OnSuccess_ReadPlayerState, OnFailed_ReadPlayerState);
        App.Bc.Client
            .PlayerStateService.GetAttributes(OnSuccess_GetAttributes, OnFailed_GetAttributes);

        App.Bc.Client.IdentityService.GetIdentities(OnSuccess_GetIdentities, OnFailed_GetIdentities);
    }

    public void OnNameChangedResponse(string responseData)
    {
        var root = JsonMapper.ToObject(responseData);
        var data = root["data"];

        m_userData.m_screenName = data["playerName"].ToString();

        SaveData();
    }


    public void InitData()
    {
        var userDataFile = Resources.Load<TextAsset>("Data/UserData");

#if UNITY_EDITOR
        if (userDataFile == null)
        {
            var blankData = new UserData("default_screen_name", "");
            var jsonCopy = JsonUtility.ToJson(blankData);
            var writer = File.CreateText(Application.dataPath + "/Resources/Data/UserData.json");
            writer.Write(jsonCopy);
            writer.Close();

            AssetDatabase.ImportAsset("Assets/Resources/Data/UserData.json");

            userDataFile = Resources.Load<TextAsset>("Data/UserData");
        }
#endif

        m_userData = JsonUtility.FromJson<UserData>(userDataFile.text);
    }

    public void SaveData()
    {
        var jsonCopy = JsonUtility.ToJson(m_userData);
        var writer = File.CreateText(Application.dataPath + "/Resources/Data/UserData.json");
        writer.Write(jsonCopy);
        writer.Close();
    }


    public void OnSuccess_ReadPlayerState(string responseData, object cbObject)
    {
        // Handle

        var root = JsonMapper.ToObject(responseData);
        var data = root["data"];

        m_userData.m_screenName = data["playerName"].ToString();

        SaveData();
    }

    public void OnFailed_ReadPlayerState(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        // Handle
    }


    public void OnSuccess_GetAttributes(string responseData, object cbObject)
    {
        // Get any desired Attributes on Log in
    }

    public void OnFailed_GetAttributes(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        // Handle error case
    }

    public void OnSuccess_UpdatePlayerName(string responseData, object cbObject)
    {
        // Handle
    }

    public void OnFailed_UpdatePlayerName(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        // Handle
    }

    public void OnSuccess_GetIdentities(string responseData, object cbObject)
    {
        var root = JsonMapper.ToObject(responseData);
        var data = root["data"];

        m_userData.m_identities = "";

        var identities = data["identities"];

        foreach (var identitiy in identities)
        {
            m_userData.m_identities += identitiy.ToString();

            Debug.Log(identitiy);
        }

        SaveData();
    }

    public void OnFailed_GetIdentities(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        // Handle
    }
}