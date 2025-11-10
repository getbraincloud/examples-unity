using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using UnityEngine;

public static class AssetLoader 
{
	public static Sprite LoadSprite(string path)
	{
		Sprite result = Resources.Load<Sprite>(path.IsNullOrEmpty() ? BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY : path);
		if(result == null)
		{
			Debug.LogError($"Couldn't load sprite: {path}");
		}
		return result;
	}
}
