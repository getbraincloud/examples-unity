using System.Collections;
using UnityEngine;
using ArrayUtility;

public class ItemDisplayBridge : MonoBehaviour
{
	[SerializeField] private GameUI hud;

	public void PlaySpinTickSound()
	{
		AudioManager.Play("tickItemUI", AudioManager.MixerTarget.UI);
	}

	public void GetRandomIcon()
	{
		hud.SetPickupDisplay(ResourceManager.Instance.powerups.RandomElement());
	}
}