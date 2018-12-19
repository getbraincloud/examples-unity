using BrainCloudUnity.BrainCloudPlugin;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BrainCloudExampleStartup : MonoBehaviour {

	static BrainCloudExampleStartup()
	{
		// Setting the default template id.
		PlayerPrefs.SetString(BrainCloudPluginSettings.DEFAULT_TEMPLATE_ID_KEY, "112186");
		PlayerPrefs.SetString(BrainCloudPluginSettings.DEFAULT_TEMPLATE_NAME_KEY, "TicTacToe");
	}
}
