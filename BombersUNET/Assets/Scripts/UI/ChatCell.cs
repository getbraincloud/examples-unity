using UnityEngine;
using UnityEngine.UI;

public class ChatCell : ImageDownloader
{
    public const string DEFAULT_NAME = "DEFAULT NAME";
    #region public Properties
    public GameObject NameDisplay;
    public GameObject NameDisplayYou;

    public GameObject Background;
    public GameObject BackgroundYou;
    public GameObject BackgroundAlternate;

    public Text Message;
    public Text UserName { get; private set; }

    public double MessageId { get; private set; }

    public string ProfileId { get; private set; }
    public bool IsYou { get { return BombersNetworkManager._BC.Client.ProfileId == ProfileId; } }
    #endregion

    public void Init(string in_userName, string in_message, string in_profileId, string in_imageURL)//, ulong in_messageId)
    {
        ProfileId = in_profileId;
        NameDisplay.SetActive(!IsYou);
        NameDisplayYou.SetActive(IsYou);

        Message.resizeTextMinSize = Message.fontSize;
        Message.resizeTextMaxSize = Message.fontSize;
        Message.text = in_message;

        MessageId = -1;// in_messageId;

        Canvas.ForceUpdateCanvases();

        UserName = NameDisplay.transform.GetChild(1).GetChild(0).GetComponent<Text>();
        if (IsYou)
        {
            this.RawImage = NameDisplayYou.transform.GetChild(0).GetComponent<RawImage>();
            UserName = NameDisplayYou.transform.GetChild(1).GetChild(0).GetComponent<Text>();
        }

        DownloadImage(in_imageURL);
        BackgroundYou.SetActive(IsYou);
        bool bDisplayBG = !IsYou && transform.GetSiblingIndex() % 2 == 0;
        BackgroundAlternate.SetActive(bDisplayBG);
        Background.SetActive(!bDisplayBG);

        UserName.text = in_userName == "" && !IsYou ? DEFAULT_NAME :
            IsYou ? "YOU" : in_userName;

        // change the size of the cell dynamically
        RectTransform rt = (RectTransform)transform;
        int lineHeightIncrement = 4;
        int lineHeighDelta = Message.cachedTextGenerator.lineCount == 1 ? Message.fontSize + (lineHeightIncrement + 2) : Message.fontSize + lineHeightIncrement;
        rt.sizeDelta = new Vector2(rt.rect.width, 30 + (lineHeighDelta * Message.cachedTextGenerator.lineCount));
    }
}
