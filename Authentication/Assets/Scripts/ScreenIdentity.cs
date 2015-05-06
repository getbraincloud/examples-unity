using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using LitJson;

public class ScreenIdentity : BCScreen {
	
	public override void Activate()
	{
	}

	public override void OnScreenGUI()
	{
		GUILayout.BeginVertical();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Logout"))
		{
			BrainCloudWrapper.GetBC().PlayerStateService.Logout(Logout_Success, Failure_Callback, null);
		}
		GUILayout.EndHorizontal();
		
		GUILayout.EndVertical();
    }

	private void Logout_Success(string json, object o)
	{
		Application.LoadLevel("Connect");
	}
}
