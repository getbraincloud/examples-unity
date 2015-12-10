using UnityEngine;
using System.Collections;

public class EvasiveManeuver : MonoBehaviour
{
	public Boundary boundary;
	public float tilt;
	public float dodge;
	public float smoothing;
	public Vector2 startWait;
	public Vector2 maneuverTime;
	public Vector2 maneuverWait;

	private float currentSpeed;
	private float targetManeuver;

	private Rigidbody m_rigidbody;

	void Start ()
	{
		m_rigidbody = GetComponent<Rigidbody>();

		currentSpeed = GetComponent<Rigidbody>().velocity.z;
		StartCoroutine(Evade());
	}
	
	IEnumerator Evade ()
	{
		yield return new WaitForSeconds (Random.Range (startWait.x, startWait.y));
		while (true)
		{
			targetManeuver = Random.Range (1, dodge) * -Mathf.Sign (transform.position.x);
			yield return new WaitForSeconds (Random.Range (maneuverTime.x, maneuverTime.y));
			targetManeuver = 0;
			yield return new WaitForSeconds (Random.Range (maneuverWait.x, maneuverWait.y));
		}
	}
	
	void FixedUpdate ()
	{
		float newManeuver = Mathf.MoveTowards (m_rigidbody.velocity.x, targetManeuver, smoothing * Time.deltaTime);
		m_rigidbody.velocity = new Vector3 (newManeuver, 0.0f, currentSpeed);
		m_rigidbody.position = new Vector3
		(
			Mathf.Clamp(m_rigidbody.position.x, boundary.xMin, boundary.xMax), 
			0.0f, 
			Mathf.Clamp(m_rigidbody.position.z, boundary.zMin, boundary.zMax)
		);
		if(m_rigidbody.tag != "Powerup1" || m_rigidbody.tag != "Powerup2")
			m_rigidbody.rotation = Quaternion.Euler (0, 0, m_rigidbody.velocity.x * -tilt);
	}
}
