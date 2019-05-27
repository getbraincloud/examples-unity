using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Gameframework
{
    public class PlaySoundComponent : BaseBehaviour
    {
        // Public members.
        public string[] m_indexSounds = null;

        // Public methods.
        public void PlaySound(string in_soundKey)
        {
            PlaySound(in_soundKey, false);
        }

        // dummy second param
        public uint PlaySound(string in_soundKey, bool bReturn)
        {
            if (!m_myReferencedSounds.ContainsKey(in_soundKey))
            {
                m_myReferencedSounds[in_soundKey] = new List<uint>();
            }

            uint toReturn = GSoundMgr.PlaySound(in_soundKey);
            m_myReferencedSounds[in_soundKey].Add(toReturn);
            return toReturn;
        }

        public void PlaySoundIndex(int in_index)
        {
            PlaySoundIndex(in_index, false);
        }

        // dummy second param
        public uint PlaySoundIndex(int in_index, bool bReturn)
        {
            uint toReturn = 0;
            if (in_index < m_indexSounds.Length)
                toReturn = PlaySound(m_indexSounds[in_index], false);
            else
                GDebug.LogWarning("Invalid PlaySoundIndex index!");
            return toReturn;
        }

        public void PlayChildSound(string in_soundKey)
        {
            if (!m_myReferencedSounds.ContainsKey(in_soundKey))
            {
                m_myReferencedSounds[in_soundKey] = new List<uint>();
            }

            m_myReferencedSounds[in_soundKey].Add(GSoundMgr.PlaySound(in_soundKey, transform));
        }

        public void StopSound(string in_soundKey)
        {
            // can only stop the ones that i started
            if (m_myReferencedSounds.ContainsKey(in_soundKey))
            {
                for (int i = 0; i < m_myReferencedSounds[in_soundKey].Count; ++i)
                {
                    GSoundMgr.Instance.StopSound(m_myReferencedSounds[in_soundKey][i]);
                }
            }
        }

        public void DestroySound(string in_soundKey)
        {
            GSoundMgr.Instance.DestroySound(in_soundKey);
        }

        private Dictionary<string, List<uint>> m_myReferencedSounds = new Dictionary<string, List<uint>>();
    }
}
