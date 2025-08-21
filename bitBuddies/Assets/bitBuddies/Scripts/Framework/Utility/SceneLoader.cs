using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gameframework;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
	public static bool IsLoading()
	{
		return SceneManager.GetSceneByName(BrainCloudConsts.LOADING_SCREEN_SCENE_NAME).IsValid();
	}
	
	public static void LoadLevel(string in_levelName)
	{
		LoadingScreen.Tasks.Push(CO_LoadLevel(in_levelName));
		UnloadAllLevelsExcept(BrainCloudConsts.LOADING_SCREEN_SCENE_NAME);
		SceneManager.LoadSceneAsync(in_levelName);
	}
	
	public static void UnloadAllLevelsExcept(params string[] in_sceneNames)
	{
		LoadingScreen.Tasks.Push(CO_UnloadAllScenesExcept(in_sceneNames));
	}
	
	public static void ShowLoadingScreen()
	{
		if(!IsLoading() && LoadingScreen.Tasks.Count > 0)
		{
			SceneManager.LoadSceneAsync(BrainCloudConsts.LOADING_SCREEN_SCENE_NAME, LoadSceneMode.Additive);
		}
	}
	
	public static void RemoveLoadingScreen()
	{
		SceneManager.UnloadSceneAsync(BrainCloudConsts.LOADING_SCREEN_SCENE_NAME);
		
	}
	
	//CO = Coroutine
	private static IEnumerator CO_LoadLevel(string in_levelName)
	{
		yield return "Loading " + in_levelName;
		AsyncOperation operation = SceneManager.LoadSceneAsync(in_levelName, LoadSceneMode.Additive);
		
		while(!operation.isDone)
		{
			yield return operation.progress;
		}

	}
	
	//CO = Coroutine
	private static IEnumerator CO_UnloadAllScenesExcept(params string[] in_sceneNames)
	{
		yield return "Unloading scenes";
		List<AsyncOperation> scenesToUnload = new List<AsyncOperation>();
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			Scene currentScene = SceneManager.GetSceneAt(i);
			if(in_sceneNames.Any(sceneNames => currentScene.name.Equals(sceneNames)) || 
			   currentScene.name.Equals(BrainCloudConsts.LOADING_SCREEN_SCENE_NAME))
			{
				continue;
			}
			scenesToUnload.Add(SceneManager.UnloadSceneAsync(currentScene));
		}
		
		//This doesn't support tracking progress, just shows loading... type of thing
		while(scenesToUnload.Any(operation => !operation.isDone))
		{
			yield return -1;
		}
	}
}
