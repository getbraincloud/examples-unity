using UnityEngine;

/// <summary>
/// This allows us to call 'Awake' (b.Setup()) and 'OnDestroy' (b.OnDestruction()) on disabled MonoBehaviours.
/// Usage: Put this class on a parent with children that implement the 'IDisabledUI' interface.
/// </summary>
public class DisabledUI : MonoBehaviour {
    private void Awake() {
        foreach ( var behaviour in GetComponentsInChildren<IDisabledUI>(true) ) behaviour.Setup();
    }

    private void OnDestroy() {
        foreach ( var behaviour in GetComponentsInChildren<IDisabledUI>(true) ) behaviour.OnDestruction();
    }
}
