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
			//SceneManager.LoadScene(BrainCloudConsts.LOGIN_SCENE_NAME);
			SceneLoader.ShowLoadingScreen();
			SceneLoader.LoadLevel(BrainCloudConsts.LOGIN_SCENE_NAME);
			
		}
	}
	
	//The idea here is to use InitializeUI to re-assign the UI elements to the updated variables. 
	public void RefreshScreen()
	{
		var screens = FindObjectsByType<ContentUIBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		foreach (ContentUIBehaviour screen in screens)
		{
			screen.RefreshScreen();
		}
	}
	
	private IEnumerator WaitToReconnect()
	{
		yield return new WaitUntil(() => !BrainCloudManager.Instance.IsProcessingRequest);
		//SceneManager.LoadScene(BrainCloudConsts.PARENT_SCENE_NAME);
		SceneLoader.ShowLoadingScreen();
		SceneLoader.LoadLevel(BrainCloudConsts.PARENT_SCENE_NAME);
		yield return null;
	}
	
	public void GoToParent()
	{
		//SceneManager.LoadScene(BrainCloudConsts.PARENT_SCENE_NAME);
		SceneLoader.ShowLoadingScreen();
		SceneLoader.LoadLevel(BrainCloudConsts.PARENT_SCENE_NAME);
		
	}
	
	public void GoToBuddysRoom()
	{
		//SceneManager.LoadScene(BrainCloudConsts.GAME_SCENE_NAME);
		SceneLoader.ShowLoadingScreen();
		SceneLoader.LoadLevel(BrainCloudConsts.GAME_SCENE_NAME);
		
	}
}
