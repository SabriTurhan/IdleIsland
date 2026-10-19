using System.Threading.Tasks;

namespace IslandEscape
{
    /// <summary>
    /// Yandex Games ads (interstitial = fullscreen ad, rewarded video). Ad units are served and rate-limited by
    /// the platform, so they are always "loaded"; sticky banners are enabled in the Developer Console.
    /// </summary>
    public class YandexAdHandler : AdProviderHandler
    {
        public YandexAdHandler(AdProvider providerType) : base(providerType) { }

        protected override Task<bool> InitProviderAsync()
        {
            return Task.FromResult(true);
        }

        public override void ShowBanner() { }
        public override void HideBanner() { }
        public override void DestroyBanner() { }

        public override void RequestInterstitial()
        {
            AdsManager.OnProviderAdLoaded(providerType, AdType.Interstitial);
        }

        public override bool IsInterstitialLoaded() => YandexGames.IsAvailable;

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            AdsManager.OnProviderAdDisplayed(providerType, AdType.Interstitial);

            YandexGames.ShowFullscreen(shown =>
            {
                AdsManager.ExecuteInterstitialCallback(shown);
                AdsManager.OnProviderAdClosed(providerType, AdType.Interstitial);
            });
        }

        public override void RequestRewardedVideo()
        {
            AdsManager.OnProviderAdLoaded(providerType, AdType.RewardedVideo);
        }

        // outside Yandex (editor) YandexGames grants the reward directly so the flow stays testable
        public override bool IsRewardedVideoLoaded() => true;

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            AdsManager.OnProviderAdDisplayed(providerType, AdType.RewardedVideo);

            YandexGames.ShowRewarded(rewarded =>
            {
                AdsManager.ExecuteRewardVideoCallback(rewarded);
                AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
            });
        }
    }
}
