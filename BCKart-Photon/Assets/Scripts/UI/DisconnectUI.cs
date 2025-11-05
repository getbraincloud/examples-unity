using UnityEngine;
using UnityEngine.UI;

public class DisconnectUI : MonoBehaviour
{
	public Transform parent;
	public Text disconnectStatus;
	public Text disconnectMessage;

	public void ShowMessage( string status, string message)
	{
		if (status == null || message == null)
			return;

		disconnectStatus.text = status;
		disconnectMessage.text = message;

		Debug.Log($"Showing message({status},{message})");
		parent.gameObject.SetActive(true);
	}
}