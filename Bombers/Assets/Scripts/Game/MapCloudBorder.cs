//This script places the cloud border particle emitters on the edges of the playfield based on the bounds of the playfield extents.

using UnityEngine;
using System.Collections;

public class MapCloudBorder : MonoBehaviour {

	private Collider m_playfieldExtents; //the bounds of the playfield adjusted by the playfield size choice when creating a game
	public Transform[] m_cloudEmitters; //the cloud border effects (0=right 1=left 2=top 3=bottom)
	private float m_inset = 20.0f; //the number of units by which to inset the cloud effects from the edge of the playfield.
	private float m_heightOffset = 8.0f; //the height offset of the clouds

	public void SetCloudBorder () 
	{
		m_playfieldExtents = GameObject.Find("MapBounds").GetComponent<Collider>();

		//set the four cloud emitter positions to the edge of the playfield
		m_cloudEmitters[0].position = new Vector3( m_playfieldExtents.bounds.max.x - m_inset , 0.0f , m_heightOffset );
		m_cloudEmitters[1].position = new Vector3( m_playfieldExtents.bounds.min.x + m_inset, 0.0f, m_heightOffset );
		m_cloudEmitters[2].position = new Vector3( 0.0f , m_playfieldExtents.bounds.max.y - m_inset , m_heightOffset );
		m_cloudEmitters[3].position = new Vector3( 0.0f , m_playfieldExtents.bounds.min.y + m_inset , m_heightOffset );

	}
	

}
