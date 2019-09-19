using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardCell : MonoBehaviour
{
    public const string DEFAULT_NAME = "DEFAULT NAME";

    [SerializeField] public TextMeshProUGUI Rank;
    [SerializeField] public TextMeshProUGUI Name;

    #region public
    public virtual void Init(PlayerInfo in_pData, int in_rank)
    {
        m_pLeaderboardData = in_pData;
        m_rank = in_rank;
        UpdateUI();
    }

    public virtual void UpdateUI()
    {
        Rank.text = m_rank.ToString();
        Name.text = m_pLeaderboardData.PlayerName;
    }
    #endregion

    #region private
    private int m_rank = 0;
    private PlayerInfo m_pLeaderboardData = null;
    #endregion
}
