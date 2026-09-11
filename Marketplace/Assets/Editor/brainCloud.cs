//brainCloud Toolbar

using UnityEngine;
using UnityEditor;
using System.Collections;

public class brainCloud : EditorWindow
{
	[MenuItem ("Window/brainCloud")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(brainCloud));
	}

	[SerializeField]
	public static string bcGameID = ""; 

	void OnGUI () {
		if (GUILayout.Button ("Access brainCloud Portal")) {
			Help.BrowseURL("https://internal.braincloudservers.com");
		}

		if (GUILayout.Button ("About brainCloud")) {
			Help.BrowseURL("http://api.braincloudservers.com/");
		}

		if (GUILayout.Button ("brainCloud API & Tutorials")) {
			Help.BrowseURL ("http://api.braincloudservers.com/api/tutorials");
		}

		GUI.Label(new Rect(5,80,80,20),"BC Game ID:");
		bcGameID = GUI.TextField(new Rect(90,80,100,20), bcGameID, 10);
	}
}