using UnityEngine;

public class FinishLine : MonoBehaviour {
    public bool debug;

    private void OnTriggerStay(Collider other) {
        if ( other.TryGetComponent(out KartLapController kart) ) {
            kart.ProcessFinishLine(this);
        }
    }
}
