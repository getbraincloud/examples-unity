#region
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#endregion

public class GameScene : MonoBehaviour
{
    public App App;

    [SerializeField] public TMP_InputField UserName;
    [SerializeField] public Image UserNameBG;

    [SerializeField]
    private GameObject AchievementsTab = null;
    [SerializeField]
    private GameObject LeaderboardTab = null;
    [SerializeField]
    private GameObject MyGamesTab = null;

    [SerializeField]
    public MatchSelect MatchSelectObj = null;
    [SerializeField]
    public Achievements AchievementsObj = null;
    [SerializeField]
    public Leaderboard LeaderboardObj = null;

    public void OnEditName()
    {
        UserName.interactable = true;
        UserName.Select();
    }

    public void OnGoToLeaderboardScene()
    {
        AchievementsTab.gameObject.SetActive(false);
        LeaderboardTab.gameObject.SetActive(true);
        MyGamesTab.gameObject.SetActive(false);
        LeaderboardObj.OnUpdateUI();
    }

    public void OnGoToAchievementsScene()
    {
        AchievementsTab.gameObject.SetActive(true);
        LeaderboardTab.gameObject.SetActive(false);
        MyGamesTab.gameObject.SetActive(false);
        AchievementsObj.OnUpdateUI();
    }

    public void OnGoToMatchSelectScene()
    {
        AchievementsTab.gameObject.SetActive(false);
        LeaderboardTab.gameObject.SetActive(false);
        MyGamesTab.gameObject.SetActive(true);
    }

    public void OnEndEditName(string str)
    {
        UserName.text = UserName.text.Trim();
        App.Name = UserName.text;

        App.Bc.PlayerStateService.UpdateName(App.Name,
            (response, cbObject) => { },
            (status, code, error, cbObject) => { Debug.Log("Failed to change Player Name"); });
    }

    public void OnLogout()
    {
        App.Bc.PlayerStateService.Logout((response, cbObject) => { App.GotoLoginScene(gameObject); });
        PlayerPrefs.SetString(App.WrapperName + "_hasAuthenticated", "false");
    }
}