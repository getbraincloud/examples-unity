using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class App : MonoBehaviour
{
	public static BrainCloudWrapper BC = null;
	
	void Start () {
		// Create the instance of your brainCloud Wrapper
		if (!BC)
		{
			BC = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
			DontDestroyOnLoad(this);							// on an Object that won't be destroyed on Scene Changes

			BC.WrapperName = "PlayerOne"; // Optional: Add a WrapperName
			BC.Init(); // Required: Initialize the Wrapper.
			
			// Now that brainCloud is setup. Let's go to the Login Scene
			SceneManager.LoadScene("Login");
			
		}
		else
		{
			Destroy(gameObject);	// Destroy duplicate objects.
		}
		
	}
	
	void Update () {
		// If you aren't attaching brainCloud as a Component to a gameObject,
		// you must manually update it with this call.
		// _bc.Update();
		// 
		// Given we are using a game Object. Leave _bc.Update commented out.
	}
}
