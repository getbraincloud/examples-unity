using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndRaceUI : MonoBehaviour, GameUI.IGameUIComponent, IDisabledUI
{
  	public PlayerResultItem resultItemPrefab;
	public Button continueEndButton;
	public GameObject resultsContainer;

	public Text raceCompleteText;

	private KartEntity _kart;

	private const float DELAY = 2;
	public void Init(KartEntity entity)
	{
		_kart = entity;

		// we will stay with this lobby
		continueEndButton.onClick.AddListener(() =>
		{	
			SendContinueSignal();
		});
	}

	public void Setup()
	{
		_hasInvokedContinue = false;
		raceCompleteText.text = "Waiting for others...";

		KartLapController.OnRaceCompleted += RedrawResultsList;
		KartEntity.OnKartSpawned += RedrawResultsList;
		KartEntity.OnKartDespawned += RedrawResultsList;
	}

	public void OnDestruction()
	{
		KartLapController.OnRaceCompleted -= RedrawResultsList;
		KartEntity.OnKartSpawned -= RedrawResultsList;
		KartEntity.OnKartDespawned -= RedrawResultsList;
	}

	public void RedrawResultsList(KartComponent updated)
	{
		var parent = resultsContainer.transform;
		ClearParent(parent);

		var karts = GetFinishedKarts();
		for (var i = 0; i < karts.Count; i++)
		{
			var kart = karts[i];

			Instantiate(resultItemPrefab, parent)
				.SetResult(kart.Controller.RoomUser.Username.Value, kart.LapController.GetTotalRaceTime(), i + 1);
		}

		EnsureContinueButton(karts);
	}

	private static List<KartEntity> GetFinishedKarts() =>
        KartEntity.Karts
            .OrderBy(x => x.LapController.GetTotalRaceTime())
            .Where(kart => kart.LapController.HasFinished)
            .ToList();

	private void SendContinueSignal()
	{
		// since only the leader can do this, 
		// send a lobby signal that we are continuing
		Dictionary<string, object> signalData = new Dictionary<string, object>();

		signalData["continueLobbyId"] = BCManager.LobbyManager.LobbyId;
		BCManager.LobbyService.SendSignal(BCManager.LobbyManager.LobbyId, signalData);
	}
	
	private bool _hasInvokedContinue = false;
	private void EnsureContinueButton(List<KartEntity> karts)
	{
		// always hide it for now, we will update the messaging
		// and auto go back to the main lobby with everyone
		// mimicing a button press
		var allFinished = karts.Count == KartEntity.Karts.Count;
		if (RoomPlayer.Local.IsLeader)
		{
			continueEndButton.gameObject.SetActive(false); //allFinished);
		}

		if (allFinished)
		{
			raceCompleteText.text = "Race Complete!";

			if (_hasInvokedContinue) return;
			_hasInvokedContinue = true;
            Invoke("SendContinueSignal", DELAY);
        }
	}

	private static void ClearParent(Transform parent)
	{
		var len = parent.childCount;
		for (var i = 0; i < len; i++)
		{
			Destroy(parent.GetChild(i).gameObject);
		}
	}
}