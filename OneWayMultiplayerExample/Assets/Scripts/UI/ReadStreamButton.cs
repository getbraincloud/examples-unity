using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadStreamButton : MonoBehaviour
{
    //Called from a button
    public void RequestReadStream()
    {
         BrainCloudManager.Instance.ReadStream();
    }
}
