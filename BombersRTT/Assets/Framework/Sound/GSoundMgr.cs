using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameframework
{
    public class GSoundMgr : SingletonBehaviour<GSoundMgr>
    {
        public const string DEFAULT_MUSIC_KEY = "music";
        #region Public Accessors
        public float GlobalVolume
        {
            get { return m_globalSoundVolume; }
            set
            {
                m_globalSoundVolume = Mathf.Clamp01(value);// cap it between 0-1
                m_musicEnabled = true;      // TODO: FORCING music to be ON
                m_sfxEnabled = true;        // TODO: FORCING sFX to be ON
                UpdateActiveSoundVolume();
                SavePlayerSettings();
            }
        }

        public float MusicVolume
        {
            get { return m_musicVolume; }
            set
            {
                m_musicVolume = Mathf.Clamp01(value);// cap it between 0-1
                m_musicEnabled = true;      // TODO: FORCING music to be ON
                UpdateActiveSoundVolume();
                SavePlayerSettings();
            }
        }

        public float EffectVolume
        {
            get { return m_effectSoundVolume; }
            set
            {
                m_effectSoundVolume = Mathf.Clamp01(value);// cap it between 0-1
                m_sfxEnabled = true;        // TODO: FORCING sFX to be ON
                SavePlayerSettings();
            }
        }

        public bool IsMuted
        {
            get { return m_isMuted; }
            set { m_isMuted = value; }
        }

        public bool MusicEnabled
        {
            get { return m_musicEnabled; }
        }

        public bool SoundFxEnabled
        {
            get { return m_sfxEnabled; }
        }
        #endregion Public Accessors

        #region SingletonBehavior
        void Start()
        {
            // Load default sound info
            LoadSoundConfig();

            // Load Player settings
            LoadPlayerSettings();

            this.gameObject.AddComponent<AudioListener>();
        }

        private void Update()
        {
            m_playedSoundKeysInFrame.Clear();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // pause all active sounds
                foreach (var pair in m_activeAudioSources)
                {
                    AudioSource source = pair.Value;
                    if (source != null && source.isPlaying)
                        source.Pause();
                }
            }
            else
            {
                // resume them all
                foreach (var pair in m_activeAudioSources)
                {
                    AudioSource source = pair.Value;
                    // not playing
                    if (source != null && !source.isPlaying)
                        source.Play();
                }
            }
        }
        #endregion

        #region Public

        public static uint PlaySound(string in_key, Transform in_parent = null)
        {
            return Instance.PlaySoundInternal(in_key, in_parent);
        }

        public uint PlayMusic(string in_musickey = DEFAULT_MUSIC_KEY)
        {
            if (m_musicEnabled)
            {
                m_lastTriggeredMusicKey = in_musickey;
                m_currentMusicID = PlaySoundInternal(in_musickey);
                return m_currentMusicID;
            }
            return uint.MaxValue;//; AHH max value!
        }

        public uint PlaySoundAndFadeMusic(string in_key, Transform in_parent = null)
        {
            uint soundId = PlaySound(in_key, in_parent);
            if (soundId != 0)
            {
                AudioSource source = m_activeAudioSources[soundId];
                if (source != null)
                {
                    float soundLen = source.clip.length;
                    if (m_activeAudioSources.ContainsKey(m_currentMusicID))
                    {
                        AudioSource musicSource = m_activeAudioSources[m_currentMusicID];
                        StartCoroutine(FadeAudioObjectOutAndIn(musicSource.gameObject, soundLen, .1f, 1.5f));
                    }
                }
            }

            return soundId;
        }

        /**
		 * Fades out the audio object for an amount of time then fade in back. 
		 * Good to play a sound on top of the music and fade the music temporaly
		 * 
		 * @param in_aObject Audio source
		 * @param in_totalTime the total time of the fade out/fade in.
		 * @param in_fadeOutTime duration of the fading out
		 * @param in_fadeInTime duration of the fading in
		 * */
        private IEnumerator FadeAudioObjectOutAndIn(GameObject in_aObject, float in_totalTime, float in_fadeOutTime, float in_fadeInTime)
        {
            AudioSource aSource = in_aObject.GetComponent<AudioSource>();
            float startingVolume = aSource.volume;

            // Fade out
            float time = in_fadeOutTime;
            while (time > 0)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();

                if (in_aObject == null) yield return true; // AudioSource might have been destroyed

                time -= Time.deltaTime;
                if (time < 0) time = 0;
                aSource.volume = time / in_fadeOutTime * startingVolume;
                if (time <= 0) break;
            }

            // Silence
            time = in_totalTime - in_fadeOutTime - in_fadeInTime;
            while (time > 0)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();

                if (in_aObject == null) yield return true; // AudioSource might have been destroyed

                time -= Time.deltaTime;
                if (time <= 0) break;
            }

            // Fade in				
            time = 0;
            while (time < in_fadeInTime)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();

                if (in_aObject == null) yield return true; // AudioSource might have been destroyed

                time += Time.deltaTime;
                if (time > in_fadeInTime) time = in_fadeInTime;
                aSource.volume = time / in_fadeInTime * startingVolume;
                if (time >= in_fadeInTime) break;
            }

            aSource.volume = startingVolume;
            yield return true;
        }

        public void DestroySound(string in_soundId)
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                if (child.name == in_soundId)
                {
                    Destroy(child.gameObject);
                    return;
                }
            }
        }

        public bool StopSound(uint in_soundId)
        {
            bool bToReturn = false;
            
            // look up to see if its still active
            if (m_activeAudioSources.ContainsKey(in_soundId) && m_activeAudioSources[in_soundId] != null)
            {
                float fadeOutTime = m_audioLookupData[m_activeAudioSources[in_soundId].name].FadeOutTime;
                if (fadeOutTime <= 0.0f)
                {
                    // stop it, and kill right away
                    cleanupAudioSource(in_soundId);
                    bToReturn = true;
                }
                // start the process to fade it out and stop
                else
                {
                    StartCoroutine(FadeOutSound(in_soundId, fadeOutTime));
                }
            }

            return bToReturn;
        }

        public IEnumerator FadeOutSound(uint in_soundId, float in_FadeTime)
        {
            // look up to see if its still active
            if (m_activeAudioSources.ContainsKey(in_soundId) && m_activeAudioSources[in_soundId] != null)
            {
                AudioSource audioSource = m_activeAudioSources[in_soundId];
                float startVolume = audioSource.volume;
                while (audioSource != null && audioSource.volume > 0)
                {
                    audioSource.volume -= startVolume * Time.deltaTime / in_FadeTime;
                    yield return YieldFactory.GetWaitForEndOfFrame();
                }

                cleanupAudioSource(in_soundId);
            }
            yield return true;
        }

        // removes from preloaded 
        public bool PurgeAllSounds()
        {
            bool bToReturn = false;

            if (m_activeAudioSources != null)
            {
                Dictionary<uint, AudioSource> sources = new Dictionary<uint, AudioSource>(m_activeAudioSources);
                // rkill all not playing
                foreach (var pair in sources)
                {
                    AudioSource source = pair.Value;
                    // not playing
                    if (source != null && !source.isPlaying)
                        StopSound(pair.Key);
                }
            }

            return bToReturn;
        }

        public void UpdateActiveSoundVolume()
        {
            // change the volume of all active sounds
            foreach (var pair in m_activeAudioSources)
            {
                AudioSource source = pair.Value;
                if (source != null)
                    source.volume = GetVolumeToPlayAt(GetAudioDataFromKey(source.name));
            }
        }

        public void SavePlayerSettings()
        {
            GSettingsMgr.GlobalVolume = (GlobalVolume);
            GSettingsMgr.MusicVolume = (MusicVolume);
            GSettingsMgr.EffectVolume = (EffectVolume);
            GSettingsMgr.GlobalMuted = (IsMuted);
            GSettingsMgr.MusicMuted = (m_musicEnabled);
            GSettingsMgr.SoundMuted = (m_sfxEnabled);
        }

        public bool IsMusicPlaying()
        {
            return m_activeMusicSounds.Count > 0;
        }

        public void StartAmbientSounds()
        {
            if (!m_sfxEnabled)
                return;
        }

        public void StopAmbientSounds()
        {
            if (m_ambientPlaying != 0)
            {
                StopSound(m_ambientMusicSounds);
                m_ambientPlaying = 0;
            }
        }

        public void StopMusic()
        {
            uint musicID;
            for (int i = 0; i < m_activeMusicSounds.Count; ++i)
            {
                musicID = m_activeMusicSounds[i];
                if (StopSound(musicID))
                {
                    m_activeMusicSounds.Remove(musicID);
                }

            }
        }

        public void ToggleMusic()
        {
            m_musicEnabled = !m_musicEnabled;
            MusicVolume = m_musicEnabled ? 1.0f : 0.0f;
            if (m_musicEnabled)
            {
                PlayMusic(m_lastTriggeredMusicKey);
            }
            else
            {
                StopMusic();
            }

            SavePlayerSettings();
        }

        public void ToggleSfx()
        {
            m_sfxEnabled = !m_sfxEnabled;
            EffectVolume = m_sfxEnabled ? 1.0f : 0.0f;
            if (m_sfxEnabled)
            {
                StartAmbientSounds();
            }
            else
            {
                StopAmbientSounds();
            }

            SavePlayerSettings();
        }

        public void LoadSoundConfig(string in_jsonConfig)
        {
            var data = JsonReader.Deserialize<Dictionary<string, object>[]>(in_jsonConfig);
            Dictionary<string, object> soundConfig;

            AudioData newData = null;
            string keyValue = "";
            string assetBundleName = "";
            string fileName = "";
            string[] fileNames = null;
            for (int i = 0; i < data.Length; ++i)
            {
                soundConfig = data[i];
                newData = null;
                try
                {
                    keyValue = (string)soundConfig[SOUND_KEY];

                    try
                    {
                        fileName = ((string)soundConfig[FILE_NAME_KEY]).Trim();
                    }
                    catch { fileName = ""; }

                    try
                    {
                        assetBundleName = (string)soundConfig[ASSET_BUNDLE_KEY];
                    }
                    catch { assetBundleName = ""; }

                    try
                    {
                        fileNames = ((string)soundConfig[FILE_NAMES_KEY]).Split(',');
                        for (int index = 0; index < fileNames.Length; ++index)
                        {
                            fileNames[index] = fileNames[index].Trim();
                        }
                    }
                    catch { fileNames = null; }

                    newData = new AudioData(keyValue,
                                            assetBundleName,
                                            fileName,
                                            fileNames,
                                            (float)(double)soundConfig[VOLUME_KEY],
                                            (bool)soundConfig[LOOP_KEY]
                                            );
                    // set fade amount 
                    try
                    {
                        newData.FadeAmount = (float)(double)soundConfig[FADE_AMOUNT_KEY];
                    }
                    catch { }

                    // set fade out time  
                    try
                    {
                        newData.FadeOutTime = (float)(double)soundConfig[FADE_OUT_TIME_KEY];
                    }
                    catch { }

                    // set probability 
                    try
                    {
                        newData.Probability = (float)(double)soundConfig[PROBABILITY_KEY];
                    }
                    catch { }

                    // set the audio type
                    try
                    {
                        string tempType = (string)soundConfig[AUDIO_TYPE_KEY];
                        if (tempType == "effect")
                            newData.AudioType = AudioData.eAudioType.effect;
                        else if (tempType == "music")
                            newData.AudioType = AudioData.eAudioType.music;
                        else if (tempType == "voice")
                            newData.AudioType = AudioData.eAudioType.voice;
                    }
                    catch { }

                }
                catch { }

                if (newData != null)
                {
                    if (!m_audioLookupData.ContainsKey(keyValue))
                    {
                        m_audioLookupData[keyValue] = newData;
                    }
                    else
                    {
                        GDebug.LogWarning("Attempted to add new AudioData " + keyValue + ", but it already exists.  No action taken.  Have you tried unloading a previous config file");
                    }
                }
            }
        }

        public void LoadSoundConfig(string in_assetBundle, string in_fileName)
        {
            StartCoroutine(LoadSoundConfigRoutine(in_assetBundle, in_fileName));
        }

        public void UnloadSoundConfig(string in_jsonConfig)
        {
            var data = JsonReader.Deserialize<Dictionary<string, object>[]>(in_jsonConfig);
            Dictionary<string, object> soundConfig;
            GEntityFactory entFact = GEntityFactory.Instance;

            string keyValue = "";
            for (int i = 0; i < data.Length; ++i)
            {
                soundConfig = data[i];
                try
                {
                    keyValue = (string)soundConfig[SOUND_KEY];
                    m_audioLookupData.Remove(keyValue);
                    entFact.RemoveReferencedResource(keyValue, false);
                }
                catch { }
            }
        }

        public void UnloadSoundConfig(string in_assetBundle, string in_fileName)
        {
            StartCoroutine(UnloadSoundConfigRoutine(in_assetBundle, in_fileName));
        }

        public IEnumerator LoadSoundConfigRoutine(string in_assetBundle, string in_fileName)
        {
#if ENABLE_ASSET_BUNDLES
            yield return StartCoroutine(GEntityFactory.Instance.LoadObjectFromAssetBundle<TextAsset>(in_assetBundle, in_fileName));

            TextAsset asset = GEntityFactory.Instance.GetObjectFromAssetBundle<TextAsset>(in_assetBundle, in_fileName);
            if (asset != null)
                LoadSoundConfig(asset.text);
#endif
            yield return null;
        }

        public IEnumerator UnloadSoundConfigRoutine(string in_assetBundle, string in_fileName)
        {
            yield return StartCoroutine(GEntityFactory.Instance.LoadObjectFromAssetBundle<TextAsset>(in_assetBundle, in_fileName));
            TextAsset asset = GEntityFactory.Instance.GetObjectFromAssetBundle<TextAsset>(in_assetBundle, in_fileName);
            UnloadSoundConfig(asset.text);

            GEntityFactory.Instance.UnloadAssetBundlesByName(in_assetBundle);
            yield return null;
        }
        #endregion Public

        #region Private Helpers
        private uint PlaySoundInternal(string in_key, Transform in_parent = null)
        {
            // increment the soundID so people can say stop on it
            uint soundId = 0;

            // look up the file name from the key information
            AudioData data = GetAudioDataFromKey(in_key);

            // Odds of not playing.
            if (data != null && !m_playedSoundKeysInFrame.ContainsKey(in_key) &&
                (data.Probability == 1.0f || Random.Range(0.0f, 1.0f) <= data.Probability))
            {
                soundId = ++m_soundID;
                m_playedSoundKeysInFrame[in_key] = soundId;
                AudioClip pClip = null;

                pClip = GetAudioClipFromFileName(data, soundId, in_parent);

                // Play the clip only if it has an AudioClip. Otherwise, LoadAudioClipFromAssetBundle will play it after loading it.
                if (pClip != null)
                    playClipWithdata(data, pClip, soundId, in_parent);
            }
            return soundId;
        }

        private void playClipWithdata(AudioData in_data, AudioClip in_clip, uint in_soundId, Transform in_parent)
        {
            AudioSource source = null;

            float volumeToPlayAt = GetVolumeToPlayAt(in_data);
            if ((in_data.AudioType == AudioData.eAudioType.effect || in_data.AudioType == AudioData.eAudioType.voice) && m_sfxEnabled == false)
                volumeToPlayAt = 0;

            // TODO: does this audio clip have fade information ? 
            if (in_data.FadeAmount != 0)
            {
                AnimationClip newClip = new AnimationClip();
                newClip.legacy = true;
                source = CreateGetFadeAudioObject(in_clip, volumeToPlayAt, in_data.Loop, newClip, in_data.Key, in_parent);
                StartCoroutine(FadeAudioObject(source.gameObject, in_data.FadeAmount));
                newClip = null;
            }
            // otherwise just play it regularly
            else
            {
                source = CreatePlayAudioObject(in_clip, volumeToPlayAt, in_data.Loop, in_data.Key, in_parent);
            }

            m_activeAudioSources[in_soundId] = source;

            if (in_data.AudioType == AudioData.eAudioType.music)
            {
                m_activeMusicSounds.Add(in_soundId);
            }
        }

        private float GetVolumeToPlayAt(AudioData in_audioData)
        {
            float fToReturn = (m_isMuted ? 0.0f : in_audioData.Volume *                     // defined volume
                                    GlobalVolume *                                          // scaled with global sound
                    (in_audioData.AudioType == AudioData.eAudioType.effect ? EffectVolume : // and if its an effect, scaled by effect volume
                     in_audioData.AudioType == AudioData.eAudioType.music ? MusicVolume :   // or its a music effect, scaled by music volume
                     1.0f));                                                                // no extra scaling

            return fToReturn;
        }

        private void ToggleMute()
        {
            m_isMuted = !m_isMuted;
            UpdateActiveSoundVolume();
            SavePlayerSettings();
        }
        private void IncrementGlobalVolume()
        {
            GlobalVolume = GlobalVolume + 0.1f;
            UpdateActiveSoundVolume();
            SavePlayerSettings();
        }

        private void DecrementGlobalVolume()
        {
            GlobalVolume = GlobalVolume - 0.1f;
            UpdateActiveSoundVolume();
            SavePlayerSettings();
        }

        private void IncrementMusicVolume()
        {
            MusicVolume = MusicVolume + 0.1f;
            SavePlayerSettings();
        }

        private void DecrementMusicVolume()
        {
            MusicVolume = MusicVolume - 0.1f;
            SavePlayerSettings();
        }

        private void IncrementEffectVolume()
        {
            EffectVolume = EffectVolume + 0.1f;
            SavePlayerSettings();
        }

        private void DecrementEffectVolume()
        {
            EffectVolume = EffectVolume - 0.1f;
            SavePlayerSettings();
        }

        // creates a dynamic Audio object to position and play in the world
        private AudioSource CreatePlayAudioObject(AudioClip in_aClip, float in_vol, bool in_bLoop, string in_objName, Transform in_parent)
        {
            // return this script for use
            AudioSource apAudio = CreateAudioSource(in_aClip, in_vol, in_bLoop, in_objName);

            // may be null because of a wrong or missing key / file name
            if (apAudio != null && in_aClip != null)
            {
                // play the clip
                apAudio.Play();

                // TODO: does this take into consideration if it loops?
                // destroy this object after clip length (*1.25 to compensate for loading)
                if (!in_bLoop)
                {
                    Destroy(apAudio.gameObject, in_aClip.length * 1.25f);
                }

                if (in_parent != null)
                    apAudio.gameObject.transform.SetParent(in_parent);
                else
                    apAudio.gameObject.transform.SetParent(gameObject.transform);
            }

            return apAudio;
        }
        
        private void cleanupAudioSource(uint in_soundId)
        {
            if (m_activeAudioSources.ContainsKey(in_soundId))
            {
                m_activeAudioSources[in_soundId].Stop();
                Destroy(m_activeAudioSources[in_soundId].gameObject);
                m_activeAudioSources.Remove(in_soundId);
            }
        }

        // fade our AudioSource object based on speed (> 0 fades volume up, < 0 fades volume out,
        // == 0 assumes the sound is playing and just destroys it)
        private IEnumerator FadeAudioObject(GameObject in_aObject, float in_fadeSpeed)
        {
            Animation apAnim = in_aObject.GetComponent<Animation>();
            AudioSource aSource = in_aObject.GetComponent<AudioSource>();

            // we are not a fade audio object
            if (apAnim == null)
            {
                // we simply destroy the object and return
                if (in_fadeSpeed <= 0)
                {
                    Destroy(in_aObject);
                }

                // we are a psitive playing sound, so just play it
                if (in_fadeSpeed > 0 && aSource != null)
                {
                    aSource.Play();
                }
                yield return true;
            }

            // animation clip is default to fade out (1 to 0), these will look reveresed but 
            //they are correct
            if (in_fadeSpeed < 0)
            {
                apAnim[apAnim.clip.name].time = apAnim[apAnim.clip.name].length;
            }
            else
            {
                apAnim[apAnim.clip.name].time = 0;
            }

            // set our speed
            apAnim[apAnim.clip.name].speed = in_fadeSpeed;

            // play the audio
            if (aSource.isPlaying == false)
            {
                aSource.Play();
            }

            // play the fade
            apAnim.Play();

            // yield the length of the clip
            if (in_fadeSpeed < 0)
            {
                while (apAnim.isPlaying) { yield return YieldFactory.GetWaitForEndOfFrame(); }
                Destroy(in_aObject);
            }
        }

        // over loads
        // creates/returns a dynamic Audio object to position and plays in the world with loop
        private AudioSource CreateGetFadeAudioObject(AudioClip in_aClip, float in_vol, bool in_bLoop, AnimationClip in_fadeClip, string in_objName, Transform in_parent)
        {
            AudioSource apAudio = CreateAudioSource(in_aClip, in_vol, in_bLoop, in_objName);

            if (in_fadeClip != null)
            {
                Animation apAnim = apAudio.gameObject.AddComponent<Animation>();
                apAnim.AddClip(in_fadeClip, in_fadeClip.name);
                apAnim.clip = in_fadeClip;
                apAnim.clip.name = in_objName;
                apAnim.playAutomatically = false;
            }

            // attach ourselves as parents
            if (in_parent != null)
                apAudio.gameObject.transform.SetParent(in_parent);
            else
                apAudio.gameObject.transform.SetParent(gameObject.transform);

            // return our AudioObject
            return apAudio;
        }

        private AudioSource CreateAudioSource(AudioClip in_aClip, float in_vol, bool in_loop, string in_objName)
        {
            // instance a new gameobject
            GameObject apObject = new GameObject(in_objName);

            // position the object in the world
            apObject.transform.position = Vector3.zero;

            // add an AudioSource component
            apObject.AddComponent<AudioSource>();

            // return this script for use
            AudioSource apAudio = apObject.GetComponent<AudioSource>();

            // initialize some AudioSource fields
            // TODO: extend these propteries to a sound definition
            apAudio.playOnAwake = false;
            apAudio.rolloffMode = AudioRolloffMode.Linear;
            apAudio.loop = in_loop;
            apAudio.clip = in_aClip;
            apAudio.volume = in_vol;
            apAudio.spatialBlend = 0.0f;    // 2d SOUNDS

            // dont destroy this between scenes
            DontDestroyOnLoad(apAudio.gameObject);

            return apAudio;
        }

        private IEnumerator LoadAudioClipFromAssetBundle(AudioData in_data, string in_fileName, string in_assetBundle, uint in_soundId, Transform in_parent)
        {
            yield return StartCoroutine(GEntityFactory.Instance.LoadObjectFromAssetBundle<AudioClip>(in_assetBundle, in_fileName));

            AudioClip pClip = GEntityFactory.Instance.GetObjectFromAssetBundle<AudioClip>(in_assetBundle, in_fileName);
            playClipWithdata(in_data, pClip, in_soundId, in_parent);
        }

        private AudioClip GetAudioClipFromFileName(AudioData in_data, uint in_soundId, Transform in_parent)
        {
            string fileToLoad = in_data.FileName;

            if (in_data.FileNames != null && in_data.FileNames.Length > 1)
            {
                int rndPick = Random.Range(0, in_data.FileNames.Length);
                while (rndPick == in_data.LastPickIndex)
                {
                    rndPick = Random.Range(0, in_data.FileNames.Length);
                }
                in_data.LastPickIndex = rndPick;
                fileToLoad = in_data.FileNames[rndPick];
            }

            AudioClip toReturn = in_data.AssetBundleName == "" ?
                                GEntityFactory.Instance.CreateAudioResourceAtPath(fileToLoad) :
                                GEntityFactory.Instance.GetObjectFromAssetBundle<AudioClip>(in_data.AssetBundleName, fileToLoad);

            if (toReturn == null && in_data.AssetBundleName != "")
            {
                StartCoroutine(LoadAudioClipFromAssetBundle(in_data, fileToLoad, in_data.AssetBundleName, in_soundId, in_parent));
            }

            return toReturn;
        }

        private AudioData GetAudioDataFromKey(string in_key)
        {
            AudioData toReturn = null;

            if (m_audioLookupData.ContainsKey(in_key))
            {
                toReturn = m_audioLookupData[in_key];
            }

            return toReturn;
        }

        private void LoadPlayerSettings()
        {
            m_globalSoundVolume = GSettingsMgr.GlobalVolume;
            m_effectSoundVolume = GSettingsMgr.EffectVolume;
            m_musicVolume = GSettingsMgr.MusicVolume;
            m_isMuted = false;

            m_musicEnabled = true;
            m_sfxEnabled = true;
        }

        private void LoadSoundConfig()
        {
            // read the config dict
            TextAsset textAsset = (TextAsset)Resources.Load("Sounds/SoundConfig");
            LoadSoundConfig(textAsset.text);
        }

        #endregion Private Helpers

        #region Private Members
        private Dictionary<string, uint> m_playedSoundKeysInFrame = new Dictionary<string, uint>();
        private Dictionary<uint, AudioSource> m_activeAudioSources = new Dictionary<uint, AudioSource>();
        private Dictionary<string, AudioData> m_audioLookupData = new Dictionary<string, AudioData>();
        private List<uint> m_activeMusicSounds = new List<uint>();

        private uint m_ambientMusicSounds = 0;
        private uint m_soundID = 0;
        private uint m_currentMusicID = 0;

        private float m_globalSoundVolume = 1.0f;
        private float m_effectSoundVolume = 1.0f;
        private float m_musicVolume = 1.0f;
        private bool m_isMuted = false;
        private bool m_musicEnabled = true;
        private bool m_sfxEnabled = true;
        private int m_ambientPlaying = 0;

        private string m_lastTriggeredMusicKey = "";

        private static string SOUND_KEY = "soundKey";
        private static string FILE_NAME_KEY = "fileName";
        private static string ASSET_BUNDLE_KEY = "assetBundle";
        private static string FILE_NAMES_KEY = "fileNames";
        private static string VOLUME_KEY = "volume";
        private static string LOOP_KEY = "loop";

        private static string FADE_AMOUNT_KEY = "fadeSpeed";
        private static string PROBABILITY_KEY = "probability";
        private static string AUDIO_TYPE_KEY = "type";

        private static string FADE_OUT_TIME_KEY = "fadeOutTime";
        #endregion Private Members
    }
}
