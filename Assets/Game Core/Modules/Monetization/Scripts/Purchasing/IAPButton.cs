using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IslandEscape
{
    public class IAPButton : MonoBehaviour
    {
        [SerializeField] Image backImage;
        [SerializeField] Button button;
        [SerializeField] TMP_Text priceText;
        [SerializeField] GameObject loadingObject;

        [Space]
        [SerializeField] Sprite activeBackSprite;
        [SerializeField] Sprite unactiveBackSprite;

        private ProductKeyType key;

        private void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        public void Init(ProductKeyType key)
        {
            this.key = key;

            UpdateState();
        }

        public void UpdateState()
        {
            UpdateState(IAPManager.GetProductData(key));
        }

        public void UpdateState(ProductData product)
        {
            if (product != null)
            {
                loadingObject.SetActive(false);
                priceText.gameObject.SetActive(true);

                backImage.sprite = activeBackSprite;

                priceText.text = product.GetLocalPrice();
                ShowCurrencyIcon(product.CurrencyImageUrl);
            }
            else
            {
                SetDisabledState();
            }
        }

        // Yandex Games: the portal currency icon goes next to the price
        private RawImage currencyIcon;
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> iconCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

        private void ShowCurrencyIcon(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (currencyIcon == null)
            {
                GameObject iconObject = new GameObject("Currency Icon", typeof(RectTransform), typeof(RawImage));
                RectTransform newIconRect = (RectTransform)iconObject.transform;
                newIconRect.SetParent(priceText.transform, false);
                float size = priceText.fontSize * 0.95f;
                newIconRect.sizeDelta = new Vector2(size, size);
                newIconRect.anchorMin = newIconRect.anchorMax = new Vector2(0.5f, 0.5f);
                currencyIcon = iconObject.GetComponent<RawImage>();
                currencyIcon.raycastTarget = false;
                currencyIcon.enabled = false;
            }

            // one line, text shifted left by the icon's width so the pair stays centred, icon after the glyphs
            float iconSize = ((RectTransform)currencyIcon.transform).sizeDelta.x;
            priceText.enableWordWrapping = false;
            priceText.enableAutoSizing = true;
            priceText.fontSizeMax = priceText.fontSize;
            priceText.fontSizeMin = priceText.fontSize * 0.5f;
            priceText.margin = new Vector4(0f, 0f, iconSize + 6f, 0f);
            priceText.ForceMeshUpdate();
            Bounds bounds = priceText.textBounds;
            RectTransform iconRect = (RectTransform)currencyIcon.transform;
            // textBounds are relative to the text pivot, the icon is anchored to the rect centre
            RectTransform textRect = priceText.rectTransform;
            Vector2 centre = new Vector2((0.5f - textRect.pivot.x) * textRect.rect.width, (0.5f - textRect.pivot.y) * textRect.rect.height);
            iconRect.anchoredPosition = new Vector2(bounds.max.x + 4f + iconSize * 0.5f, bounds.center.y) - centre;

            if (iconCache.TryGetValue(url, out Texture2D cached))
            {
                currencyIcon.texture = cached;
                currencyIcon.enabled = true;
                return;
            }

            StartCoroutine(LoadIcon(url));
        }

        private System.Collections.IEnumerator LoadIcon(string url)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success) yield break;

                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                iconCache[url] = texture;
                if (currencyIcon != null)
                {
                    currencyIcon.texture = texture;
                    currencyIcon.enabled = true;
                }
            }
        }

        private void SetDisabledState()
        {
            loadingObject.SetActive(true);
            priceText.gameObject.SetActive(false);

            backImage.sprite = unactiveBackSprite;
        }

        private void OnButtonClicked()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            IAPManager.BuyProduct(key);
        }
    }
}