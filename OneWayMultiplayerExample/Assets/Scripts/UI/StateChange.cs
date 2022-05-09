using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateChange : MonoBehaviour
{
    //Called from Unity Button
    public void StateButtonChange() => MenuManager.Instance.ButtonPressChangeState();

    public void SetRating() => BrainCloudManager.Instance.SetDefaultPlayerRating();
}