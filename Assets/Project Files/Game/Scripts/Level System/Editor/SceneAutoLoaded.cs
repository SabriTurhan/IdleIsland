using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    [InitializeOnLoad]
    public static class SceneAutoLoaded
    {
        static SceneAutoLoaded()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

            RewriteStartScene();
        }

        private static void RewriteStartScene()
        {
            WorldBehavior world = GameObject.FindAnyObjectByType<WorldBehavior>(FindObjectsInactive.Include);
            if(world != null)
            {
                WorldsDatabase worldsDatabase = EditorUtils.GetAsset<WorldsDatabase>();
                if (worldsDatabase != null)
                {
                    WorldData worldData = worldsDatabase.GetWorldByName(world.gameObject.scene.name);
                    if (worldData != null)
                    {
                        Serializer.Init();
                        GlobalSave globalSave = SaveController.GetGlobalSave();

                        WorldGlobalSave globalLevelSaveData = globalSave.GetSaveObject<WorldGlobalSave>("worldGlobal");
                        globalLevelSaveData.worldID = worldData.ID;

                        SaveController.SaveCustom(globalSave);
                    }
                }

                ActivateGameScene();

                return;
            }

            SubworldBehavior subworld = GameObject.FindAnyObjectByType<SubworldBehavior>();
            if(subworld != null)
            {
                ActivateGameScene();

                return;
            }

            EditorSceneManager.playModeStartScene = null;
        }

        private static void ActivateGameScene()
        {
            SceneAsset gameScene = EditorUtils.GetAsset<SceneAsset>("Game");
            if (gameScene != null)
            {
                EditorSceneManager.playModeStartScene = gameScene;
            }
        }

        private static void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            RewriteStartScene();
        }
    }
}
