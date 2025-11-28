using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * Main goal for this class is to be adjustable to be either:
 * Confirm Window - Overlays the canvas to display a question to user to confirm a choice and the window has
 * 2 buttons to confirm and decline the choice
 * Info Window - Overlays the canvas to display information to user and only has 1 button to dismiss the message. 
 */
public class PopUpUI : ContentUIBehaviour
{
	[SerializeField] private TMP_Text TitleText;
	[SerializeField] private TMP_Text BodyText;
	[SerializeField] private Button CloseButton;
	[SerializeField] private Button ConfirmButton;
	
	private Action OnConfirmAction;
	protected override void InitializeUI()
	{
		
	}
	
	public void SetUpInfoPopup(string in_title, string in_body)
	{
		ConfirmButton.gameObject.SetActive(false);
		ConfirmButton.onClick.RemoveAllListeners();
		
		CloseButton.onClick.AddListener(OnCloseButton);
		TitleText.text = in_title;
		BodyText.text = in_body;
	}
	
	public void DisableConfirmButton()
	{
		ConfirmButton.interactable = false;
	}
	
	public void SetupConfirmPopup(string in_title, string in_body, Action in_confirmAction)
	{
		ConfirmButton.gameObject.SetActive(true);
		ConfirmButton.onClick.AddListener(OnConfirmButton);
		OnConfirmAction = in_confirmAction;
		
		CloseButton.onClick.AddListener(OnCloseButton);
		TitleText.text = in_title;
		BodyText.text = in_body;
	}
	
	private void OnConfirmButton()
	{
		if(OnConfirmAction != null)
		{
			OnConfirmAction();			
		}
		//Destroy popup
		OnCloseButton();
	}
}
