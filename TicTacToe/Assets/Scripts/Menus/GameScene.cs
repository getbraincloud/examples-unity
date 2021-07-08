#region
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#endregion

public class GameScene : MonoBehaviour
{
    public App App;

    [SerializeField] public TMP_InputField UserName;
    [SerializeField] public TextMeshProUGUI SkillRating;
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

    private bool isEditingPlayerName;
    
    private void Update()
    {
        if (SkillRating != null)
            SkillRating.text = "Skill Rating: " + App.PlayerRating;
    }

    public void OnEditName()
    {
        isEditingPlayerName = true;
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
        isEditingPlayerName = false;

        App.Bc.PlayerStateService.UpdateUserName(App.Name,
            (response, cbObject) => { },
            (status, code, error, cbObject) => { Debug.Log("Failed to change Player Name"); });
    }

    public void OnLogout()
    {
        App.Bc.PlayerStateService.Logout((response, cbObject) => { App.GotoLoginScene(gameObject); });
        PlayerPrefs.SetString(App.WrapperName + "_hasAuthenticated", "false");
    }
}