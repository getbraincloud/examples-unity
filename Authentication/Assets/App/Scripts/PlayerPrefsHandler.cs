using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.XR;
using static UnityEngine.UI.Image;

public enum PlayerPrefKey
{
    Email,
    Username,
    Password
}

public static class PlayerPrefsHandler
{
    private const string PREFS_USER_EMAIL = ".email";
    private const string PREFS_USER_NAME = ".username";
    private const string PREFS_USER_PASSWORD = ".password";

    private static readonly Dictionary<PlayerPrefKey, string> PREFS_KEY_STRING_NAME = new Dictionary<PlayerPrefKey, string>
    {
        { PlayerPrefKey.Email,    BrainCloudManager.AppName + PREFS_USER_EMAIL },
        { PlayerPrefKey.Username, BrainCloudManager.AppName + PREFS_USER_NAME },
        { PlayerPrefKey.Password, BrainCloudManager.AppName + PREFS_USER_PASSWORD }
    };

    private static readonly List<PlayerPrefKey> PREFS_ENCRYPTED_KEYS = new List<PlayerPrefKey>
    {
        PlayerPrefKey.Email, PlayerPrefKey.Username, PlayerPrefKey.Password
    };

    private static BrainCloudManager bc { get; set; } // User's ProfileID will be used as the key

    #region Public

    public static void InitPlayerPrefs(BrainCloudManager brainCloudManager)
    {
        bc = brainCloudManager;

        foreach (PlayerPrefKey key in PREFS_KEY_STRING_NAME.Keys)
        {
            string keyName = PREFS_KEY_STRING_NAME[key];
            if (!PlayerPrefs.HasKey(keyName))
            {
                PlayerPrefs.SetString(keyName, string.Empty);
            }
        }
    }

    public static void SavePlayerPref(PlayerPrefKey pref, string value)
    {
        try
        {
            if (!string.IsNullOrEmpty(value) && PREFS_ENCRYPTED_KEYS.Contains(pref))
            {
                Debug.Assert(bc != null && !string.IsNullOrEmpty(bc.ProfileID),
                             "Cannot encrypt key if user has not logged in and been assigned a Profile ID.");

                byte[] key = Convert.FromBase64String(SanitizeString(bc.ProfileID));
                string encryptedString = Convert.ToBase64String(EncryptPrefValue(value, key));

                PlayerPrefs.SetString(PREFS_KEY_STRING_NAME[pref], encryptedString);
            }
            else
            {
                PlayerPrefs.SetString(PREFS_KEY_STRING_NAME[pref], value);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetString(PREFS_KEY_STRING_NAME[pref], string.Empty);
        }
    }

    public static string LoadPlayerPref(PlayerPrefKey pref)
    {
        try
        {
            string prefsValue = PlayerPrefs.GetString(PREFS_KEY_STRING_NAME[pref]);
            if (!string.IsNullOrEmpty(prefsValue) && PREFS_ENCRYPTED_KEYS.Contains(pref))
            {
                Debug.Assert(bc != null && !string.IsNullOrEmpty(bc.ProfileID),
                             "Cannot decrypt key if user has not logged in and been assigned a Profile ID.");

                byte[] key = Convert.FromBase64String(SanitizeString(bc.ProfileID));
                byte[] encryptedBytes = Convert.FromBase64String(prefsValue);

                return DecryptPrefValue(encryptedBytes, key);
            }

            return prefsValue;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            PlayerPrefs.SetString(PREFS_KEY_STRING_NAME[pref], string.Empty);
        }

        return string.Empty;
    }

    public static void ClearPlayerPrefs()
    {
        foreach (PlayerPrefKey key in PREFS_KEY_STRING_NAME.Keys)
        {
            PlayerPrefs.SetString(PREFS_KEY_STRING_NAME[key], string.Empty);
        }
    }

    #endregion

    #region Encryption & Decryption

    private static string SanitizeString(string original)
    {
        return original.Replace("-", string.Empty).Trim();
    }

    private static byte[] EncryptPrefValue(string stringValue, byte[] key)
    {
        if (string.IsNullOrEmpty(stringValue))
        {
            throw new ArgumentNullException("stringValue");
        }
        else if (key == null || key.Length <= 0)
        {
            throw new ArgumentNullException("key");
        }

        byte[] iv = null;
        byte[] encryptedBytes = null;
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            iv = aes.IV;
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(stringValue);
                    }

                    encryptedBytes = msEncrypt.ToArray();
                }
            }
        }

        byte[] concatenatedBytes = new byte[iv.Length + encryptedBytes.Length];
        iv.CopyTo(concatenatedBytes, 0);
        encryptedBytes.CopyTo(concatenatedBytes, iv.Length);

        return concatenatedBytes;
    }

    private static string DecryptPrefValue(byte[] encryptedBytes, byte[] key)
    {
        if (encryptedBytes == null || encryptedBytes.Length <= 0)
        {
            throw new ArgumentNullException("encryptedBytes");
        }
        else if (key == null || key.Length <= 0)
        {
            throw new ArgumentNullException("key");
        }

        byte[] iv = new byte[16]; // BlockSize is 16
        Array.Copy(encryptedBytes, iv, iv.Length);

        byte[] trimmedBytes = new byte[encryptedBytes.Length - iv.Length];
        Array.Copy(encryptedBytes, iv.Length, trimmedBytes, 0, trimmedBytes.Length);

        string decryptedStringValue = string.Empty;
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream msDecrypt = new MemoryStream(trimmedBytes))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        decryptedStringValue = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        return decryptedStringValue;
    }

    #endregion
}
