using UnityEngine;
using UnityEngine.UI;

public class ProfileSetupUI : MonoBehaviour
{
	public InputField nicknameInput;
	public Button confirmButton;
	public Text text;
	
	public void AssertProfileSetup()
	{
		if (string.IsNullOrEmpty(ClientInfo.Username))
			UIScreen.Focus(GetComponent<UIScreen>());
	}

	private void Start()
	{
		GetMyScreen();
		nicknameInput.onValueChanged.AddListener(x => ClientInfo.Username = x);
		nicknameInput.onValueChanged.AddListener(x =>
		{
			// disallows empty usernames to be input
			confirmButton.interactable = !string.IsNullOrEmpty(x);
		});
		UpdateUI();
	}
	void OnEnable()
	{
		initialNickname = ClientInfo.Username;
		UpdateUI();
    }

	public void UpdateUI()
	{
		if (ClientInfo.LoginData != null)
		{
			text.text = ClientInfo.LoginData.universalId;   // 
			nicknameInput.text = ClientInfo.Username;
		}
	}

	public void Logout()
	{
		// force to forget the user this way
		// otherwise we just auto login the last person in
		BCManager.Wrapper.Logout(true, OnLogoutSuccess);
	}

	public void OnLogoutSuccess(string jsonResponse, object cbObject)
	{
		// reset these
		nicknameInput.text = "";
		text.text = "";

		UIScreen.BackToInitial();
		InterfaceManager.Instance.AssertLoggedIn();
	}

	public void UpdateUsername()
	{
		// only update it if changed
		if (initialNickname != ClientInfo.Username)
		{
			BCManager.Wrapper.PlayerStateService.UpdateName(ClientInfo.Username, OnUpdateNameSuccess, OnUpdateNameError);
		}
		else
        {
			myScreen.Back();
        }
    }

	public void OnUpdateNameSuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("ProfileSetupUI OnUpdateNameSuccess: " + jsonResponse);
        
        myScreen.Back();
    }

    public void OnUpdateNameError(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError($"ProfileSetupUI OnUpdateNameError: {status}, {reasonCode}, {jsonError}");
        // handle display errors to user

    }
	private UIScreen GetMyScreen()
	{
		if (!myScreen)
			myScreen = GetComponent<UIScreen>();

		return myScreen;
	}

	private string initialNickname = "";
    private UIScreen myScreen;
}