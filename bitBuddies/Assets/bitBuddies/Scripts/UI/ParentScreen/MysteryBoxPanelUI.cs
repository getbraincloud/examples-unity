using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MysteryBoxPanelUI : ContentUIBehaviour
{
	[SerializeField] private TMP_Text TitleText;
    [SerializeField] private Button CloseButton;
    [SerializeField] private Transform MysteryBoxSpawnPoint;
	[SerializeField] private List<MysteryBoxInfo> MysteryBoxes;
	[SerializeField] private MysteryBoxUI MysteryBoxPrefab;
	[SerializeField] private Button DoneButton; //for page 3, closes the whole panel but captures the data input
	[SerializeField] private TMP_InputField NameBuddyInput;	// for page 3
	[SerializeField] private Button OpenBoxButton;	// for page 2
	
	[SerializeField] private List<GameObject> _mysteryScreens;   //0 = selection, 1 = open box, 2 = name buddy,display stats etc
	private MysteryBoxInfo _mysteryBoxInfo;
	public MysteryBoxInfo MysteryBoxInfo
	{
		set {_mysteryBoxInfo = value;}
	}
	private int _screenIndex;

	private const string DEFAULT_BUDDY_NAME = "MyBuddy";
	private const string LIST_BOXES_TEXT_TITLE = "Pick a mystery box";
	private const string OPEN_BOX_TEXT_TITLE = "Open your Mystery Box";
	private const string NEW_BUDDY_TEXT_TITLE = "New BitBuddy!";

	protected override void Awake()
	{
		InitializeUI();
		base.Awake();
	}

	protected override void InitializeUI()
    {
	    foreach (MysteryBoxInfo mysteryBoxInfo in MysteryBoxes)
	    {
			var box = Instantiate(MysteryBoxPrefab, MysteryBoxSpawnPoint);
			box.MysteryBoxInfo = mysteryBoxInfo;    
			box.Init();
	    }
	    OpenBoxButton.onClick.AddListener(OnOpenBox);
	    CloseButton.onClick.AddListener(OnCloseButton);
	    DoneButton.onClick.AddListener(OnDoneButton);
	    TitleText.text = LIST_BOXES_TEXT_TITLE;
	    _mysteryScreens[0].SetActive(true);
	    _screenIndex = 0;
    }
    
	private void OnOpenBox()
	{
		//Open another screen where we Animate the box opening
		// After box is opened, we show another screen where the user 
		// picks the name of buddy
		NextPage();
	}
    
    public void NextPage()
    {
	    if(_screenIndex < MysteryBoxes.Count - 1)
	    {
		    _screenIndex++;
		    foreach (GameObject screen in _mysteryScreens)
		    {
			    screen.SetActive(false);
		    }
		    if(_screenIndex == 1)
		    {
			    TitleText.text = OPEN_BOX_TEXT_TITLE;
		    }
		    else
		    {
			    TitleText.text = NEW_BUDDY_TEXT_TITLE;
		    }
		    _mysteryScreens[_screenIndex].SetActive(true);
	    }
	    else
	    {
		    Destroy(gameObject);
	    }
    }
    
    private void OnDoneButton()
    {
	    //ToDo: Save the bit buddy name somewhere..(NameBuddyInput)
	    Destroy(gameObject);
    }
}
