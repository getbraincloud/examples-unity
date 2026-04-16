using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using Managers;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
	public Dropdown trackNameDropdown;
	public Dropdown gameTypeDropdown;
	public Image trackIconImage;
	public PlayerResultItem resultItemPrefab;
	public Transform parent;

	public Button moreAfterButton;
	public Button moreBeforeButton;
	private Dictionary<LeaderboardEntry, PlayerResultItem> ListItems = new Dictionary<LeaderboardEntry, PlayerResultItem>();

	private const int PAGE_SIZE = 10;
	private const int START_OFFSET = 1;
	private void OnEnable()
	{
		trackNameDropdown.onValueChanged.AddListener(x =>
		{
			bool wasUpdated = x != BCManager.LobbyManager.TrackId;
			if (wasUpdated)
			{
				BCManager.LobbyManager.TrackId = x;
				RemoveAllPlayers();
				UpdateUI();
				FetchLeaderboard();
			}
		});
		gameTypeDropdown.onValueChanged.AddListener(x =>
		{
			bool wasUpdated = x != BCManager.LobbyManager.GameTypeId;
			if (wasUpdated)
			{
				BCManager.LobbyManager.GameTypeId = x;
				RemoveAllPlayers();
				UpdateUI();
				FetchLeaderboard();
			}
		});
		RemoveAllPlayers();
		UpdateUI();
		FetchLeaderboard();
	}
	
	private void FetchLeaderboard()
	{
		BCManager.LobbyManager.FetchLeaderboardData(BCManager.LobbyManager.GetLeaderboardId(),
		0, PAGE_SIZE,
		OnFetchLeaderboardSuccess);
	}

	private void OnFetchLeaderboardSuccess(string jsonResponse, object cbObject)
	{
		Debug.Log("Leaderboard data fetched successfully: " + jsonResponse);

		var root = JsonUtility.FromJson<LeaderboardRoot>(jsonResponse);
		var leaderboardEntries = root.data.response.leaderboardData;

		foreach (var entry in leaderboardEntries)
		{
			AddPlayer(entry);
		}

		int topRank = 0;
		if (ListItems.Count > 0)
		{
			topRank = ListItems.Keys.Min(x => x.index);
		}

		Debug.Log("topRank" + topRank);
		// you can use this to check paging
		bool hasMoreAfter = root.data.response.moreAfter;
		bool hasMoreBefore = root.data.response.moreBefore && topRank != 0;

		moreAfterButton.gameObject.SetActive(hasMoreAfter);
		moreBeforeButton.gameObject.SetActive(hasMoreBefore);
	}

	public void GetMoreAfter()
	{
		// if we are getting more after we get the last index and add more to it
		int bottomRank = 0;
		if (ListItems.Count > 0)
		{
			bottomRank = ListItems.Keys.Max(x => x.index);
		}
		BCManager.LobbyManager.FetchLeaderboardData(BCManager.LobbyManager.GetLeaderboardId(),
		bottomRank + START_OFFSET, bottomRank + PAGE_SIZE + START_OFFSET,
		 OnFetchLeaderboardSuccess);
	}

	public void GetMoreBefore()
	{
		// if we are. getting more before, we should get the current index of the top of the list
		// and then fetch it based off that
		int topRank = 0;
		if (ListItems.Count > 0)
		{
			topRank = ListItems.Keys.Min(x => x.index);
		}

		BCManager.LobbyManager.FetchLeaderboardData(BCManager.LobbyManager.GetLeaderboardId(),
		topRank - START_OFFSET, topRank - PAGE_SIZE - START_OFFSET,
		OnFetchLeaderboardSuccess);
	}

	private void OnDisable()
	{
		trackNameDropdown.onValueChanged.RemoveAllListeners();
		gameTypeDropdown.onValueChanged.RemoveAllListeners();
	}

	private void UpdateUI()
	{
		// Update leaderboard UI elements here
		var tracks = ResourceManager.Instance.tracks;
		var trackOptions = tracks.Select(x => x.trackName).ToList();
		trackNameDropdown.ClearOptions();
		trackNameDropdown.AddOptions(trackOptions);
		trackNameDropdown.value = BCManager.LobbyManager.TrackId;
		
		trackIconImage.sprite = ResourceManager.Instance.tracks[BCManager.LobbyManager.TrackId].trackIcon;

		var gameTypes = ResourceManager.Instance.gameTypes;
		var filteredGameTypes = gameTypes
    		.Where(gt => !string.Equals(gt.modeName, "Practice", StringComparison.OrdinalIgnoreCase))
    		.ToList();
		
		gameTypeDropdown.ClearOptions();
		gameTypeDropdown.AddOptions(filteredGameTypes.Select(x => x.modeName).ToList());
		gameTypeDropdown.value = BCManager.LobbyManager.GameTypeId;
	}

  	private void AddPlayer(LeaderboardEntry player)
	{
		if (ListItems.ContainsKey(player))
		{
			var toRemove = ListItems[player];
			Destroy(toRemove.gameObject);

			ListItems.Remove(player);
		}

		var obj = Instantiate(resultItemPrefab, parent).GetComponent<PlayerResultItem>();
		obj.SetResult(player.name, player.score, player.rank, player.data.kartId);
		
		ListItems.Add(player, obj);
	}

	private void RemovePlayer(LeaderboardEntry player)
	{
		if (!ListItems.ContainsKey(player))
			return;

		var obj = ListItems[player];
		if (obj != null)
		{
			Destroy(obj.gameObject);
			ListItems.Remove(player);
		}
	}

	private void RemoveAllPlayers()
	{
		foreach (var item in ListItems.Values)
		{
			if (item != null)
			{
				Destroy(item.gameObject);
			}
		}
		ListItems.Clear();
	}
}
