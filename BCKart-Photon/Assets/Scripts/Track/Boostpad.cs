using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boostpad : MonoBehaviour
{
	public int boostLevel = 1;

	private void OnTriggerStay(Collider other)
	{
		if (other.TryGetComponent(out KartEntity kart))
		{
            kart.Controller.GiveBoost(true, boostLevel);
		}
	}
}