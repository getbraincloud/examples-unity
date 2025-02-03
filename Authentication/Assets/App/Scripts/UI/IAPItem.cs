using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct IAPItemData
{
    public string productId;
    public string name;
    public string title;
    public string imageUrl;
}

public class IAPItem : MonoBehaviour
{
    [SerializeField] private Image _itemThumbnail;
    [SerializeField] private Image _itemSelectedBorder;
    [SerializeField] private TMP_Text _itemName;

    private Button _button;
    private bool _itemSelected = false;
    private Action<IAPItem> _onSelectedAction;

    public IAPItemData data { get; private set; } = new IAPItemData();

    public bool itemSelected { get { return _itemSelected; } set { _itemSelected = value; OnItemUpdated(); } }

    public void InitializeItem(IAPItemData itemData, Action<IAPItem> OnSelected)
    {
        _itemName.text = itemData.name;
        //_itemThumbnail.sprite = itemSprite;
        _onSelectedAction = OnSelected;

        data = itemData;
        //start image download
        StartCoroutine(DownloadImage(data.imageUrl));

        _button = GetComponent<Button>();
        _button.interactable = true;
        _button.onClick.AddListener(OnClicked);

        OnItemUpdated();
    }

    private void OnClicked()
    {
        itemSelected = true;
        _onSelectedAction?.Invoke(this);
    }

    private void OnItemUpdated()
    {
        if (itemSelected)
        {
            //show border
            _itemSelectedBorder.gameObject.SetActive(true);
        }
        else
        {
            //hide border
            _itemSelectedBorder.gameObject.SetActive(false);
        }
    }

    private IEnumerator DownloadImage(string imageUrl)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                if (downloadedTexture != null)
                {
                    Sprite newSprite = Sprite.Create(downloadedTexture, new Rect(0, 0, downloadedTexture.width, downloadedTexture.height), new Vector2(0.5f, 0.5f));
                    //on complete set sprite
                    _itemThumbnail.sprite = newSprite;
                }
            }
            else
            {
                Debug.LogError($"Error downloading image: {request.error}");
            }
        }
    }

}
