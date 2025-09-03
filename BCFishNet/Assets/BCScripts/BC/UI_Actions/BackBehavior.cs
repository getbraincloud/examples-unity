using UnityEngine;
using UnityEngine.SceneManagement;

public class BackBehavior : MonoBehaviour
{
    public void OnMainMenu(bool leaveLobby)
    {
        // Gracefully disconnect using BCFishNetTransport
        var bcFishNetTransport = FindObjectOfType<BCFishNet.BCFishNetTransport>();
        if (bcFishNetTransport != null)
        {
            // Call OnDestroy or a custom shutdown if available
            // This will trigger the transport's shutdown logic
            bcFishNetTransport.Shutdown();
            Debug.Log("Closing down the scene");

            if (leaveLobby) BCManager.Instance.LeaveCurrentLobby();
            else
                PlayerListItemManager.Instance.ClearAll();
        }
        
        // Give time for disconnect to process
        Invoke("ToMainMenu", TimeUtils.DELAY);
    }
    
    private void ToMainMenu()
    {
        SceneManager.LoadScene("Main");
    }
}   
