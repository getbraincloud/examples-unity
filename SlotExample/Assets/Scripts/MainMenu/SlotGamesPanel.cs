using UnityEngine;
using BrainCloudSlots.Connection;

public class SlotGamesPanel : MonoBehaviour
{

    void Start()
    {

    }

    public void LoadGame(string sceneName)
    {
        FindObjectOfType<BrainCloudStats>().m_readyToPlay = true;
        Application.LoadLevel(sceneName);
    }
}
