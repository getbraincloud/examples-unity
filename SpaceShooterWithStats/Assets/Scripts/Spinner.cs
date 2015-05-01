using UnityEngine;
using System.Collections;

public class Spinner : MonoBehaviour {

	public float spinSpeed = .10f;

	float m_spinTime = 0;

	// Use this for initialization
	void Start () {
	
	}

	// Update is called once per frame
	void Update () {
		m_spinTime -= Time.deltaTime;
		while (m_spinTime <= 0)
		{
			if (spinSpeed <= 0) spinSpeed = .10f;
			m_spinTime += spinSpeed;
			transform.Rotate(new Vector3(0, 45.0f, 0));
		}
	}
}
