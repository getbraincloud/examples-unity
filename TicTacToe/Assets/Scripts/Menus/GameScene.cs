#region

using UnityEngine;

#endregion

public class GameScene : MonoBehaviour
{
    public App App;
    
    public void OnEditName()
    {
        editablePlayerName = App.Name;
        isEditingPlayerName = true;
    }

    public void OnGoToLeaderboardScene()
    {
        App.GotoLeaderboardScene(gameObject);
    }

    public void OnGoToAchievementsScene()
    {
        App.GotoAchievementsScene(gameObject);
    }

    public void OnGoToMatchSelectScene()
    {
        App.GotoMatchSelectScene(gameObject);
    }

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
        
        if(GetType() != typeof(Leaderboard))
            if (GUILayout.Button("Leaderboard", GUILayout.MinWidth(50))) OnGoToLeaderboardScene();

        if(GetType() != typeof(Achievements))
            if (GUILayout.Button("Achievements", GUILayout.MinWidth(50))) OnGoToAchievementsScene();
        
        if(GetType() != typeof(MatchSelect))
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