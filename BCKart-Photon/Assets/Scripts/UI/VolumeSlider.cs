using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
	public string mixerParameter;
	public string mixerGroup;
    private float lastVal;

	private void OnEnable()
	{
		if (TryGetComponent(out Slider slider))
		{
			lastVal = slider.value = PlayerPrefs.GetFloat(mixerParameter, 0.75f);
			slider.onValueChanged.AddListener((val) =>
			{
				if (Mathf.Round(val * 10) != Mathf.Round(lastVal * 10))
				{
					AudioManager.Play("hoverUI", mixerGroup);
					lastVal = val;
				}
			});
		}
	}
}
