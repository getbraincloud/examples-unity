using UnityEngine;
using System.Collections;

//This script simply forces the object's roation to 0,0,0 at every frame 

public class LockOrientation : MonoBehaviour {

	void LateUpdate()
	{
		transform.rotation = Quaternion.identity;
	}
}
