using UnityEngine;
using UnityEngine.UI;

public class ProfileSetupUI : MonoBehaviour
{
	public InputField nicknameInput;
	public Button confirmButton;

	private void Start()
	{
		nicknameInput.onValueChanged.AddListener(x => ClientInfo.Username = x);
		nicknameInput.onValueChanged.AddListener(x =>
		{
			// disallows empty usernames to be input
			confirmButton.interactable = !string.IsNullOrEmpty(x);
		});

		nicknameInput.text = ClientInfo.Username;
	}

	public void AssertProfileSetup()
	{
		if (string.IsNullOrEmpty(ClientInfo.Username))
			UIScreen.Focus(GetComponent<UIScreen>());
	}
}