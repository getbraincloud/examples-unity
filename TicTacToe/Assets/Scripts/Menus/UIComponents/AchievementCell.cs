using TMPro;
using UnityEngine;
using Gameframework;

public class AchievementCell : ImageDownloader
{
    [SerializeField] public TextMeshProUGUI AchievementName;

    #region public
    public virtual void Init(Achievements.AchievementInfo in_pData, Achievements in_pAchievements)
    {
        m_pAchievementsData = in_pData;
        m_pAchievements = in_pAchievements;
        RawImage.gameObject.SetActive(false);
        AchievementName.gameObject.SetActive(false);
        DownloadImage(in_pData.ImageURL, false, ImageDownloaded);
    }

    public void SetAchievementName(string in_str)
    {
        AchievementName.text = in_str;
    }
    #endregion

    #region private
    private void ImageDownloaded(string in_url)
    {
        RawImage.gameObject.SetActive(true);
        AchievementName.gameObject.SetActive(true);
    }

    private Achievements.AchievementInfo m_pAchievementsData = null;
    private Achievements m_pAchievements = null;
    #endregion
}
