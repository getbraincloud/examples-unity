using BrainCloudUNETExample.Connection;
using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
        private GameObject CheckBoxFrame;
        [SerializeField]
        private CanvasGroup CanvasGroup;
        [SerializeField]
        private Image ProgressImg;
        [SerializeField]
        private Text Progress;
        [SerializeField]
        private GameObject ProgressGroup;
#pragma warning restore 649
        #endregion

        #region public
        public virtual void Init(AchievementData in_aData)
        {
            m_pData = in_aData;
            if (m_pData != null)
                DownloadImage(m_pData.ImageURL);

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
                ProgressGroup.SetActive(!m_pData.Achieved);
                CheckBoxFrame.SetActive(m_pData.Achieved);

                List<BrainCloudStats.Stat> playerStats = BrainCloudStats.Instance.GetStats();

                if (m_pData.Thresholds != null)
                {
                    string strstats;
                    int curValue;

                    Dictionary<string, object> stats = m_pData.Thresholds;
                    foreach (string key in stats.Keys)
                    {
                        strstats = key;
                        curValue = 0;
                        int maxValue = (int)m_pData.Thresholds[key];

                        for (int i = 0; i < playerStats.Count; i++)
                        {
                            if (playerStats[i].Key == strstats)
                            {
                                curValue = playerStats[i].Value;
                                Progress.text = curValue + "/" + maxValue;
                                break;
                            }
                        }
                        if (curValue > 0 && curValue == maxValue)
                        {
                            Progress.gameObject.SetActive(false);
                            ProgressImg.gameObject.SetActive(false);
                        }
                        else
                        {
                            ProgressImg.fillAmount = ((float)curValue / (float)maxValue);
                        }
                    }
                }
                else
                {
                    // Handle special achievement
                    if (m_pData.Name.Equals("Quickshot"))
                    {
                        ProgressGroup.SetActive(false);
                        CheckBoxFrame.SetActive(true);
                    }
                }
            }
        }
        #endregion
        #region private
        private AchievementData m_pData = null;
        #endregion
    }
}
