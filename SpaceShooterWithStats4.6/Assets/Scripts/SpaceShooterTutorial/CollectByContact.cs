using UnityEngine;
using System.Collections;

public class CollectByContact : MonoBehaviour
{
	private GameController gameController;

	void Start ()
	{
		GameObject gameControllerObject = GameObject.FindGameObjectWithTag ("GameController");
		if (gameControllerObject != null)
		{
			gameController = gameControllerObject.GetComponent <GameController>();
		}
		if (gameController == null)
		{
			Debug.Log ("Cannot find 'GameController' script");
		}
	}

	void OnTriggerEnter (Collider other)
	{
		if (other.tag == "Boundary" || other.tag == "Enemy" || other.tag == "Bolt")
		{
			return;
		}

		Destroy (gameObject);
	}
}