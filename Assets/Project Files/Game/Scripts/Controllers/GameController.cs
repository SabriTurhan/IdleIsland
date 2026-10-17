using UnityEngine;
using UnityEngine.SceneManagement;

namespace IslandEscape
{
    [DefaultExecutionOrder(-10)]
    public class GameController : MonoBehaviour
    {
        private static GameController instance;

        [SerializeField] GameData gameData;
        public static GameData Data => instance.gameData;

        [Space]
        [SerializeField] UIController uiController;
        [SerializeField] DefaultMusicController defaultMusicController;
        [SerializeField] CameraController cameraController;

        private static WorldController worldController;
        private static GlobalUpgradesController globalUpgradesController;
        private static MissionsController missionsController;
        private static ParticlesController particlesController;
        private static NavigationHelper navigationHelper;
        private static FloatingTextController floatingTextController;
        private static UnlockableToolsController unlockableToolsController;
        private static FishingController fishingController;
        private static DiggingController diggingController;
        private static SkinController skinController;
        private static EnergyController energyController;
        private static EnvironmentController environmentController;

        private void Awake()
        {
            instance = this;

            CacheComponent(out worldController);
            CacheComponent(out globalUpgradesController);
            CacheComponent(out missionsController);
            CacheComponent(out particlesController);
            CacheComponent(out navigationHelper);
            CacheComponent(out floatingTextController);
            CacheComponent(out unlockableToolsController);
            CacheComponent(out fishingController);
            CacheComponent(out diggingController);
            CacheComponent(out skinController);
            CacheComponent(out energyController);
            CacheComponent(out environmentController);

            Data.Init();

            defaultMusicController.Initialise();

            uiController.Init();

            cameraController.Initialise();
            PreviewCamera.Initialise();

            skinController.Init();

            unlockableToolsController.Initialise();

            energyController.Initialise();

            environmentController.Initialise();

            fishingController.Initialise();

            diggingController.Initialise();

            globalUpgradesController.Initialise();

            floatingTextController.Init();

            worldController.Initialise();

            uiController.InitPages();

            particlesController.Init();

            navigationHelper.Initialise();
        }

        private void Start()
        {
            if (gameData.UseMainMenu)
            {
                DefaultMusicController.ActivateMusic();

                UIController.ShowPage<UIMainMenu>();
            }
            else
            {
                worldController.LoadCurrentWorld();
            }

            // Move this method to the point when the game is fully loaded
            GameLoading.MarkAsReadyToHide();
        }

        private void OnDestroy()
        {
            Data.Unload(); 
            
            NavigationHelper.Unload();

            DistanceToggle.Unload();

            FishingController.Unload();

            diggingController.Disable();
            diggingController.Unload();

            NavMeshController.Reset();

            Tween.RemoveAll();
        }

        public static void LoadCurrentWorld()
        {
            worldController.LoadCurrentWorld();
        }

        public static void OpenMainMenu()
        {            
            // Show fullscreen black overlay
            Overlay.Show(0.3f, () =>
            {
                // Save the current state of the game
                SaveController.Save(true);

                // Show main menu
                UIController.ShowPage<UIMainMenu>();

                // Unload the current world and all the dependencies
                GameController.UnloadWorld(() =>
                {
                    DefaultMusicController.ActivateMusic();

                    UIGamepadButton.DisableAllTags();
                    UIGamepadButton.EnableTag(UIGamepadButtonTag.MainMenu);

                    // Disable fullscreen black overlay
                    Overlay.Hide(0.3f);
                });
            }, true);
        }

        public static void OnWorldLoaded(WorldBehavior worldBehavior)
        {
            UIController.ShowPage<UIGame>();

            fishingController.SpawnFishingPlaces();

            missionsController.Initialise(worldBehavior.MissionsHolder?.Missions);

            diggingController.Activate(worldBehavior.DiggingSpawnSettings);
        }

        public static void UnloadWorld(SimpleCallback onUnloaded)
        {
            Tween.RemoveAll();

            NavigationHelper.Unload();

            DistanceToggle.Unload();

            FishingController.Unload();

            diggingController.Disable();
            diggingController.Unload();

            worldController.UnloadWorld(onUnloaded);
        }

        public static void LoadWorld(string worldID, SimpleCallback onWorldUnloaded = null, SimpleCallback onNewWorldLoaded = null)
        {
            UIController.HidePage<UIGame>();

            // Show fullscreen black overlay
            Overlay.Show(0.3f, () =>
            {
                // Save the current state of the game
                SaveController.Save(true);

                // Unload the current world and all the dependencies
                GameController.UnloadWorld(() =>
                {
                    onWorldUnloaded?.Invoke();

                    // Load next world
                    worldController.LoadWorld(worldID);

                    // Disable fullscreen black overlay
                    Overlay.Hide(0.3f, () =>
                    {
                        UIController.ShowPage<UIGame>();

                        onNewWorldLoaded?.Invoke();
                    });
                });
            }, true);
        }

        #region Extensions
        public bool CacheComponent<T>(out T component) where T : Component
        {
            Component unboxedComponent = gameObject.GetComponent(typeof(T));

            if (unboxedComponent != null)
            {
                component = (T)unboxedComponent;

                return true;
            }

            Debug.LogError(string.Format("Scripts Holder doesn't have {0} script added to it", typeof(T)));

            component = null;

            return false;
        }
        #endregion
    }
}