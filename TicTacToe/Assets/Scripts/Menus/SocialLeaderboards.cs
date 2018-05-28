#region

using System.Collections;
using LitJson;
using UnityEngine;

#endregion

// Currently NOT USED in this example.
// Social Leaderboards use Facebook friends to populate the leaderboard.

public class SocialLeaderboards : BestScores
{
    public override void postNewScore(int in_score)
    {
        _newScore = in_score;
        _newScoreIndex = -1; // -1 means it's not in the top 10
        Spinner.gameObject.SetActive(true);

        foreach (var entry in EntriesText) entry.text = "";

        // Post score to social leaderboard. The callback should have the results
        App.Bc.SocialLeaderboardService.PostScoreToLeaderboard(
            "spacegame_highscores", // Leaderboard Id
            in_score, // Score
            "{}", // Optional extra data
            OnPostScoreSuccess, // Success callback
            OnPostScoreFailed, // Failed callback
            null);
    }

    private void OnPostScoreSuccess(string postResponseData, object cbPostObject)
    {
        // Now we fetch the leaderboards to see were we stand
        App.Bc.SocialLeaderboardService.GetSocialLeaderboard(
            "spacegame_highscores", // Leaderboard Id
            true, // If true, your name will be replaced with "You"
            OnGetLeaderboardsSuccess, // Success callback
            OnGetLeaderboardsFailed); // Failed callback
    }

    private void OnPostScoreFailed(int a, int b, string postResponseData, object cbPostObject)
    {
        Debug.LogError("Failed to Post score");
        Spinner.gameObject.SetActive(false); // Hide spinner
    }

    private void OnGetLeaderboardsSuccess(string responseData, object cbObject)
    {
        // Parse the json
        var jsonData = JsonMapper.ToObject(responseData);

        // Just show the 10 best. If you're not in there, don't show it.
        // For a real project, you might want to shift to your player
        // position and show serounding players.
        var entries = jsonData["data"]["social_leaderboard"];
        for (var i = 0; i < entries.Count && i < 10; ++i)
        {
            var entry = entries[i];
            var score = 0;
            var name = entry["name"].ToString();
            var pictureUrl = entry["pictureUrl"].ToString();
            if (entry["score"] != null) score = int.Parse(entry["score"].ToString());

            EntriesText[i].text = i + 1 + ". " + name + ", " + score;

            // Set it's profile picture
            var profilePic = EntriesText[i].transform.Find("ProfilePic").gameObject.GetComponent<GUITexture>();
            StartCoroutine(setProfilePic(pictureUrl, profilePic));
        }

        // Stop the spinner
        Spinner.gameObject.SetActive(false);
    }

    private void OnGetLeaderboardsFailed(int a, int b, string responseData, object cbObject)
    {
        Debug.LogError("Failed to get Leaderboards");
        Spinner.gameObject.SetActive(false); // Hide spinner
    }

    private IEnumerator setProfilePic(string url, GUITexture profilePic)
    {
        var www = new WWW(url);
        yield return www;
        profilePic.texture = www.texture;
    }
}