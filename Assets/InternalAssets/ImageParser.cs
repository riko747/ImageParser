using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace InternalAssets
{
    public class ImageParser : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI inputStringUrl;
        [SerializeField] private Button parseButton;
        [SerializeField] private RawImage rawImagePrefab;
        [SerializeField] private Transform imagesParentTransform;
        [SerializeField] private TextMeshProUGUI numberOfImagesCount;

        private Dictionary<string, RawImage> _imagesDictionary = new();
        private RawImage _currentRawImage;
        private void Awake()
        {
            parseButton.onClick.AddListener(PrepareToLoadPage);
        }

        private void PrepareToLoadPage() => StartCoroutine(LoadPage());

        private IEnumerator LoadPage()
        {
            var url = inputStringUrl.text;
            using var www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                Debug.LogError("Error while loading page: " + www.error);
            else
            {
                var html = www.downloadHandler.text;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                foreach (Transform child in imagesParentTransform)
                {
                    child.gameObject.SetActive(false);
                }

                foreach (var img in doc.DocumentNode.SelectNodes("//img"))
                {
                    var src = img.GetAttributeValue("src", null);

                    if (string.IsNullOrEmpty(src) || _imagesDictionary.ContainsKey(src))
                    {
                        _imagesDictionary[src].gameObject.SetActive(true);
                    }
                    else
                    {
                        _currentRawImage = Instantiate(rawImagePrefab, imagesParentTransform);
                        _currentRawImage.name = src;
                        StartCoroutine(LoadImage(src, _currentRawImage));
                    }
                }
            }

            var childTransforms = new Transform[imagesParentTransform.childCount];
            for (var i = 0; i < imagesParentTransform.childCount; i++)
                childTransforms[i] = imagesParentTransform.GetChild(i);
            numberOfImagesCount.text = childTransforms.Count(childTransform => childTransform.gameObject.activeSelf)
                .ToString();
        }

        private IEnumerator LoadImage(string url, RawImage rawImage)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error while loading image: " + request.error);
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                rawImage.texture = texture;
                _imagesDictionary.Add(url, rawImage);
            }
        }

        private void OnDestroy() => parseButton.onClick.RemoveListener(PrepareToLoadPage);
    }
}
