using UnityEngine;
using System.Collections;

[System.Serializable]
public class Boundary 
{
	public float xMin, xMax, zMin, zMax;
}

public class PlayerController : MonoBehaviour
{
	public float speed;
	public float tilt;
	public Boundary boundary;

	public GameObject shot;
	public Transform shotSpawn;
	public float fireRate;
	 
	private float nextFire;
	private GameController gameController;
	private Rigidbody m_rigidbody;
	
	void Start ()
	{
		m_rigidbody = GetComponent<Rigidbody>();
		gameController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController>();
	}
	
	void Update ()
	{
		if (Input.GetButton("Fire1") && Time.time > nextFire) 
		{
			nextFire = Time.time + fireRate;
			Instantiate(shot, shotSpawn.position, shotSpawn.rotation);
			GetComponent<AudioSource>().Play ();

			gameController.OnShotFired();
		}
	}

	void FixedUpdate ()
	{
		float moveHorizontal = Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");

		Vector3 movement = new Vector3 (moveHorizontal, 0.0f, moveVertical);
		m_rigidbody.velocity = movement * speed;
		
		m_rigidbody.position = new Vector3
		(
			Mathf.Clamp (m_rigidbody.position.x, boundary.xMin, boundary.xMax), 
			0.0f, 
			Mathf.Clamp (m_rigidbody.position.z, boundary.zMin, boundary.zMax)
		);
		
		m_rigidbody.rotation = Quaternion.Euler (0.0f, 0.0f, m_rigidbody.velocity.x * -tilt);
	}
}
