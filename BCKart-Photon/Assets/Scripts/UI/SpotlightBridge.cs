using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotlightBridge : MonoBehaviour
{
	public string target = "";

	public void FocusIndex(int index)
	{
		if (string.IsNullOrEmpty(target))
		{
			Debug.LogWarning("SpotlightBridge target field has not been set", this);
			return;
		}

		if (SpotlightGroup.Search(target, out SpotlightGroup spotlight))
			spotlight.FocusIndex(index);
	}
}
