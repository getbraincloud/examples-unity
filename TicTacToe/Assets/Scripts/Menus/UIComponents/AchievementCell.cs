using TMPro;
using UnityEngine;
using Gameframework;

public class AchievementCell : ImageDownloader
{
    [SerializeField] public TextMeshProUGUI AchievementName;
    [SerializeField] private CanvasGroup CanvasGrp = null;


    #region public
    public virtual void Init(Achievements.AchievementInfo in_pData, Achievements in_pAchievements)
    {
        m_pAchievementsData = in_pData;
        m_pAchievements = in_pAchievements;
        RawImage.gameObject.SetActive(false);
        AchievementName.gameObject.SetActive(false);
        DownloadImage(in_pData.ImageURL, false, ImageDownloaded);
    }

    public void SetAchievementName(string in_str, bool in_achieved)
    {
        AchievementName.text = in_str;
        CanvasGrp.alpha = in_achieved ? 1.0f : 0.5f;
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
