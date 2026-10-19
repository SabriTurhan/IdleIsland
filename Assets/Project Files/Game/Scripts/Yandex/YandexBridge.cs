using UnityEngine;

namespace IslandEscape
{
    /// <summary>Receives SendMessage callbacks from the Yandex page (YandexGames.jslib / index.html).</summary>
    public class YandexBridge : MonoBehaviour
    {
        public const string OBJECT_NAME = "YandexBridge";

        // tab hidden / shown (also the SDK's game_api_pause / game_api_resume)
        public void OnVisibility(string visible)
        {
            if (visible == "1") YandexGames.Resume("hidden");
            else YandexGames.Pause("hidden");
        }

        public void OnPlatformPause(string paused)
        {
            if (paused == "1") YandexGames.Pause("platform");
            else YandexGames.Resume("platform");
        }

        public void OnAdOpened(string _) { }

        public void OnFullscreenClosed(string shown) => YandexGames.FinishFullscreen(shown == "1");

        public void OnRewardedClosed(string rewarded) => YandexGames.FinishRewarded(rewarded == "1");

        public void OnPurchaseSucceeded(string json) => YandexGames.FinishPurchase(JsonUtility.FromJson<YandexGames.Purchase>(json), null);

        public void OnPurchaseFailed(string id) => YandexGames.FinishPurchase(null, id);
    }
}
