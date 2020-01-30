using UnityEngine;

public class App : MonoBehaviour
{    
    public static BrainCloudWrapper Bc;

    [SerializeField] public string WrapperName;

    private void Awake()
    {
        Bc = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
        DontDestroyOnLoad(this); // on an Object that won't be destroyed on Scene Changes

        Bc.WrapperName = WrapperName; // Optional: Add a WrapperName
        Bc.Init(); // Required: Initialize the Wrapper   
        
        Bc.Client.EnableLogging(true);
    }
}