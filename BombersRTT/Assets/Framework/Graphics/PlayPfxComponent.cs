using UnityEngine;
using System.Collections.Generic;

namespace Gameframework
{
    public class PlayPfxComponent : BaseBehaviour
    {
        #region Public 
        public List<GameObject> _ParticleEffectLookup;

        public void PlayEffectWithIndex(int in_effectIndex)
        {
            // first some checks
            if (_ParticleEffectLookup.Count > in_effectIndex &&
                _ParticleEffectLookup[in_effectIndex] != null)
            {
                _ParticleEffectLookup[in_effectIndex].SetActive(false);     // ensure its inactive so it starts fresh
                _ParticleEffectLookup[in_effectIndex].SetActive(true);  // set it off
            }
        }

        public void HideEffectWithIndex(int in_effectIndex)
        {
            // first some checks
            if (_ParticleEffectLookup.Count > in_effectIndex &&
                _ParticleEffectLookup[in_effectIndex] != null)
            {
                _ParticleEffectLookup[in_effectIndex].SetActive(false);     // ensure its inactive so it starts fresh
            }
        }
        #endregion
    }
}
