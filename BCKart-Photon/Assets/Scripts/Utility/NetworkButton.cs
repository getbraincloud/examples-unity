using UnityEngine;
using UnityEngine.UI;

public class NetworkButton : MonoBehaviour
{
	public bool interactableWhileConnected = false;
	private Button btn;

	private void Awake()
	{
		btn = GetComponent<Button>();
	}

	private void OnEnable()
	{
		if (interactableWhileConnected)
			btn.interactable = GameLauncher.ConnectionStatus == ConnectionStatus.Connected;
		else
			btn.interactable = GameLauncher.ConnectionStatus != ConnectionStatus.Connected;
	}
}
