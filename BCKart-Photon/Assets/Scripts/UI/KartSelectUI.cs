using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KartSelectUI : MonoBehaviour
{
	public Image speedStatBar;
	public Image accelStatBar;
	public Image turnStatBar;

	private void OnEnable()
	{
		SelectKart(ClientInfo.KartId);
	}
	
	public void Confirm()
    {
		SelectKart(ClientInfo.KartId);
		GetComponent<UIScreen>().Back();
    }

	public void SelectKart(int kartIndex)
	{
		ClientInfo.KartId = kartIndex;
        if (SpotlightGroup.Search("Kart Display", out SpotlightGroup spotlight)) spotlight.FocusIndex(kartIndex);
		ApplyStats();

		if (RoomPlayer.Local != null)
		{
			RoomPlayer.Local.RPC_SetKartId(kartIndex);
		}
		
		if (BCManager.LobbyManager.Local != null)
        {
			ClientInfo.KartId = kartIndex;
			BCManager.LobbyManager.Local.kartId = kartIndex;

			// send signal update
			Dictionary<string, object> signalData = new Dictionary<string, object>();
			signalData["KartId"] = BCManager.LobbyManager.Local.kartId; // kartId
			BCManager.LobbyService.SendSignal(BCManager.LobbyManager.LobbyId, signalData);
        }
	}

	private void ApplyStats()
	{
		KartDefinition def = ResourceManager.Instance.kartDefinitions[ClientInfo.KartId];
		speedStatBar.fillAmount = def.SpeedStat;
		accelStatBar.fillAmount = def.AccelStat;
		turnStatBar.fillAmount = def.TurnStat;
	}
}
