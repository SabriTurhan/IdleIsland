using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IslandEscape
{
    /// <summary>
    /// Yandex Games platform layer (WebGL build): SDK readiness, gameplay markup, ads, purchases and cloud save.
    /// The page initialises the SDK before Unity starts (WebGLTemplates/IslandEscape), so everything here is
    /// synchronous except ads and purchases, which report back to the YandexBridge object.
    /// Outside a Yandex page (editor, other platforms) IsAvailable is false and calls fall back to no-ops.
    /// </summary>
    public static class YandexGames
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int YG_IsAvailable();
        [DllImport("__Internal")] private static extern string YG_GetLanguage();
        [DllImport("__Internal")] private static extern string YG_GetDeviceType();
        [DllImport("__Internal")] private static extern void YG_Ready();
        [DllImport("__Internal")] private static extern void YG_GameplayStart();
        [DllImport("__Internal")] private static extern void YG_GameplayStop();
        [DllImport("__Internal")] private static extern void YG_ShowFullscreen();
        [DllImport("__Internal")] private static extern void YG_ShowRewarded();
        [DllImport("__Internal")] private static extern string YG_GetCatalog();
        [DllImport("__Internal")] private static extern string YG_GetPurchases();
        [DllImport("__Internal")] private static extern void YG_Purchase(string id);
        [DllImport("__Internal")] private static extern void YG_Consume(string token);
        [DllImport("__Internal")] private static extern string YG_CloudLoad();
        [DllImport("__Internal")] private static extern void YG_CloudSave(string json);
#endif

        public static bool IsAvailable
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return YG_IsAvailable() == 1;
#else
                return false;
#endif
            }
        }

        /// <summary>ISO 639-1 language of the Yandex portal ("ru", "en", "tr"...), empty outside Yandex.</summary>
        public static string Language
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return IsAvailable ? YG_GetLanguage() : "";
#else
                return "";
#endif
            }
        }

        public static string DeviceType
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return IsAvailable ? YG_GetDeviceType() : "";
#else
                return "";
#endif
            }
        }

        #region Loading and gameplay markup

        private static bool readySent;

        /// <summary>LoadingAPI.ready(): the player can interact with the game (called once).</summary>
        public static void GameReady()
        {
            if (readySent) return;
            readySent = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable) YG_Ready();
#endif
            // gameplay markup only starts after ready
            UpdateGameplay();
        }

        private static bool gameplayWanted;
        private static bool gameplayReported;

        /// <summary>The game says whether a level is being played; ads and hidden tabs suspend it.</summary>
        public static void SetGameplay(bool active)
        {
            gameplayWanted = active;
            UpdateGameplay();
        }

        private static void UpdateGameplay()
        {
            bool active = readySent && gameplayWanted && pauseReasons.Count == 0;
            if (active == gameplayReported) return;
            gameplayReported = active;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsAvailable) return;
            if (active) YG_GameplayStart(); else YG_GameplayStop();
#endif
        }

        #endregion

        #region Pause (hidden tab, ads, platform pause)

        private static readonly HashSet<string> pauseReasons = new HashSet<string>();
        private static float savedTimeScale = 1f;
        private static bool savedAudioPause;

        public static bool IsPaused => pauseReasons.Count > 0;

        public static void Pause(string reason)
        {
            bool wasPaused = pauseReasons.Count > 0;
            if (!pauseReasons.Add(reason) || wasPaused)
            {
                UpdateGameplay();
                return;
            }

            savedTimeScale = Time.timeScale;
            savedAudioPause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            UpdateGameplay();
        }

        public static void Resume(string reason)
        {
            if (!pauseReasons.Remove(reason) || pauseReasons.Count > 0) return;

            Time.timeScale = savedTimeScale;
            AudioListener.pause = savedAudioPause;
            UpdateGameplay();
        }

        #endregion

        #region Ads

        private static Action<bool> fullscreenCallback;
        private static Action<bool> rewardedCallback;

        public static void ShowFullscreen(Action<bool> onClosed)
        {
            fullscreenCallback = onClosed;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable)
            {
                Pause("ad");
                YG_ShowFullscreen();
                return;
            }
#endif
            FinishFullscreen(false);
        }

        public static void ShowRewarded(Action<bool> onClosed)
        {
            rewardedCallback = onClosed;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable)
            {
                Pause("ad");
                YG_ShowRewarded();
                return;
            }
#endif
            // editor / other platforms: grant the reward so the flow can be tested
            FinishRewarded(true);
        }

        internal static void FinishFullscreen(bool shown)
        {
            Resume("ad");
            Action<bool> callback = fullscreenCallback;
            fullscreenCallback = null;
            callback?.Invoke(shown);
        }

        internal static void FinishRewarded(bool rewarded)
        {
            Resume("ad");
            Action<bool> callback = rewardedCallback;
            rewardedCallback = null;
            callback?.Invoke(rewarded);
        }

        #endregion

        #region Purchases

        [Serializable]
        public class Product
        {
            public string id;
            public string title;
            public string description;
            public string imageURI;
            public string price;
            public string priceValue;
            public string priceCurrencyCode;
            public string currencyImage;
        }

        [Serializable]
        public class Purchase
        {
            public string productID;
            public string purchaseToken;
        }

        [Serializable] private class ProductList { public List<Product> items = new List<Product>(); }
        [Serializable] private class PurchaseList { public List<Purchase> items = new List<Purchase>(); }

        private static List<T> ParseList<T, TList>(string json, Func<TList, List<T>> items)
        {
            if (string.IsNullOrEmpty(json) || json == "[]") return new List<T>();
            try { return items(JsonUtility.FromJson<TList>("{\"items\":" + json + "}")); }
            catch (Exception e) { Debug.LogWarning("[Yandex] Bad JSON: " + e.Message); return new List<T>(); }
        }

        /// <summary>Products configured in the Developer Console (empty when purchases are unavailable).</summary>
        public static List<Product> GetCatalog()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable) return ParseList<Product, ProductList>(YG_GetCatalog(), l => l.items);
#endif
            return new List<Product>();
        }

        /// <summary>Purchases made but not consumed yet (checked at start, as moderation requires).</summary>
        public static List<Purchase> GetUnprocessedPurchases()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable) return ParseList<Purchase, PurchaseList>(YG_GetPurchases(), l => l.items);
#endif
            return new List<Purchase>();
        }

        private static Action<Purchase> purchaseSucceeded;
        private static Action<string> purchaseFailed;

        public static void BuyProduct(string id, Action<Purchase> onSuccess, Action<string> onFail)
        {
            purchaseSucceeded = onSuccess;
            purchaseFailed = onFail;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable)
            {
                YG_Purchase(id);
                return;
            }
#endif
            FinishPurchase(null, id);
        }

        internal static void FinishPurchase(Purchase purchase, string failedId)
        {
            Action<Purchase> success = purchaseSucceeded;
            Action<string> fail = purchaseFailed;
            purchaseSucceeded = null;
            purchaseFailed = null;
            if (purchase != null) success?.Invoke(purchase);
            else fail?.Invoke(failedId);
        }

        public static void Consume(string token)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable && !string.IsNullOrEmpty(token)) YG_Consume(token);
#endif
        }

        #endregion

        #region Cloud save

        [Serializable]
        public class CloudSave
        {
            public string save;
            public double t;
        }

        /// <summary>Progress stored on the Yandex server for this player (null if none).</summary>
        public static CloudSave LoadCloud()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable)
            {
                string json = YG_CloudLoad();
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        CloudSave cloud = JsonUtility.FromJson<CloudSave>(json);
                        if (cloud != null && !string.IsNullOrEmpty(cloud.save)) return cloud;
                    }
                    catch (Exception e) { Debug.LogWarning("[Yandex] Bad cloud save: " + e.Message); }
                }
            }
#endif
            return null;
        }

        /// <summary>Queues the save for the server (the page throttles the requests).</summary>
        public static void SaveCloud(string save, double timestamp)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsAvailable) YG_CloudSave(JsonUtility.ToJson(new CloudSave() { save = save, t = timestamp }));
#endif
        }

        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBridge()
        {
            readySent = false;
            gameplayWanted = false;
            gameplayReported = false;
            pauseReasons.Clear();

            GameObject bridge = new GameObject(YandexBridge.OBJECT_NAME);
            UnityEngine.Object.DontDestroyOnLoad(bridge);
            bridge.AddComponent<YandexBridge>();
        }
    }
}
