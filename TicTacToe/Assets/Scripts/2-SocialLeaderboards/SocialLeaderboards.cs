using UnityEngine;
using System.Collections;
using BrainCloud.Entity;
using System.Collections.Generic;
using LitJson;

public class SocialLeaderboards : BestScores {

	// Use this for initialization
	void Start () 
	{
	
	}

	public override void postNewScore(int in_score)
	{
		m_newScore = in_score;
		m_newScoreIndex = -1; // -1 means it's not in the top 10
		spinner.gameObject.SetActive(true);

		foreach (GUIText entry in entriesText)
		{
			entry.text = "";
		}

		// Post score to social leaderboard. The callback should have the results
		BrainCloudWrapper.GetBC().SocialLeaderboardService.PostScoreToLeaderboard(
			"spacegame_highscores", 	// Leaderboard Id
			in_score, 			// Score
			"{}",						// Optional extra data
			OnPostScoreSuccess,			// Success callback
			OnPostScoreFailed, 			// Failed callback
			null);
	}

	void OnPostScoreSuccess(string postResponseData, object cbPostObject)
	{
		// Now we fetch the leaderboards to see were we stand
		BrainCloudWrapper.GetBC().SocialLeaderboardService.GetLeaderboard(
			"spacegame_highscores",		// Leaderboard Id
			true, 						// If true, your name will be replaced with "You"
			OnGetLeaderboardsSuccess,	// Success callback
			OnGetLeaderboardsFailed);	// Failed callback
			
	}

	void OnPostScoreFailed(int a, int b, string postResponseData, object cbPostObject)
	{
		Debug.LogError("Failed to Post score");
		spinner.gameObject.SetActive(false); // Hide spinner
	}

	void OnGetLeaderboardsSuccess(string responseData, object cbObject)
	{
		// Parse the json
		JsonData jsonData = JsonMapper.ToObject (responseData);
		
		// Just show the 10 best. If you're not in there, don't show it.
		// For a real project, you might want to shift to your player
		// position and show serounding players.
		JsonData entries = jsonData["data"]["social_leaderboard"];
		for (int i = 0; i < entries.Count && i < 10; ++i)
		{
			JsonData entry = entries[i];
			int score = 0;
			string name = entry["name"].ToString();
			string pictureUrl = entry["pictureUrl"].ToString();
			if(entry["score"] != null)
				score = int.Parse(entry["score"].ToString());
			
			entriesText[i].text = (i + 1) + ". " + name + ", " + score;
			
			// Set it's profile picture
			GUITexture profilePic = entriesText[i].transform.FindChild("ProfilePic").gameObject.GetComponent<GUITexture>();
			StartCoroutine(setProfilePic (pictureUrl, profilePic));
		}
		
		// Stop the spinner
		spinner.gameObject.SetActive(false);
	}

	void OnGetLeaderboardsFailed(int a, int b, string responseData, object cbObject)
	{
		Debug.LogError("Failed to get Leaderboards");
		spinner.gameObject.SetActive(false); // Hide spinner
	}

	IEnumerator setProfilePic(string url, GUITexture profilePic)
	{
		WWW www = new WWW(url);
		yield return www;
		profilePic.texture = www.texture;
	}
}
