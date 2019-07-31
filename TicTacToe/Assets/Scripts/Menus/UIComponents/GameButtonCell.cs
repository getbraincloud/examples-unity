using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameButtonCell : MonoBehaviour
{
    public const string DEFAULT_NAME = "DEFAULT NAME";

    [SerializeField] public TextMeshProUGUI OpponentName;
    [SerializeField] public TextMeshProUGUI Status;

    #region public
    public virtual void Init(MatchSelect.MatchInfo in_pData, MatchSelect in_pMatchSelect)
    {
        m_pMatchData = in_pData;
        m_pMatchSelect = in_pMatchSelect;
        UpdateUI();
    }

    public virtual void Init(PlayerInfo in_pData, MatchSelect in_pMatchSelect)
    {
        m_pPlayerData = in_pData;
        m_pMatchSelect = in_pMatchSelect;
        UpdateUI();
    }

    public virtual void UpdateUI()
    {
        if (m_pPlayerData != null)
        {
            OpponentName.text = m_pPlayerData.PlayerName + " (" + m_pPlayerData.PlayerRating + ")";
            Status.gameObject.SetActive(false);
        }
        if (m_pMatchData != null)
        {
            this.GetComponent<Button>().interactable = m_pMatchData.yourTurn || m_pMatchData.complete;
            OpponentName.text = m_pMatchData.matchedProfile.PlayerName;
            Status.gameObject.SetActive(true);
            Status.text = m_pMatchData.complete ? "(COMPLETE)" : m_pMatchData.yourTurn ? "(Your Turn)" : "(Opponent's Turn)";
        }
    }

    public void OnCellSelected()
    {
        if (m_pMatchData != null)
        {
            m_pMatchSelect.OnMatchSelected(m_pMatchData);
        }
        if (m_pPlayerData != null)
        {
            m_pMatchSelect.OnPlayerSelected(m_pPlayerData);
        }
    }
    #endregion

    #region private
    private PlayerInfo m_pPlayerData = null;
    private MatchSelect.MatchInfo m_pMatchData = null;
    private MatchSelect m_pMatchSelect = null;
    #endregion
}
