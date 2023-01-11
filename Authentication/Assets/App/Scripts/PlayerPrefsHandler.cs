using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum PlayerPrefKey
{
    RememberUser,
}

public static class PlayerPrefsHandler
{
    private const string PREFS_REMEMBER_USER = ".rememberUser";

    private static readonly Dictionary<PlayerPrefKey, string> PREFS_KEY_VALUE_NAME = new Dictionary<PlayerPrefKey, string>
    {
        { PlayerPrefKey.RememberUser, BrainCloudManager.AppName + PREFS_REMEMBER_USER }
    };

    #region Public

    public static void InitPlayerPrefs()
    {
        foreach (PlayerPrefKey key in PREFS_KEY_VALUE_NAME.Keys)
        {
            string keyName = PREFS_KEY_VALUE_NAME[key];
            if (!PlayerPrefs.HasKey(keyName))
            {
                PlayerPrefs.SetInt(keyName, 0);
            }
        }
    }

    public static void SavePlayerPref(PlayerPrefKey pref, bool value)
    {
        try
        {
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], value ? int.MaxValue : 0);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void SavePlayerPref(PlayerPrefKey pref, int value)
    {
        try
        {
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], value);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void SavePlayerPref(PlayerPrefKey pref, float value)
    {
        try
        {
            PlayerPrefs.SetFloat(PREFS_KEY_VALUE_NAME[pref], value);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void SavePlayerPref(PlayerPrefKey pref, string value)
    {
        try
        {
            PlayerPrefs.SetString(PREFS_KEY_VALUE_NAME[pref], value);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void LoadPlayerPref(PlayerPrefKey pref, out bool value)
    {
        value = false;

        try
        {
            value = PlayerPrefs.GetInt(PREFS_KEY_VALUE_NAME[pref]) > 0;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void LoadPlayerPref(PlayerPrefKey pref, out int value)
    {
        value = 0;

        try
        {
            value = PlayerPrefs.GetInt(PREFS_KEY_VALUE_NAME[pref]);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void LoadPlayerPref(PlayerPrefKey pref, out float value)
    {
        value = 0.0f;

        try
        {
            value = PlayerPrefs.GetFloat(PREFS_KEY_VALUE_NAME[pref]);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void LoadPlayerPref(PlayerPrefKey pref, out string value)
    {
        value = string.Empty;

        try
        {
            value = PlayerPrefs.GetString(PREFS_KEY_VALUE_NAME[pref]);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[pref], 0);
        }
    }

    public static void ClearPlayerPrefs()
    {
        foreach (PlayerPrefKey key in PREFS_KEY_VALUE_NAME.Keys)
        {
            PlayerPrefs.SetInt(PREFS_KEY_VALUE_NAME[key], 0);
        }
    }

    #endregion
}
