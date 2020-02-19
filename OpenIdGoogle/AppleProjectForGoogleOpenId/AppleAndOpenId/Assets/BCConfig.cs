using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BCConfig : MonoBehaviour
{
    public static BrainCloudWrapper _bc;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _bc = gameObject.AddComponent<BrainCloudWrapper>();
        _bc.WrapperName = gameObject.name;
        _bc.InitWithApps();
    }
}
