using Gameframework;
using UnityEngine;

namespace Gameframework
{
    public class GSettingsMgr
    {
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        public static void DeleteAppSpecificKeys()
        {
            PlayerPrefs.DeleteKey(USER_NAME);
            PlayerPrefs.DeleteKey(ASKED_TO_NOTIFY);
            PlayerPrefs.DeleteKey(PUSH_AUTHORIZED);

            PlayerPrefs.DeleteKey(GAME_SPEED);
        }

        #region Public Properties
        public static string UserName
        {
            get { return GetStringValue(USER_NAME); }
            set { SetStringValue(USER_NAME, value); }
        }
        
        // the last downloaded asset bundle version, just compares it to the version of the app
        public static string AssetBundleVersion
        {
            get { return GetStringValue(ASSET_BUNDLE_VERSION); }
            set { SetStringValue(ASSET_BUNDLE_VERSION, value); }
        }

        public static float GlobalVolume
        {
            get
            {
                float fToReturn = GetFloatValue(GLOBAL_VOLUME);
                if (fToReturn >= 0f)
                    return fToReturn;
                else
                {
                    GlobalVolume = 1f;
                    return 1f;
                }
            }

            set { SetFloatValue(GLOBAL_VOLUME, value); }
        }

        public static float MusicVolume
        {
            get
            {
                float fToReturn = GetFloatValue(MUSIC_VOLUME);
                if (fToReturn >= 0f)
                    return fToReturn;
                else
                {
                    GlobalVolume = 1f;
                    return 1f;
                }
            }

            set { SetFloatValue(MUSIC_VOLUME, value); }
        }

        public static float EffectVolume
        {
            get
            {
                float fToReturn = GetFloatValue(EFFECT_VOLUME);
                if (fToReturn >= 0f)
                    return fToReturn;
                else
                {
                    GlobalVolume = 1f;
                    return 1f;
                }
            }

            set { SetFloatValue(EFFECT_VOLUME, value); }
        }
        
        public static bool AskedToNotify
        {
            get { return GetIntValue(ASKED_TO_NOTIFY) == 0 ? false : true; }
            set { SetIntValue(ASKED_TO_NOTIFY, (value == false ? 0 : 1)); }
        }

        public static bool GlobalMuted
        {
            get { return GetIntValue(GLOBAL_MUTED) == 0 ? false : true; }
            set { SetIntValue(GLOBAL_MUTED, (value == false ? 0 : 1)); }
        }

        public static bool MusicMuted
        {
            get { return GetIntValue(MUSIC_MUTED) == 0 ? false : true; }
            set { SetIntValue(MUSIC_MUTED, (value == false ? 0 : 1)); }
        }

        public static bool SoundMuted
        {
            get { return GetIntValue(SOUND_MUTED) == 0 ? false : true; }
            set { SetIntValue(SOUND_MUTED, (value == false ? 0 : 1)); }
        }

        public static bool PushAuthorized
        {
            get { return GetIntValue(PUSH_AUTHORIZED) == 0 ? false : true; }
            set { SetIntValue(PUSH_AUTHORIZED, (value == false ? 0 : 1)); }
        }

        public static bool AutoHidePanelInSlotsGame
        {
            get { return GetIntValue(AUTO_HIDE_IN_SLOTS, 1) == 0 ? false : true; }
            set { SetIntValue(AUTO_HIDE_IN_SLOTS, (value == false ? 0 : 1)); }
        }

        public static int GameSpeed
        {
            get
            {
                int iToReturn = GetIntValue(GAME_SPEED);
                return iToReturn;
            }
            set { SetIntValue(GAME_SPEED, value); }
        }
        #endregion

        #region private 
        private const string ASKED_TO_NOTIFY = "ASKED_TO_NOTIFY";
        private const string USER_NAME = "USER_NAME";
        private const string GLOBAL_VOLUME = "GLOBAL_VOLUME";
        private const string MUSIC_VOLUME = "MUSIC_VOLUME";

        private const string EFFECT_VOLUME = "EFFECT_VOLUME";
        private const string GLOBAL_MUTED = "GLOBAL_MUTED";
        private const string MUSIC_MUTED = "MUSIC_MUTED";
        private const string SOUND_MUTED = "SOUND_MUTED";

        private const string PUSH_AUTHORIZED = "PUSH_AUTHORIZED";
        private const string LAST_COLOR_ONE = "LAST_COLOR_ONE";
        private const string GAME_SPEED = "GAME_SPEED";
        private const string AUTO_HIDE_IN_SLOTS = "AUTO_HIDE_IN_SLOTS";
        private const string ASSET_BUNDLE_VERSION = "ASSET_BUNDLE_VERSION"; // the last version of the app, when the assets were updated
        #endregion

        #region protected 
        protected static float GetFloatValue(string in_key)
        {
            return PlayerPrefs.GetFloat(in_key, -1f);
        }
        protected static void SetFloatValue(string in_key, float in_value)
        {
            PlayerPrefs.SetFloat(in_key, in_value);
        }

        protected static int GetIntValue(string in_key, int in_value = 0)
        {
            return PlayerPrefs.GetInt(in_key, in_value);
        }
        protected static void SetIntValue(string in_key, int in_value)
        {
            PlayerPrefs.SetInt(in_key, in_value);
        }

        protected static string GetStringValue(string in_key)
        {
            return PlayerPrefs.GetString(in_key, "");
        }
        protected static void SetStringValue(string in_key, string in_value)
        {
            PlayerPrefs.SetString(in_key, in_value);
        }
        #endregion
    }
}
