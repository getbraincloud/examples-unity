using UnityEngine;

public class ProfilePanel : MonoBehaviour
{
    public GameObject LogOutPopup;
    public GameObject EditPlayerPopup;

    void Start()
    {

    }

    public void OnClickSwitchPlayer()
    {
        PanelSwitcher.SwitchToPanel(Panel.PlayerSelect);
    }

    public void OnClickLogOut()
    {
        LogOutPopup.SetActive(true);
        BrainCloudWrapper.GetBC().PlayerStateService.Logout(LogoutSuccess);
    }

    private void LogoutSuccess(string jsonResponse, object cbObject)
    {
        LogOutPopup.SetActive(false);
        PanelSwitcher.SwitchToPanel(Panel.Login);
    }
}
