using UnityEngine;
using System.Collections;
using LitJson;
using System.Collections.Generic;

public class GamePicker : MonoBehaviour {

	private Vector2 m_scrollPos;

	public class FacebookFriend
	{
		public string name;
		public string facebookId;
		public string playerId;
		public string picUrl;
		public Texture2D pic;
	}

	public class MatchInfo
	{
		public bool yourTurn;
		public TicTacToe.PlayerInfo playerXInfo = new TicTacToe.PlayerInfo();
		public TicTacToe.PlayerInfo playerOInfo = new TicTacToe.PlayerInfo();
		public string yourToken;
		public string ownerId;
		public string matchId;
		public FacebookFriend fbFriend;
		public int version;

		public MatchInfo(JsonData jsonMatch)
		{
			version = (int)jsonMatch["version"];
			ownerId = (string)jsonMatch["ownerId"];
			matchId = (string)jsonMatch["matchId"];
			yourTurn = ((string)jsonMatch["status"]["currentPlayer"] == FacebookLogin.PlayerId);

			// Load player info
			LoadPlayerInfo(jsonMatch["summary"]["players"][0]);
			LoadPlayerInfo(jsonMatch["summary"]["players"][1]);
		}

		private void LoadPlayerInfo(JsonData playerData)
		{
			string token = (string)playerData["token"];
			TicTacToe.PlayerInfo playerInfo;
			if (token == "X") playerInfo = playerXInfo;
			else playerInfo = playerOInfo;

			if ((string)playerData["playerId"] == FacebookLogin.PlayerId)
			{
				playerInfo.name = FacebookLogin.PlayerName;
				playerInfo.picUrl = FacebookLogin.PlayerPicUrl;
				yourToken = token;
			}
			else 
			{
				// Find your friend in your facebook list
				foreach (FacebookFriend _fbFriend in m_fbFriends)
				{
					if (_fbFriend.playerId == (string)playerData["playerId"])
					{
						fbFriend = _fbFriend;
						break;
					}
				}
				playerInfo.name = fbFriend.name;
				playerInfo.picUrl = fbFriend.picUrl;
			}
		}
	}

	enum eState
	{
		LOADING,
		GAME_PICKER,
		NEW_GAME,
		STARTING_MATCH
	}
	private eState m_state = eState.LOADING;
	static private List<FacebookFriend> m_fbFriends = new List<FacebookFriend>();
	private List<MatchInfo> m_matches = new List<MatchInfo>();
	private List<MatchInfo> m_completedMatches = new List<MatchInfo>();

	// Use this for initialization
	void Start () 
	{
		// Get our facebook friends that have played this game also.
        //BrainCloudWrapper.GetBC().FriendService.
        BrainCloudWrapper.GetBC().FriendService.ReadFriendsWithApplication(false, OnReadFriendData, null, null);// ReadFriendData(OnReadFriendData, null, null);
	}

	void OnReadFriendData(string responseData, object cbPostObject) 
	{
		m_fbFriends.Clear();

		// Construct our friend list using response data
		JsonData jsonFriends = JsonMapper.ToObject(responseData)["data"]["friends"];
		for (int i = 0; i < jsonFriends.Count; ++i)
		{
			JsonData jsonFriend = jsonFriends[i];
			FacebookFriend fbFriend = new FacebookFriend();

			fbFriend.playerId = (string)jsonFriend["playerId"];
			fbFriend.facebookId = (string)jsonFriend["externalData"]["Facebook"]["externalId"];
			fbFriend.name = (string)jsonFriend["externalData"]["Facebook"]["name"];
			StartCoroutine(SetProfilePic((string)jsonFriend["externalData"]["Facebook"]["pictureUrl"], fbFriend)); // Load pic

			m_fbFriends.Add(fbFriend);
		}

		// After, fetch our game list from Braincloud
		BrainCloudWrapper.GetBC().GetAsyncMatchService().FindMatches(OnFindMatchesSuccess, null, null);
	}

	void OnFindMatchesSuccess(string responseData, object cbPostObject)
	{
		m_matches.Clear ();

		// Construct our game list using response data
		JsonData jsonMatches = JsonMapper.ToObject(responseData)["data"]["results"];
		for (int i = 0; i < jsonMatches.Count; ++i)
		{
			JsonData jsonMatch = jsonMatches[i];
			
			MatchInfo match = new MatchInfo(jsonMatch);
			m_matches.Add(match);
		}

		// Now, find completed matches so the user can go see the history
		BrainCloudWrapper.GetBC().GetAsyncMatchService().FindCompleteMatches(OnFindCompletedMatches, null, null);
	}

	void OnFindCompletedMatches(string responseData, object cbPostObject)
	{
		m_completedMatches.Clear ();
		
		// Construct our game list using response data
		JsonData jsonMatches = JsonMapper.ToObject(responseData)["data"]["results"];
		for (int i = 0; i < jsonMatches.Count; ++i)
		{
			JsonData jsonMatch = jsonMatches[i];
			MatchInfo match = new MatchInfo(jsonMatch);
			m_completedMatches.Add(match);
		}

		m_state = eState.GAME_PICKER;
	}

	// Update is called once per frame
	void Update ()
	{
	
	}

	void OnGUI()
	{
		switch (m_state)
		{
		case eState.LOADING:
		case eState.STARTING_MATCH:
		{
			GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			
			GUILayout.Label("Loading...");
			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.EndArea();
			break;
		}
		case eState.GAME_PICKER:
		{
			GUILayout.Window(0, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 250, 300, 500), OnPickGameWindow, "Pick Game");
			break;
		}
		case eState.NEW_GAME:
		{
			GUILayout.Window(0, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 250, 300, 500), OnNewGameWindow, "Pick Opponent");
			break;
		}
		}
	}

	void OnPickGameWindow(int windowId)
	{
		GUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();
		GUILayout.BeginVertical ();

		m_scrollPos = GUILayout.BeginScrollView(m_scrollPos, false, false);

		if (GUILayout.Button ("+ New Game", GUILayout.MinHeight (50), GUILayout.MaxWidth (250))) 
		{
			m_state = eState.NEW_GAME;
		}
		foreach (MatchInfo match in m_matches)
		{
			GUILayout.Space(10);
			GUILayout.BeginHorizontal ();
			GUILayout.Box(match.fbFriend.pic, GUILayout.Height(50), GUILayout.Width(50));
			GUI.enabled = match.yourTurn;
			if (GUILayout.Button (match.fbFriend.name + "\n" + (match.yourTurn ? "(Your Turn)" : "(His Turn)" ), GUILayout.MinHeight (50), GUILayout.MaxWidth (200))) 
			{
				// Go in the match
				EnterMatch(match);
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal ();
		}
		GUILayout.Space(10);
		foreach (MatchInfo match in m_completedMatches)
		{
			GUILayout.Space(10);
			GUILayout.BeginHorizontal ();
			GUILayout.Box(match.fbFriend.pic, GUILayout.Height(50), GUILayout.Width(50));
			if (GUILayout.Button (match.fbFriend.name + "\n(Completed)", GUILayout.MinHeight (50), GUILayout.MaxWidth (200))) 
			{
				// Go in the match
				EnterMatch(match);
			}
			GUILayout.EndHorizontal ();
		}

		GUILayout.EndScrollView();
		
		GUILayout.EndVertical ();
		GUILayout.FlexibleSpace ();
		GUILayout.EndHorizontal ();
	}
	
	void OnNewGameWindow(int windowId)
	{
		GUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();
		GUILayout.BeginVertical ();
		
		m_scrollPos = GUILayout.BeginScrollView(m_scrollPos, false, false);

		if (GUILayout.Button ("<- Cancel", GUILayout.MinHeight (32), GUILayout.MaxWidth (75))) 
		{
			m_state = eState.GAME_PICKER;
		}

		foreach (FacebookFriend fbFriend in m_fbFriends)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Box(fbFriend.pic, GUILayout.Height(50), GUILayout.Width(50));
			if (GUILayout.Button (fbFriend.name, GUILayout.MinHeight (50), GUILayout.MaxWidth (200))) 
			{
				OnPickOpponent(fbFriend);
			}
			GUILayout.EndHorizontal ();
		}
		
		GUILayout.EndScrollView();
		
		GUILayout.EndVertical ();
		GUILayout.FlexibleSpace ();
		GUILayout.EndHorizontal ();
	}

	void OnPickOpponent(FacebookFriend fbFriend)
	{
		m_state = eState.STARTING_MATCH;
		bool yourTurnFirst = Random.Range(0, 100) < 50;

		// Setup our summary data. This is what we see when we query
		// the list of games. So we want to store information about our
		// facebook ids so we can lookup our friend's picture and name
		JsonData summaryData = new JsonData();
		summaryData["players"] = new JsonData();
		{
			// Us
			JsonData playerData = new JsonData();
			playerData["playerId"] = FacebookLogin.PlayerId;
			playerData["facebookId"] = FB.UserId;
			if (yourTurnFirst)
				playerData["token"] = "X"; // First player has X
			else
				playerData["token"] = "O";
			summaryData["players"].Add(playerData);
		}
		{
			// Our friend
			JsonData playerData = new JsonData();
			playerData["playerId"] = fbFriend.playerId;
			playerData["facebookId"] = fbFriend.facebookId;//fbFriend.facebookId;
			if (!yourTurnFirst)
				playerData["token"] = "X"; // First player has X
			else
				playerData["token"] = "O";
			summaryData["players"].Add(playerData);
		}

		// Setup our match State. We only store where Os and Xs are in
		// the tic tac toe board. 
		JsonData matchState = new JsonData();
		matchState["board"] = "#########"; // Empty the board. # = nothing, O,X = tokens

		// Setup our opponent list. In this case, we have just one opponent.
		//JsonData opponentIds = new JsonData();

		// Create the match
		BrainCloudWrapper.GetBC().AsyncMatchService.CreateMatchWithInitialTurn(
			"[{\"platform\":\"BC\",\"id\":\"" + fbFriend.playerId + "\"}]", 	// Opponents
			matchState.ToJson(),	// Current match state
			"A friend has challenged you to a match of Tic Tac Toe.",	// Push notification Message
			null,	// Match id. Keep empty, we let brainCloud generate one
			(yourTurnFirst) ? FacebookLogin.PlayerId : fbFriend.playerId, // Which turn it is. We picked randomly
			summaryData.ToJson(),	// Summary data
			OnCreateMatchSuccess,
			OnCreateMatchFailed,
			null);
	}

	void OnCreateMatchSuccess(string responseData, object cbPostObject)
	{
		JsonData data = JsonMapper.ToObject(responseData);
		MatchInfo match = new MatchInfo(data["data"]);

		// Go to the game if it's your turn
		if (match.yourTurn)
		{
			EnterMatch(match);
		}
		else
		{
			Application.LoadLevel("GamePicker");
		}
	}
	
	void OnCreateMatchFailed(int a, int b, string responseData, object cbPostObject)
	{
		Debug.LogError("Failed to create Async Match");
        Debug.Log(a);
        Debug.Log(b);
        Debug.Log(responseData);
		m_state = eState.GAME_PICKER; // Just go back to game selection
	}

	void EnterMatch(MatchInfo match)
	{
		m_state = eState.LOADING;

		// Query more detail state about the match
		BrainCloudWrapper.GetBC().AsyncMatchService.ReadMatch(match.ownerId, match.matchId, OnReadMatch, OnReadMatchFailed, match);
	}

	void OnReadMatch(string responseData, object cbPostObject)
	{
		MatchInfo match = cbPostObject as MatchInfo;
		JsonData data = JsonMapper.ToObject(responseData)["data"];

		// Setup a couple stuff into our TicTacToe scene
		TicTacToe.boardState = (string)data["matchState"]["board"];
		TicTacToe.playerInfoX = match.playerXInfo;
		TicTacToe.playerInfoO = match.playerOInfo;
		TicTacToe.whosTurn = match.yourToken == "X" ? TicTacToe.playerInfoX : match.playerOInfo;
		TicTacToe.ownerId = match.ownerId;
		TicTacToe.matchId = match.matchId;
		TicTacToe.matchVersion = (ulong)match.version;
		
		// Load the Tic Tac Toe scene
		Application.LoadLevel("TicTacToe");
	}

	void OnReadMatchFailed(int a, int b, string responseData, object cbPostObject)
	{
		Debug.LogError("Failed to Read Match");
	}

	IEnumerator SetProfilePic(string url, FacebookFriend fbFriend)
	{
		fbFriend.picUrl = url;
		WWW www = new WWW(url);
		yield return www;
		fbFriend.pic = www.texture;
	}
}
