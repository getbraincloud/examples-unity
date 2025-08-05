using System;
using System.Collections;
using Gameframework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : SingletonBehaviour<StateManager>
{
	public override void Awake()
	{
		base.Awake();
		//Initialize managers
		if(BrainCloudManager.Instance != null)
		{
			BrainCloudManager.Instance.StartUp();
		}
		
		if(BrainCloudManager.Instance.CanReconnectUser())
		{
			BrainCloudManager.Instance.ReconnectUser();
			StartCoroutine(WaitToReconnect());
		}
		else
		{
			//Load into login screen
			SceneManager.LoadScene(BrainCloudConsts.LOGIN_SCENE_NAME);
		}
	}
	
	private IEnumerator WaitToReconnect()
	{
		yield return new WaitUntil(() => !BrainCloudManager.Instance.IsProcessingRequest);
		SceneManager.LoadScene(BrainCloudConsts.PARENT_SCENE_NAME);
		yield return null;
	}
	
	public void GoToParent()
	{
		SceneManager.LoadScene(BrainCloudConsts.PARENT_SCENE_NAME);
	}
	
	public void GoToBuddysRoom()
	{
		SceneManager.LoadScene(BrainCloudConsts.GAME_SCENE_NAME);
	}
}
