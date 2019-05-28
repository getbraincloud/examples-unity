using UnityEngine;
using System.Collections;

namespace Gameframework
{
    public class AudioData
    {   
        public enum eAudioType
        {
            effect,
            music,
            voice
        }

        // v 1.2
        public AudioData(string in_key, string in_assetBundleName, string in_fileName, string[] in_fileNames, float in_volume, bool in_loop)
        {
            Key = in_key;
            AssetBundleName = in_assetBundleName;
            FileName = in_fileName;
            FileNames = in_fileNames;
            Volume = in_volume;
            Loop = in_loop;

            Probability = 1.0f;
            FadeAmount = 0.0f;
            FadeOutTime = 0.0f;
            LastPickIndex = -1;
        }

        #region Public Accessors
        public string Key { get; private set; }
        public string AssetBundleName { get; private set; }
        public string FileName { get; private set; }

        public string[] FileNames { get; private set; }

        public float Volume { get; private set; }
        public float FadeAmount { get; set; }
        public float Probability { get; set; }
        public float FadeOutTime { get; set; }

        public int LastPickIndex { get; set;  }

        public eAudioType AudioType { get; set; }
        public bool Loop { get; private set; }

        #endregion Public Accessors
    }
}