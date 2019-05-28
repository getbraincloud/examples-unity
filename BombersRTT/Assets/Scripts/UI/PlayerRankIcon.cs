using Gameframework;
using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudUNETExample
{
    public class PlayerRankIcon : MonoBehaviour
    {
        #region private Properties
        [SerializeField]
        private RawImage RawImage = null;
        [SerializeField]
        private Sprite[] RankImages = null;
        #endregion

        #region public
        public void UpdateIcon(int in_rank)
        {
            RawImage.texture = RankImages[in_rank].texture;
        }
        #endregion
    }
}
