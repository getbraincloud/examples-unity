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

	    // you can use this to check paging
	    bool hasMore = root.data.response.moreAfter;
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

  	public PlayerResultItem resultItemPrefab;
	public Transform parent;
	private Dictionary<LeaderboardEntry, PlayerResultItem> ListItems = new Dictionary<LeaderboardEntry, PlayerResultItem>();
	private void AddPlayer(LeaderboardEntry player)
	{
		if (ListItems.ContainsKey(player))
		{
			var toRemove = ListItems[player];
			Destroy(toRemove.gameObject);

			ListItems.Remove(player);
		}

		var obj = Instantiate(resultItemPrefab, parent).GetComponent<PlayerResultItem>();
		obj.SetResult(player.name, player.score, player.rank);
		
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
