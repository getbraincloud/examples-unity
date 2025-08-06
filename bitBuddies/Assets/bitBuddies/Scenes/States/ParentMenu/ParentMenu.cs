using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ParentMenu : ContentUIBehaviour
{
    [SerializeField] private Button OpenSettingsButton;
    [SerializeField] private TMP_Text UsernameText;
    [SerializeField] private TMP_Text LevelText;
    [SerializeField] private TMP_Text CoinsText;
    [SerializeField] private TMP_Text GemsText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        InitializeUI();
        base.Awake();
    }

    protected override void InitializeUI()
    {
        OpenSettingsButton.onClick.AddListener(OpenSettingsButtonOnClick);
    
        UserInfo userInfo = BrainCloudManager.Instance.UserInfo;
        UsernameText.text = userInfo.Username;
        LevelText.text = userInfo.Level.ToString();
        CoinsText.text = userInfo.Coins.ToString();
        GemsText.text = userInfo.Gems.ToString();
    }
    
    private void OpenSettingsButtonOnClick()
    {
        // what do other than open ?
        // Settings page shows: Volume slider, connect an email (attach email to anonymous account), about the app and log out
    }
}
