
using UnityEngine;

public class BCConfig : MonoBehaviour
{
	private BrainCloudWrapper _bc;

	public BrainCloudWrapper GetBrainCloud()
	{
		return _bc;
	}
	
	
	// Use this for initialization
	void Awake ()
	{
		DontDestroyOnLoad(gameObject);
		_bc = gameObject.AddComponent<BrainCloudWrapper>();
		
		_bc.WrapperName = gameObject.name;    // Optional: Set a wrapper name
        _bc.Init();      // Init data is taken from the brainCloud Unity Plugin
        _bc.Client.EnableLogging(true);
	}
}
