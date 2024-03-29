using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameButtonCell : MonoBehaviour
{
    public const string DEFAULT_NAME = "DEFAULT NAME";

    [SerializeField] public TextMeshProUGUI OpponentName;
    [SerializeField] public TextMeshProUGUI Status;
    [SerializeField] public GameObject CloseButton = null;

    #region public
    public virtual void Init(MatchSelect.MatchInfo in_pData, MatchSelect in_pMatchSelect)
    {
        CloseButton.SetActive(true);
        m_pMatchData = in_pData;
        m_pMatchSelect = in_pMatchSelect;
        UpdateUI();
    }

    public virtual void Init(PlayerInfo in_pData, MatchSelect in_pMatchSelect)
    {
        CloseButton.SetActive(false);
        m_pPlayerData = in_pData;
        m_pMatchSelect = in_pMatchSelect;
        UpdateUI();
    }

    public virtual void UpdateUI()
    {
        if (m_pPlayerData != null)
        {
            OpponentName.text = m_pPlayerData.PlayerName;
            Status.gameObject.SetActive(false);
        }
        if (m_pMatchData != null)
        {
            GetComponent<Button>().interactable = m_pMatchData.yourTurn || m_pMatchData.complete;
            OpponentName.text = m_pMatchData.matchedProfile.PlayerName;
            Status.gameObject.SetActive(true);
            Status.text = m_pMatchData.complete ? "(COMPLETE)" : m_pMatchData.expired ? "(ABANDONNED)" : m_pMatchData.yourTurn ? "(Your Turn)" : "(Opponent's Turn)";
        }
    }

    public void OnCellSelected()
    {
        if (m_pMatchData != null)
        {
            m_pMatchSelect.OnMatchSelected(m_pMatchData, this);
        }
        if (m_pPlayerData != null)
        {
            m_pMatchSelect.OnPlayerSelected(m_pPlayerData);
        }
    }

    public void OnAbandonMatchButton()
    {
        // However, we are using a custom FINISH_RANK_MATCH script which is set up on brainCloud. View the commented Cloud Code script below
        var matchResults = new Dictionary<string, object>() { { "ownerId", m_pMatchData.ownerId },
                                                              { "matchId", m_pMatchData.matchId },
                                                              { "abandonnedId", m_pMatchSelect.App.ProfileId },
                                                              { "version", m_pMatchData.version },
                                                              { "isTie", false }, };

        m_pMatchSelect.App.Bc.ScriptService.RunScript("RankGame_FinishMatch", JsonWriter.Serialize(matchResults), OnAbandonMatchSuccess,
            (status, code, error, cbObject) => { });
    }

    private void OnAbandonMatchSuccess(string responseData, object cbPostObject)
    {
        // Go back to game select scene
        m_pMatchSelect.App.GotoMatchSelectScene(m_pMatchSelect.gameObject);
    }
    #endregion

    #region private
    private PlayerInfo m_pPlayerData = null;
    private MatchSelect.MatchInfo m_pMatchData = null;
    private MatchSelect m_pMatchSelect = null;

    public MatchSelect.MatchInfo MatchInfo
    {
        get => m_pMatchData;
    }
    #endregion
}
