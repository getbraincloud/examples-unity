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

    private void Update()
    {
        if (SkillRating != null)
            SkillRating.text = "Skill Rating: " + App.PlayerRating;
    }

    public void OnEditName()
    {
        editablePlayerName = App.Name;
        isEditingPlayerName = true;
        UserName.interactable = true;
        UserNameBG.gameObject.SetActive(true);
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

    // TODO: Get rid of this when the new Achievements and Leaderboard screens are updated.
    protected void OnPlayerInfoWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        if (!isEditingPlayerName)
        {
            GUILayout.Label(string.Format("PlayerName: {0}", App.Name), GUILayout.MinWidth(200));
            if (GUILayout.Button("Edit", GUILayout.MinWidth(50)))
            {
                OnEditName();
            }
        }
        else
        {
            editablePlayerName = GUILayout.TextField(editablePlayerName, GUILayout.MinWidth(200));
            if (GUILayout.Button("Save", GUILayout.MinWidth(50)))
            {
                App.Name = editablePlayerName;
                isEditingPlayerName = false;

                App.Bc.PlayerStateService.UpdateUserName(App.Name,
                    (response, cbObject) => { },
                    (status, code, error, cbObject) => { Debug.Log("Failed to change Player Name"); });
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Label(string.Format("PlayerRating: {0}", App.PlayerRating), GUILayout.MinWidth(200));

        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical();

        if (GetType() != typeof(Leaderboard))
            if (GUILayout.Button("Leaderboard", GUILayout.MinWidth(50))) OnGoToLeaderboardScene();

        if (GetType() != typeof(Achievements))
            if (GUILayout.Button("Achievements", GUILayout.MinWidth(50))) OnGoToAchievementsScene();

        if (GetType() != typeof(MatchSelect))
            if (GUILayout.Button("MatchSelect", GUILayout.MinWidth(50))) OnGoToMatchSelectScene();

        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
    }

    private string editablePlayerName = "";
    private bool isEditingPlayerName;
}