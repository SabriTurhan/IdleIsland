using UnityEngine;

namespace IslandEscape
{
    /// <summary>Yandex Games: purchase UI is shown only when the console has active products.</summary>
    public static class YandexStore
    {
        public static bool PurchasesOffered => !YandexGames.IsAvailable || YandexIAPWrapper.HasProducts;
    }

    /// <summary>
    /// Connects the game to the Yandex Games platform layer:
    /// - LoadingAPI.ready() once the loading screen is gone and the island is playable
    /// - GameplayAPI start/stop: gameplay = the game screen alone on screen (no pause, store, upgrades page,
    ///   popup or story cinematic), ads and hidden tabs suspend it (YandexGames)
    /// - a fullscreen ad at the logical pause between missions, never in the first missions
    /// - purchases paid in an earlier session but not granted yet open the store, which grants them
    /// </summary>
    public class YandexGameFlow : MonoBehaviour
    {
        private const int FIRST_AD_MISSION = 5;
        private const float CHECK_INTERVAL = 0.25f;

        private float nextCheck;
        private bool pendingPurchasesChecked;
        private float worldLoadedTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            GameObject flow = new GameObject("Yandex Game Flow");
            DontDestroyOnLoad(flow);
            flow.AddComponent<YandexGameFlow>();
        }

        private void OnEnable()
        {
            GameLoading.OnLoadingFinished += OnLoadingFinished;
            WorldController.OnWorldLoaded += OnWorldLoaded;
            MissionsController.OnNextMissionStarted += OnNextMissionStarted;
        }

        private void OnDisable()
        {
            GameLoading.OnLoadingFinished -= OnLoadingFinished;
            WorldController.OnWorldLoaded -= OnWorldLoaded;
            MissionsController.OnNextMissionStarted -= OnNextMissionStarted;
        }

        /// <summary>Language of the portal, read through the SDK (requirement 2.14). The game ships in English only.</summary>
        public static string PortalLanguage { get; private set; } = "en";

        private void Start()
        {
            string language = YandexGames.Language;
            if (!string.IsNullOrEmpty(language)) PortalLanguage = language;
            Debug.Log("[Yandex] portal language: " + PortalLanguage + " (game text: en)");
        }

        private void OnLoadingFinished()
        {
            YandexGames.GameReady();
        }

        private void OnWorldLoaded()
        {
            worldLoadedTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CHECK_INTERVAL;

            bool playing = PlayerBehavior.GetBehavior() != null
                && UIController.IsOnlyPageDisplayed<UIGame>()
                && !UIController.IsPopupOpened
                && !World2Story.IsPlaying;
            YandexGames.SetGameplay(playing);

            if (!pendingPurchasesChecked && playing && YandexIAPWrapper.HasPending && YandexStore.PurchasesOffered)
            {
                pendingPurchasesChecked = true;
                UIController.ShowPage<IAPStore.UIStore>();
            }
        }

        // the player has just claimed a mission reward and the next task appears: a natural pause
        private void OnNextMissionStarted()
        {
            if (!YandexGames.IsAvailable || World2Story.IsPlaying) return;
            if (Time.unscaledTime - worldLoadedTime < 10f) return; // the first mission of a freshly loaded world

            Mission mission = MissionsController.ActiveMission;
            if (mission == null || mission.transform.GetSiblingIndex() < FIRST_AD_MISSION) return;

            AdsManager.ShowInterstitial(null);
        }
    }
}
