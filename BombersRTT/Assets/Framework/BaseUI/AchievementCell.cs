using BrainCloudUNETExample.Connection;
using Gameframework;
using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudUNETExample
{
    public class AchievementCell : ImageDownloader
    {
        #region private Properties
#pragma warning disable 649
        [SerializeField]
        private Text Name;
        [SerializeField]
        private Text Description;
        [SerializeField]
        private GameObject Check;
        [SerializeField]
        private CanvasGroup CanvasGroup;
#pragma warning restore 649
        #endregion

        #region public
        public virtual void Init(AchievementData in_aData)
        {
            m_pData = in_aData;
            UpdateUI();
        }

        public virtual void UpdateUI()
        {
            if (m_pData != null)
            {
                Name.text = m_pData.Name;
                Description.text = m_pData.Description;
                Check.gameObject.SetActive(m_pData.Achieved);
                CanvasGroup.alpha = m_pData.Achieved ? 1.0f : 0.3f;
                DownloadImage(m_pData.ImageURL);
            }
        }
        #endregion
        #region private
        private AchievementData m_pData = null;
        #endregion
    }
}
