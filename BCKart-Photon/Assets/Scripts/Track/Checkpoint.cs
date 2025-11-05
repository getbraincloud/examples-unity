using UnityEngine;

public class Checkpoint : MonoBehaviour
{
	public int index = -1;

	private void OnTriggerStay(Collider other)
	{
		if (other.TryGetComponent(out KartLapController kart)) {
            kart.ProcessCheckpoint(this);
		}
	}
}
