using System;
using UnityEngine;

public class AnimatedCoinCollect : MonoBehaviour
{
    public Action<int> OnCoinAnimComplete;
    public int newCoinsBalance = 0;

    public void OnAnimationComplete()
    {
        Destroy(gameObject);
        OnCoinAnimComplete?.Invoke(newCoinsBalance);
    }
}
