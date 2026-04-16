using System;
using System.Collections;
using UnityEngine;

public static class MonoBehaviourUtilities {

    public static void Invoke(this MonoBehaviour behaviour, Action d, float t) {
        if (behaviour != null)
            behaviour.StartCoroutine(ExecuteAfterTime(d, t));
    }

    private static IEnumerator ExecuteAfterTime(Action theDelegate, float delay) {
        yield return new WaitForSeconds(delay);
        theDelegate?.Invoke();
    }
}