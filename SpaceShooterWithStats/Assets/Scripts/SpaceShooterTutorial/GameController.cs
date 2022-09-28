using UnityEngine;
using UnityEngine.UI;
using System;
using BrainCloud.LitJson;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
	////////////////////////////////////////////
	// BrainCloud integration code
	////////////////////////////////////////////

	private void ReadStatistics()
	{
		// Ask brainCloud for statistics
		App.Bc.PlayerStatisticsService.ReadAllUserStats(StatsSuccess_Callback, StatsFailure_Callback, null);

		brainCloudStatusText.text = "Reading statistics from brainCloud...";
		brainCloudStatusText.gameObject.SetActive(true);
	}

	private void SaveStatisticsToBrainCloud()
	{
		// Build the statistics name/inc value dictionary
		Dictionary<string, object> stats = new Dictionary<string, object> {
			{"enemyShipsKilled", m_enemiesKilledThisRound},
			{"asteroidsDestroyed", m_asteroidsDestroyedThisRound},
			{"shotsFired", m_shotsFiredThisRound},
			{"gamesPlayed", 1}
		};

		// Send to the cloud
		App.Bc.PlayerStatisticsService.IncrementUserStats(
			stats, StatsSuccess_Callback, StatsFailure_Callback, null);

		brainCloudStatusText.text = "Incrementing statistics on brainCloud...";
		brainCloudStatusText.gameObject.SetActive(true);
	}

	void addScoreToLeaderBoard()
	{
		//sending to the cloud
		App.Bc.SocialLeaderboardService.PostScoreToLeaderboard("Leaderboard", m_score, "{ \"" + "username" + "\" : \"" + PlayerPrefs.GetString("username") + "\"}");
	}

	void ReadLeaderBoard()
	{
		App.Bc.SocialLeaderboardService.GetGlobalLeaderboardView("Leaderboard", BrainCloud.BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, 2, 2, LeaderBoardSuccess_Callback, StatsFailure_Callback);
	}

	void DisplayTopLeaderBoardScores(string data)
	{
		//displaying the top 5 scores from users on the leaderboard
		int rank;
		int score;
		string name;
		// Read the json to display nearby leaderboard rankings
		JsonData jsonData = JsonMapper.ToObject(data);
		JsonData[] myPlayer = new JsonData[5];

		for (int i = 0; i < 5; i++)
		{
			if (jsonData["data"]["leaderboard"].Count == i)
				break;

			myPlayer[i] = jsonData["data"]["leaderboard"][i];
			rank = int.Parse(myPlayer[i]["rank"].ToString());
			score = int.Parse(myPlayer[i]["score"].ToString());
			name = myPlayer[i]["data"]["username"].ToString();
			//name = "test";

			scoreArray[i].gameObject.SetActive(true);
			scoreArray[i].text = "Rank: " + rank + " Username: " + name + " Score: " + score;
		}
	}

	private void DisplayPlayerScores(string response)
	{
		//displaying users own personal best 5 scores
		int score;
		JsonData jsonData = JsonMapper.ToObject(response);
		JsonData player = jsonData["data"]["scores"];

		//.size?
		for (int i = 0; i < 5; i++)
		{
			if (player.Count == i)
				break;

			score = int.Parse(player[i]["score"].ToString());
			scoreArray[i].gameObject.SetActive(true);
			scoreArray[i].text = i + 1 + ": " + "Score: " + score;
		}
	}

	private void ReadPersonalHighestScores()
	{
		//gets the players personal top scores
		App.Bc.SocialLeaderboardService.GetPlayerScores("Leaderboard", -1, 5, GetPlayerScoresSuccess_Callback, StatsFailure_Callback);
	}

	private void StatsSuccess_Callback(string responseData, object cbObject)
	{
		// Read the json and update our values
		JsonData jsonData = JsonMapper.ToObject (responseData);
		JsonData entries = jsonData["data"]["statistics"];

		m_statEnemiesKilled = int.Parse(entries["enemyShipsKilled"].ToString());
		m_statAsteroidsDestroyed = int.Parse(entries["asteroidsDestroyed"].ToString());
		m_statShotsFired = int.Parse(entries["shotsFired"].ToString());
		m_statGamesPlayed = int.Parse(entries["gamesPlayed"].ToString());

		ShowStatistics();

		if (brainCloudStatusText)
		{
			brainCloudStatusText.text = "Sync'd with brainCloud";
		}
	}

	private void StatsFailure_Callback(int statusCode, int reasonCode, string statusMessage, object cbObject)
	{
		if (brainCloudStatusText)
		{
			brainCloudStatusText.gameObject.SetActive(true);
			brainCloudStatusText.text = "Failed to increment stats on brainCloud";
		}
		Debug.Log (statusMessage);
	}
	////////////////////////////////////////////
	private void Success_Callback(string responseData, object cbObject)
	{
		Debug.Log(responseData);
	}

	//
	private void Fail_Callback(int statusCode, int reasonCode, string statusMessage, object cbObject)
	{
		Debug.Log(statusMessage);
	}
	////////////////////////////////////////////

	private void LeaderBoardSuccess_Callback(string responseData, object cbObject)
	{
		DisplayTopLeaderBoardScores(responseData);
	}

	private void GetPlayerScoresSuccess_Callback(string responseData, object cbObject)
	{
		DisplayPlayerScores(responseData);
	}

	private void LeaderBoardFailure_Callback(int statusCode, int reasonCode, string statusMessage, object cbObject)
	{
		if (brainCloudStatusText)
		{
			brainCloudStatusText.text = "Failed to get leaderboard from brainCloud";
		}
		Debug.Log(statusMessage);
	}
	////////////////////////////////////////////


	// Prefabs
	public GameObject player;
	public GameObject[] hazards;

	// Game Parameters
	public Vector3 spawnValues;
	public int hazardCount;
	public float spawnWait;
	public float startWait;
	public float waveWait;
	public float gameOverWait;

	// Screen text objects
	public Text scoreText;
	public Text restartText;
	public Text gameOverText;
	public Text clickToStartText;
	public Text brainCloudStatusText;
	public Text enemiesKilledText;
	public Text asteroidsDestroyedText;
	public Text accuracyText;
	public Text shotsFiredText;
	public Text gamesPlayedText;
	public Text[] scoreArray;

	// States
	private enum eGameState
	{
		GAME_STATE_START_SCREEN,
		GAME_STATE_PLAYING,
		GAME_STATE_GAME_OVER,
		GAME_STATE_SCORE_SCREEN,
		GAME_STATE_LEADERBOARD_SCREEN,
		GAME_STATE_BEST_SCORES_SCREEN,
		GAME_STATE_WAITING_FOR_BRAINCLOUD
	}
	private eGameState m_state = eGameState.GAME_STATE_START_SCREEN;
	private enum ePlayState
	{
		PLAY_STATE_STARTUP,
		PLAY_STATE_WAVE,
		PLAY_STATE_IN_BETWEEN_WAVES
	}
	private ePlayState m_playState = ePlayState.PLAY_STATE_STARTUP;


	// our per round values
	private int m_enemiesKilledThisRound = 0;
	private int m_asteroidsDestroyedThisRound = 0;
	private int m_shotsFiredThisRound = 0;

	// our statistics
	private int m_statEnemiesKilled = 0;
	private int m_statAsteroidsDestroyed = 0;
	private int m_statShotsFired = 0;
	private int m_statGamesPlayed = 0;

	// other game vars
	private int m_score = 0;
	private int m_hazardSpawned = 0;

	// calculated on the fly
	private double m_accuracy = 0;

	// Timers
	private float m_startupTime;
	private float m_spawnTime;
	private float m_gameOverTime;
	private float m_scoreTime;

	void Start ()
	{
		if (!App.Bc.Client.IsAuthenticated())
		{
			SceneManager.LoadScene("BrainCloudConnect");
		}
		else
		{
			UpdateScoreText();
			ReadStatistics();
		}
	}

	void Update ()
	{
		switch (m_state)
		{
		case eGameState.GAME_STATE_START_SCREEN:
			if (Input.GetMouseButtonDown(0))
			{
				StartRound();
			}
			break;
		case eGameState.GAME_STATE_PLAYING:
			switch (m_playState)
			{
			case ePlayState.PLAY_STATE_STARTUP:
			case ePlayState.PLAY_STATE_IN_BETWEEN_WAVES:
				m_startupTime -= Time.deltaTime;
				if (m_startupTime <= 0)
				{
					StartWave();
				}
				break;
			case ePlayState.PLAY_STATE_WAVE:
				m_spawnTime -= Time.deltaTime;
				if (m_spawnTime <= 0)
				{
					if (m_hazardSpawned >= hazardCount)
					{
						// We have done the wave.
						m_playState = ePlayState.PLAY_STATE_IN_BETWEEN_WAVES;
						m_startupTime = waveWait;
					}
					else
					{
						m_hazardSpawned++;
						m_spawnTime = spawnWait;
						SpawnHazard();
					}
				}
				break;
			}
			break;
		case eGameState.GAME_STATE_GAME_OVER:
			m_gameOverTime -= Time.deltaTime;
			if (m_gameOverTime <= 0)
			{
				m_state = eGameState.GAME_STATE_SCORE_SCREEN;
				scoreText.gameObject.SetActive(false);
				gameOverText.gameObject.SetActive(false);

				clickToStartText.text = "Click to see your best scores";
				clickToStartText.gameObject.SetActive(true);

				SaveStatisticsToBrainCloud();
			}
			break;
		case eGameState.GAME_STATE_SCORE_SCREEN:
			if (Input.GetMouseButtonDown(0))
			{
				m_state = eGameState.GAME_STATE_BEST_SCORES_SCREEN;
				DisableText();
				addScoreToLeaderBoard();
				clickToStartText.gameObject.SetActive(true);
				clickToStartText.text = "Click to see leaderboard";
				ReadPersonalHighestScores();
			}
			break;
		case eGameState.GAME_STATE_BEST_SCORES_SCREEN:
			if (Input.GetMouseButtonDown(0))
			{
				m_state = eGameState.GAME_STATE_LEADERBOARD_SCREEN;
				DisableText();
				clickToStartText.gameObject.SetActive(true);
				clickToStartText.text = "Click to restart";

				ReadLeaderBoard();
			}
			break;
		case eGameState.GAME_STATE_LEADERBOARD_SCREEN:
			if (Input.GetMouseButtonDown(0))
			{	
				StartRound();
			}
			break;
		}
	}

	void StartRound()
	{
		scoreText.gameObject.SetActive(true);
		DisableText();

		m_enemiesKilledThisRound = 0;
		m_asteroidsDestroyedThisRound = 0;
		m_shotsFiredThisRound = 0;

		// don't reset this every round... m_totalGamesPlayed = 0;

		m_accuracy = 0;
		m_score = 0;

		UpdateScoreText ();
		m_state = eGameState.GAME_STATE_PLAYING;
		m_playState = ePlayState.PLAY_STATE_STARTUP;
		m_startupTime = startWait;

		SpawnPlayer();
	}

	void StartWave()
	{
		m_hazardSpawned = 1;
		m_spawnTime = spawnWait;
		m_playState = ePlayState.PLAY_STATE_WAVE;
		SpawnHazard();
	}

	void SpawnPlayer()
	{
		GameObject.Instantiate (player, Vector3.zero, Quaternion.identity);
	}

	void SpawnHazard()
	{
		GameObject hazard = hazards [UnityEngine.Random.Range (0, hazards.Length)];
		Vector3 spawnPosition = new Vector3 (UnityEngine.Random.Range (-spawnValues.x, spawnValues.x), spawnValues.y, spawnValues.z);
		Quaternion spawnRotation = Quaternion.identity;
		Instantiate (hazard, spawnPosition, spawnRotation);
	}

	public void AddScore (int newScoreValue)
	{
		m_score += newScoreValue;
		UpdateScoreText();
	}

	void UpdateScoreText()
	{
		scoreText.text = "Score: " + m_score;
	}

	public void GameOver()
	{
		gameOverText.gameObject.SetActive(true);
		m_state = eGameState.GAME_STATE_GAME_OVER;
		m_gameOverTime = gameOverWait;
	}

	public void OnEnemyKilled()
	{
		m_enemiesKilledThisRound++;
	}

	public void OnAsteroidDestroyed()
	{
		m_asteroidsDestroyedThisRound++;
	}

	public void OnShotFired()
	{
		m_shotsFiredThisRound++;
	}

	private void SaveStatisticsLocally()
	{
		m_statEnemiesKilled += m_enemiesKilledThisRound;
		m_statAsteroidsDestroyed += m_asteroidsDestroyedThisRound;
		m_statShotsFired += m_shotsFiredThisRound;
		m_statGamesPlayed += 1;

		ShowStatistics();
	}

	private void ShowStatistics()
	{
		enemiesKilledText.text = "Enemies Killed: " + m_statEnemiesKilled;
		enemiesKilledText.gameObject.SetActive(true);

		asteroidsDestroyedText.text = "Asteroids Destroyed: " + m_statAsteroidsDestroyed;
		asteroidsDestroyedText.gameObject.SetActive(true);

		shotsFiredText.text = "Shots Fired: " + m_statShotsFired;
		shotsFiredText.gameObject.SetActive(true);

		m_accuracy = (m_statShotsFired == 0) ? 0 : (m_statEnemiesKilled + m_statAsteroidsDestroyed) / (double) m_statShotsFired * 100.0d;
		accuracyText.text = String.Format("Accuracy: {0:0.00}%", m_accuracy);
		accuracyText.gameObject.SetActive(true);

		gamesPlayedText.text = "Games Played: " + m_statGamesPlayed;
		gamesPlayedText.gameObject.SetActive(true);
	}

	void DisableText()
	{
		gameOverText.gameObject.SetActive(false);
		enemiesKilledText.gameObject.SetActive(false);
		asteroidsDestroyedText.gameObject.SetActive(false);
		shotsFiredText.gameObject.SetActive(false);
		accuracyText.gameObject.SetActive(false);
		gamesPlayedText.gameObject.SetActive(false);
		clickToStartText.gameObject.SetActive(false);
		brainCloudStatusText.gameObject.SetActive(false);

		for (int i = 0; i < 5; i++)
		{
			scoreArray[i].gameObject.SetActive(false);
		}
	}
}
