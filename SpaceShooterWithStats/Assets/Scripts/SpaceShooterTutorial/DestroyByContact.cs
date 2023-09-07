using UnityEngine;
using System.Collections;

public class DestroyByContact : MonoBehaviour
{
	public GameObject explosion;
	public GameObject playerExplosion;
	public int scoreValue;
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
		
		if (other.CompareTag("Boundary"))
		{
			return;
		}
		
		if (explosion != null)
		{
			Instantiate(explosion, transform.position, transform.rotation);
		}

		gameController.AddScore(scoreValue);

		if (other.CompareTag("Player"))
		{
			Instantiate(playerExplosion, other.transform.position, other.transform.rotation);
			gameController.GameOver();
		}

		if (gameObject.CompareTag("Enemy"))
		{
			gameController.OnEnemyKilled();
		}
		else if (gameObject.CompareTag("Asteroid"))
		{
			gameController.OnAsteroidDestroyed();
		}

		Destroy (other.gameObject);
		Destroy (gameObject);
	}
}
