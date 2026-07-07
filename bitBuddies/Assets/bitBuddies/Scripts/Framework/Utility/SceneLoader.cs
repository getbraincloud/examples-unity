using Gameframework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static bool IsLoading()
    {
        return SceneManager.GetSceneByName(BitBuddiesConsts.LOADING_SCREEN_SCENE_NAME).IsValid();
    }

    public static void LoadLevel(string in_levelName)
    {
        if (LoadingScreen.Tasks.Count > 0)
        {
            LoadingScreen.Tasks.Clear();
        }

        LoadingScreen.Tasks.Push(CO_LoadLevel(in_levelName, waitCondition: null));
        UnloadAllLevelsExcept(BitBuddiesConsts.LOADING_SCREEN_SCENE_NAME);
    }

    public static void LoadLevelWithCondition(string in_levelName, Func<bool> waitCondition)
    {
        if (LoadingScreen.Tasks.Count > 0)
        {
            LoadingScreen.Tasks.Clear();
        }

        LoadingScreen.Tasks.Push(CO_LoadLevel(in_levelName, waitCondition));
        UnloadAllLevelsExcept(BitBuddiesConsts.LOADING_SCREEN_SCENE_NAME);
    }

    private static void UnloadAllLevelsExcept(params string[] in_sceneNames)
    {
        LoadingScreen.Tasks.Push(CO_UnloadAllScenesExcept(in_sceneNames));
    }

    public static void ShowLoadingScreen()
    {
        if (!IsLoading() && LoadingScreen.Tasks.Count > 0)
        {
            SceneManager.LoadSceneAsync(BitBuddiesConsts.LOADING_SCREEN_SCENE_NAME, LoadSceneMode.Additive);
        }
    }

    public static void RemoveLoadingScreen()
    {
        SceneManager.UnloadSceneAsync(BitBuddiesConsts.LOADING_SCREEN_SCENE_NAME);
    }

    private static IEnumerator CO_LoadLevel(string in_levelName, Func<bool> waitCondition)
    {
        yield return "Loading " + in_levelName;

        AsyncOperation operation = SceneManager.LoadSceneAsync(in_levelName, LoadSceneMode.Additive);
        while (!operation.isDone)
        {
            yield return operation.progress;
        }

        //Wait for response from server
        if (waitCondition != null)
        {
            yield return new WaitUntil(waitCondition);
        }
    }

    // CO = Coroutine
    private static IEnumerator CO_UnloadAllScenesExcept(params string[] in_sceneNames)
    {
        yield return "Unloading scenes";
        List<AsyncOperation> scenesToUnload = new List<AsyncOperation>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene currentScene = SceneManager.GetSceneAt(i);
            if (in_sceneNames.Any(sceneName => currentScene.name.Equals(sceneName)) ||
                currentScene.name.Equals(BitBuddiesConsts.LOADING_SCREEN_SCENE_NAME))
            {
                continue;
            }
            scenesToUnload.Add(SceneManager.UnloadSceneAsync(currentScene));
        }

        while (scenesToUnload.Any(operation => !operation.isDone))
        {
            yield return -1;
        }
    }
}
