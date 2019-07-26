using BrainCloud.Internal;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Gameframework
{
    public class ImageDownloader : BaseBehaviour
    {
        public RawImage RawImage = null;
        private void Start()
        {
            DownloadImage(_imageURL, true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RawImage = null;
        }

        public void DownloadImage(string in_url, bool in_forcedDownload = false, ImageDownloadedCallBack success = null)
        {
            if (_imageURL != in_url || in_forcedDownload)
            {
                _imageURL = in_url;

                if (gameObject.activeInHierarchy)
                {
                    if (_originalTexture == null)
                        _originalTexture = RawImage != null ? RawImage.texture : null;
                    else
                        RawImage.texture = _originalTexture;

                    if (in_url != null && in_url != "")
                    {
                        StartCoroutine(LoadIcon(in_url, success));
                    }
                }
            }
        }

        protected IEnumerator LoadIcon(string in_url, ImageDownloadedCallBack success = null)
        {
            using (UnityWebRequest www = new UnityWebRequest(in_url))
            {
                www.downloadHandler = new DownloadHandlerTexture();
                yield return www.SendWebRequest();
                RawImage.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                success(in_url);
            }
        }

        public delegate void ImageDownloadedCallBack(string in_url);

        protected Texture _originalTexture = null;
        protected string _imageURL = "";
    }
}
