using UnityEngine;
using System.Collections;

namespace EpicToonFX
{
	public class ETFXLoopScript : MonoBehaviour
	{
		public GameObject chosenEffect;
		public float loopTimeLimit = 2.0f;
	
		[Header("Spawn without")]
	
		public bool disableLights = true;
		public bool disableSound = true;

		void Start ()
		{	
			PlayEffect();
		}

		public void PlayEffect()
		{
			StartCoroutine("EffectLoop");
		}

		IEnumerator EffectLoop()
		{
			GameObject effectPlayer = (GameObject) Instantiate(chosenEffect, transform.position, transform.rotation);
		
			if (disableLights && effectPlayer.GetComponent<Light>())
			{
				effectPlayer.GetComponent<Light>().enabled = false;
			}

			if (disableSound && effectPlayer.GetComponent<AudioSource>())
			{
				effectPlayer.GetComponent<AudioSource>().enabled = false;
			}
			
			yield return new WaitForSeconds(loopTimeLimit);

			Destroy (effectPlayer);
			PlayEffect();
		}
	}
}