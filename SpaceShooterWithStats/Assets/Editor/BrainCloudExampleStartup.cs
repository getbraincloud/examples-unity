using BrainCloud;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BrainCloudExampleStartup : MonoBehaviour {

	static BrainCloudExampleStartup()
	{
		// Setting the default template id.
		PlayerPrefs.SetString("APP_ID", BrainCloud.Plugin.Interface.AppId);
		PlayerPrefs.SetString("NAME", "Space Shooter");
	}
}
