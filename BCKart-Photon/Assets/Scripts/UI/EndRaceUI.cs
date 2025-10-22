using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;
using UnityEngine.UI;

public class EndRaceUI : MonoBehaviour, GameUI.IGameUIComponent, IDisabledUI
{
  public PlayerResultItem resultItemPrefab;
	public Button continueEndButton;
	public GameObject resultsContainer;

	private KartEntity _kart;

	private const float DELAY = 4;
	public void Init(KartEntity entity)
	{
		_kart = entity;
		continueEndButton.onClick.AddListener(() => LevelManager.LoadMenu());
	}

	public void Setup()
	{
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

    private void EnsureContinueButton(List<KartEntity> karts)
	{
        var allFinished = karts.Count == KartEntity.Karts.Count;
		if (RoomPlayer.Local.IsLeader) {
            continueEndButton.gameObject.SetActive(allFinished);
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