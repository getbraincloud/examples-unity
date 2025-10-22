using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KartSelectUI : MonoBehaviour
{
	public Image speedStatBar;
	public Image accelStatBar;
	public Image turnStatBar;

	private void OnEnable() {
		SelectKart(ClientInfo.KartId);
	}

	public void SelectKart(int kartIndex)
	{
		ClientInfo.KartId = kartIndex;
        if (SpotlightGroup.Search("Kart Display", out SpotlightGroup spotlight)) spotlight.FocusIndex(kartIndex);
		ApplyStats();

        if ( RoomPlayer.Local != null ) {
            RoomPlayer.Local.RPC_SetKartId(kartIndex);
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
