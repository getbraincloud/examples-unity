using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RegionUI : MonoBehaviour
{
	private void Awake()
	{
		if (TryGetComponent(out Dropdown dropdown))
		{
			// TODO: update options once we can request a list of regions
			string[] options = new string[] { "us", "eu", "asia" };

			dropdown.AddOptions(new List<string>(options));
			dropdown.onValueChanged.AddListener((index) =>
			{
				string region = dropdown.options[index].text;
				Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.FixedRegion = region;
				Debug.Log($"Setting region to {region}");
			});

			string curRegion = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.FixedRegion;
			Debug.Log($"Initial region is {curRegion}");
			int curIndex = dropdown.options.FindIndex((op) => op.text == curRegion);
			dropdown.value = curIndex != -1 ? curIndex : 0;
		}
	}
}