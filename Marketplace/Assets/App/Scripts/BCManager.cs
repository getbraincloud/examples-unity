using BrainCloud;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BrainCloudWrapper))]
public class BCManager : MonoBehaviour
{
    // Singleton instance
    public static BCManager Instance { get; private set; }

    // The BrainCloud wrapper
    public BrainCloudWrapper BCWrapper { get; private set; }

    // RunCallbacks interval in seconds
    [Header("Config")]
    [SerializeField] private float callbackInterval = 0.01f;
    private float callbackTimer = 0f;

    [Header("Events")]
    public UnityEvent OnBrainCloudInitialized;

    public bool isInitialized { get; private set; }

    private void Awake()
    {
        // Ensure there's only one instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize BrainCloud wrapper
        BCWrapper = GetComponent<BrainCloudWrapper>();
        BCWrapper.Init();
        BCWrapper.EnableLongSession(true);
        Debug.Log("BCManager initialized and BrainCloudWrapper ready.");
        isInitialized = BCWrapper.Client.Initialized;
        OnBrainCloudInitialized?.Invoke();
    }

    private void Update()
    {
        // Increment timer
        callbackTimer += Time.deltaTime;

        // Only run callbacks at the defined interval
        if (callbackTimer >= callbackInterval)
        {
            callbackTimer = 0f;

            if (BCWrapper != null && BCWrapper.Client.Initialized)
            {
                BCWrapper.Client.RunCallbacks();
            }
        }
    }

    // Optional helper to access BrainCloudClient directly
    public BrainCloudClient Client => BCWrapper.Client;
}
