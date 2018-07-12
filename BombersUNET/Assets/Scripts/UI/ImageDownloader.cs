using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageDownloader : MonoBehaviour
{
    public RawImage RawImage = null;
    private void Start()
    {
        DownloadImage(_imageURL, true);
    }

    protected void OnDestroy()
    {
        RawImage = null;
        Destroy(_tex);
    }

    public void DownloadImage(string in_url, bool in_forcedDownload = false)
    {
        if (_imageURL != in_url || in_forcedDownload)
        {
            _imageURL = in_url;

            if (gameObject.activeInHierarchy)
            {
                if (_originalTexture == null)
                    _originalTexture = RawImage.texture;
                else
                    RawImage.texture = _originalTexture;

                if (in_url != null && in_url != "")
                {
                    if (_tex != null) Destroy(_tex);
                    StartCoroutine(LoadIcon(in_url));
                }
            }
        }
    }

    protected IEnumerator LoadIcon(string in_url)
    {
        _tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
        using (WWW www = new WWW(in_url))
        {
            yield return www;
            www.LoadImageIntoTexture(_tex);
            RawImage.texture = _tex;
        }
    }

    protected Texture2D _tex = null;
    protected Texture _originalTexture = null;
    protected string _imageURL = "";
}
