using System;
using UnityEngine;

public class LevelUpStarAnim : MonoBehaviour
{
    public Action OnAnimComplete;

    public void AnimationComplete()
    {
        OnAnimComplete?.Invoke();
        Destroy(gameObject);
    }
}
