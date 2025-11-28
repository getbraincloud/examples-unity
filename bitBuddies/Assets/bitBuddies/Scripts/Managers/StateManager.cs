using System;
using System.Collections;
using Gameframework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : SingletonBehaviour<StateManager>
{
	[SerializeField] private PopUpUI _genericPopUpUI;

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
			SceneLoader.LoadLevel(BitBuddiesConsts.LOGIN_SCENE_NAME);
			
		}
	}
	
	public void OpenInfoPopUp(string in_title, string in_body)
	{
		var popUp = Instantiate(_genericPopUpUI);
		popUp.SetUpInfoPopup(in_title, in_body);
	}
	
	public void OpenConfirmPopUp(string in_title, string in_body, Action buttonCallback, bool in_showConfirmButton = true)
	{
		var popUp = Instantiate(_genericPopUpUI);
		if(!in_showConfirmButton)
		{
			popUp.DisableConfirmButton();
		}
		popUp.SetupConfirmPopup(in_title, in_body, buttonCallback);
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
		SceneLoader.LoadLevel(BitBuddiesConsts.PARENT_SCENE_NAME);
		yield return null;
	}
	
	public void GoToParent()
	{
		//SceneManager.LoadScene(BitBuddiesConsts.PARENT_SCENE_NAME);
		SceneLoader.ShowLoadingScreen();
		SceneLoader.LoadLevel(BitBuddiesConsts.PARENT_SCENE_NAME);
	}
	
	public void GoToLogin()
	{
		SceneLoader.ShowLoadingScreen();
		SceneLoader.LoadLevel(BitBuddiesConsts.LOGIN_SCENE_NAME);
	}
	
	public void GoToBuddysRoom()
	{
		//SceneManager.LoadScene(BrainCloudConsts.GAME_SCENE_NAME);
		SceneLoader.ShowLoadingScreen();
		SceneLoader.LoadLevel(BitBuddiesConsts.GAME_SCENE_NAME);
		
	}
}
