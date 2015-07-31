using BrainCloudSlots.Connection;
using UnityEngine;

public class PlayerSelectPanel : MonoBehaviour
{
    public GameObject MainMenu;

    void Start()
    {

    }

    public void ToMainMenu()
    {
        FindObjectOfType<BrainCloudStats>().ReadSlotsData();
        FindObjectOfType<BrainCloudStats>().ReadStatistics();
        PanelSwitcher.SwitchToPanel(Panel.MainMenu);
    }
}
