using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace EpicToonFX
{
	public class ETFXEffectCycler : MonoBehaviour
	{
		public List<GameObject> listOfEffects;
		int effectIndex = 0;
	
		[Header("Spawn Settings")]
		
		[SerializeField]
		
		[Space(10)]
		public float loopLength = 1.0f;
		public float startDelay = 1.0f;
		public bool disableLights = true;
		public bool disableSound = true;

		void Start ()
		{	
			Invoke("PlayEffect", startDelay);
		}

		public void PlayEffect()
		{
			StartCoroutine("EffectLoop");
			
			if (effectIndex < listOfEffects.Count - 1)
			{
                effectIndex++;
			}
				
            else
			{
                effectIndex = 0;
			}
		}

		private IEnumerator EffectLoop()
		{
            GameObject instantiatedEffect = (GameObject) Instantiate(listOfEffects[effectIndex], transform.position, transform.rotation * Quaternion.Euler (0, 0, 0));
			
			if (disableLights && instantiatedEffect.GetComponent<Light>())
			{
				instantiatedEffect.GetComponent<Light>().enabled = false;
			}

			if (disableSound && instantiatedEffect.GetComponent<AudioSource>())
			{
				instantiatedEffect.GetComponent<AudioSource>().enabled = false;
			}
			
			yield return new WaitForSeconds(loopLength);
			
			Destroy(instantiatedEffect);
			PlayEffect();
		}
	}
}