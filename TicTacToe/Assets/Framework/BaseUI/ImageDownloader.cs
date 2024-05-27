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
            yield return new WaitForEndOfFrame();

            UnityWebRequest request = UnityWebRequestTexture.GetTexture(in_url);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                RawImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                if (success != null)
                {
                    success(in_url);
                }
            }
            else
            {
                Debug.LogError(request.error);
            }
        }

        public delegate void ImageDownloadedCallBack(string in_url);

        protected Texture _originalTexture = null;
        protected string _imageURL = "";
    }
}
