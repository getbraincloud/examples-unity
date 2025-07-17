using System;
using UnityEngine;

public class bitBuddiesNetworkManager:MonoBehaviour
{
    private BrainCloudWrapper _bcWrapper;

    private string _url = "https://internal.braincloudservers.com/dispatcherv2";
    private string _appParentId = "49161";
    private string _appParentSecret = "2a5a1156-e5ab-4954-8b49-ab6baa1af8a2";
    private string _appChildId = "49162";
    private string _appChildSecret = "59944767-461e-4a40-996d-15baf5b7a5bf";
    private string _version = "1.0.0";

    private void Awake()
    {
        _bcWrapper = gameObject.AddComponent<BrainCloudWrapper>();
        _bcWrapper.Init(_url, _appParentId, _appParentSecret, _version);
    }
}
