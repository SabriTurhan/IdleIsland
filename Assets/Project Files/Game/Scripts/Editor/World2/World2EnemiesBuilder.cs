using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace IslandEscape.World2
{
    // World 2 enemies built from the template skeleton (same rig and animations) + the sword upgrade
    public static class World2EnemiesBuilder
    {
        private const string SKELETON_PREFAB = "Assets/Project Files/Game/Prefabs/Enemies/Skeleton Enemy.prefab";
        private const string ENEMIES_DATABASE = "Assets/Project Files/Data/Enemies Database.asset";
        private const string CHARACTERS_MATERIAL = "Assets/Project Files/Game/Materials/Characters.mat";
        private const string UPGRADES_DATABASE = "Assets/Project Files/Data/Upgrades/Global Upgrades Database.asset";
        private const string GATHERING_UPGRADE = "Assets/Project Files/Data/Upgrades/Player Gathering Upgrade.asset";
        public const string SWORD_UPGRADE = "Assets/Project Files/Data/Upgrades/Sword Upgrade.asset";
        private const string ENEMIES_FOLDER = PirateArtAssets.ROOT + "/Prefabs/Enemies";

        private class EnemyDefinition
        {
            public EnemyType Type;
            public string Name;
            public bool Boss;
            public float Health;
            public int Damage;
            public float AttackRange;
            public float Speed;
            public float Scale;
            public float Detection;
            public float Leash;
            public int Coins;
            public Color Tint;
            public Color Emission;
            public System.Func<Mesh>[] HeadAccessories;
            public bool Cutlass;
        }

        private static readonly EnemyDefinition[] ENEMIES =
        {
            new EnemyDefinition
            {
                Type = EnemyType.PirateSkeleton, Name = "Pirate Skeleton", Health = 8, Damage = 8, AttackRange = 1.05f, Speed = 1.8f, Scale = 1f,
                Detection = 6f, Leash = 12f, Coins = 8, Tint = new Color(1f, 0.96f, 0.9f), Emission = Color.black,
                HeadAccessories = new System.Func<Mesh>[] { PirateModels.Bandana }, Cutlass = true,
            },
            new EnemyDefinition
            {
                Type = EnemyType.SkeletonBrute, Name = "Skeleton Brute", Health = 24, Damage = 14, AttackRange = 1.5f, Speed = 1.2f, Scale = 1.4f,
                Detection = 6.5f, Leash = 13f, Coins = 25, Tint = new Color(0.78f, 0.8f, 0.82f), Emission = Color.black,
                HeadAccessories = new System.Func<Mesh>[] { PirateModels.HornedHelmet }, Cutlass = false,
            },
            new EnemyDefinition
            {
                Type = EnemyType.GhostCaptain, Name = "Ghost Captain", Boss = true, Health = 60, Damage = 6, AttackRange = 2.0f, Speed = 1.7f, Scale = 2f,
                Detection = 11f, Leash = 30f, Coins = 300, Tint = new Color(0.55f, 1f, 0.95f), Emission = new Color(0.12f, 0.45f, 0.42f),
                HeadAccessories = new System.Func<Mesh>[] { PirateModels.TricornHat, PirateModels.GhostBeard, PirateModels.GlowingEyes }, Cutlass = true,
            },
        };

        [MenuItem("Tools/World 2/2. Build Enemies And Sword", priority = 2)]
        public static void BuildAll()
        {
            Material pirateMaterial = AssetDatabase.LoadAssetAtPath<Material>(PirateArtAssets.MATERIAL_PATH) ?? PirateArtAssets.BuildMaterials();
            Material ghostAccessoryMaterial = AssetDatabase.LoadAssetAtPath<Material>(PirateArtAssets.MATERIAL_GHOST_PATH);

            if (!AssetDatabase.IsValidFolder(ENEMIES_FOLDER))
                AssetDatabase.CreateFolder(PirateArtAssets.ROOT + "/Prefabs", "Enemies");

            Mesh cutlass = PirateArtAssets.SaveMesh(PirateModels.Cutlass());
            Dictionary<EnemyType, GameObject> prefabs = new Dictionary<EnemyType, GameObject>();

            foreach (EnemyDefinition definition in ENEMIES)
                prefabs[definition.Type] = BuildEnemy(definition, pirateMaterial, ghostAccessoryMaterial, cutlass);

            RegisterEnemies(prefabs);
            BuildSwordUpgrade(pirateMaterial, cutlass);
            BuildExtraIcons(pirateMaterial);

            AssetDatabase.SaveAssets();
            Debug.Log("[World 2] Enemies and sword built");
        }

        private static GameObject BuildEnemy(EnemyDefinition definition, Material accessoryMaterial, Material ghostMaterial, Mesh cutlass)
        {
            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(SKELETON_PREFAB);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = definition.Name;

            SkeletonEnemyBehavior behavior = instance.GetComponent<SkeletonEnemyBehavior>();
            SerializedObject serialized = new SerializedObject(behavior);

            // the boss swaps the script (GhostCaptainBehavior derives from SkeletonEnemyBehavior, every field is kept)
            if (definition.Boss)
            {
                MonoScript bossScript = MonoScript.FromMonoBehaviour(instance.AddComponent<GhostCaptainBehavior>());
                Object.DestroyImmediate(instance.GetComponent<GhostCaptainBehavior>());
                serialized.FindProperty("m_Script").objectReferenceValue = bossScript;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                behavior = instance.GetComponent<SkeletonEnemyBehavior>();
                serialized = new SerializedObject(behavior);
                serialized.FindProperty("slamMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(PirateArtAssets.MATERIAL_GHOST_PATH);
            }

            serialized.FindProperty("health").floatValue = definition.Health;
            serialized.FindProperty("enemyType").intValue = (int)definition.Type;
            serialized.FindProperty("attackRange").floatValue = definition.AttackRange;
            serialized.FindProperty("attackDamage").intValue = definition.Damage;
            serialized.FindProperty("detectionDistance").floatValue = definition.Detection;
            serialized.FindProperty("leashDistance").floatValue = definition.Leash;
            SerializedProperty drop = serialized.FindProperty("drop");
            drop.arraySize = 1;
            drop.GetArrayElementAtIndex(0).FindPropertyRelative("currency").intValue = (int)CurrencyType.Coins;
            drop.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = definition.Coins;
            serialized.FindProperty("dropRate").intValue = Mathf.Max(1, definition.Coins / 5);
            if (definition.Boss)
                serialized.FindProperty("flashEmissionColor").colorValue = new Color(0.6f, 1f, 0.95f) * 0.6f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
            agent.speed = definition.Speed;
            agent.radius *= Mathf.Lerp(1f, definition.Scale, 0.7f);

            SphereCollider collider = instance.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius *= definition.Scale;
                collider.center *= definition.Scale;
            }

            // Spawn() resets the root scale, so the model is scaled instead
            Transform model = instance.transform.Find("Skeleton_1");
            model.localScale = Vector3.one * definition.Scale;

            Transform healthBar = instance.transform.Find("Healthbar Holder");
            if (healthBar != null) healthBar.localPosition = new Vector3(0, 2f * definition.Scale + (definition.Boss ? 0.6f : 0f), 0);

            SkinnedMeshRenderer body = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            body.sharedMaterial = BodyMaterial(definition);

            Transform head = FindBone(model, "Head");
            foreach (System.Func<Mesh> accessory in definition.HeadAccessories)
            {
                Mesh mesh = PirateArtAssets.SaveMesh(accessory());
                // ghost parts glow, clothes keep their colours
                bool glowing = mesh.name.Contains("ghost") || mesh.name.Contains("glowing");
                Attach(head, mesh.name, mesh, glowing ? ghostMaterial : accessoryMaterial);
            }

            if (definition.Cutlass)
            {
                Transform hand = FindBone(model, "RightHand");
                GameObject sword = Attach(hand, "Cutlass", cutlass, accessoryMaterial);
                sword.transform.localPosition = new Vector3(0, 0.08f, 0.02f);
                sword.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }

            string path = ENEMIES_FOLDER + "/" + definition.Name + " Enemy.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);

            return prefab;
        }

        private static GameObject Attach(Transform bone, string name, Mesh mesh, Material material)
        {
            Transform old = bone.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject accessory = new GameObject(name);
            accessory.transform.SetParent(bone, false);
            // the template rig has unit scale bones; meshes are authored in the bone's space
            accessory.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = accessory.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return accessory;
        }

        private static Transform FindBone(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }

            return root;
        }

        private static Material BodyMaterial(EnemyDefinition definition)
        {
            string path = PirateArtAssets.ROOT + "/Materials/Characters " + definition.Name + ".mat";
            Material template = AssetDatabase.LoadAssetAtPath<Material>(CHARACTERS_MATERIAL);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(template);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.CopyPropertiesFromMaterial(template);
            }

            material.SetColor("_Color", definition.Tint);
            if (definition.Emission.maxColorComponent > 0)
            {
                material.EnableKeyword("EMISSION_ON");
                material.SetFloat("_EmissionOn", 1f);
                material.SetColor("_EmissionColor", definition.Emission);
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private static void RegisterEnemies(Dictionary<EnemyType, GameObject> prefabs)
        {
            Object database = AssetDatabase.LoadAssetAtPath<Object>(ENEMIES_DATABASE);
            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty enemies = serialized.FindProperty("enemies");

            foreach (KeyValuePair<EnemyType, GameObject> pair in prefabs)
            {
                int index = -1;
                for (int i = 0; i < enemies.arraySize; i++)
                {
                    if (enemies.GetArrayElementAtIndex(i).FindPropertyRelative("enemyType").intValue == (int)pair.Key)
                        index = i;
                }

                if (index < 0)
                {
                    enemies.arraySize++;
                    index = enemies.arraySize - 1;
                }

                enemies.GetArrayElementAtIndex(index).FindPropertyRelative("enemyType").intValue = (int)pair.Key;
                enemies.GetArrayElementAtIndex(index).FindPropertyRelative("prefab").objectReferenceValue = pair.Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        // sword levels, paid with coins (the gold kept from World 1 finally has a use)
        private static void BuildSwordUpgrade(Material material, Mesh cutlass)
        {
            Sprite icon;
            using (PirateIconRenderer renderer = new PirateIconRenderer())
            {
                Texture2D texture = renderer.RenderMesh(cutlass, material, new Vector3(0, 0, -40));
                icon = PirateIconRenderer.SaveSprite(texture, PirateArtAssets.ROOT + "/Icons/ic_sword.png", "Assets/Project Files/Game/Images/Drop/ic_stone.png");
            }

            SwordUpgrade upgrade = AssetDatabase.LoadAssetAtPath<SwordUpgrade>(SWORD_UPGRADE);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<SwordUpgrade>();
                AssetDatabase.CreateAsset(upgrade, SWORD_UPGRADE);
            }

            SerializedObject gathering = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(GATHERING_UPGRADE));
            SerializedObject serialized = new SerializedObject(upgrade);
            serialized.FindProperty("globalUpgradeType").intValue = (int)GlobalUpgradeType.Sword;
            serialized.FindProperty("title").stringValue = "Sword";
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("descriptionFormat").stringValue = gathering.FindProperty("descriptionFormat").stringValue;

            int[] prices = { 0, 150, 400, 900, 1800 };
            int[] damage = { 1, 2, 3, 5, 8 };
            SerializedProperty stages = serialized.FindProperty("upgrades");
            stages.arraySize = prices.Length;
            for (int i = 0; i < prices.Length; i++)
            {
                SerializedProperty stage = stages.GetArrayElementAtIndex(i);
                stage.FindPropertyRelative("price").intValue = prices[i];
                stage.FindPropertyRelative("currencyType").intValue = (int)CurrencyType.Coins;
                stage.FindPropertyRelative("damage").intValue = damage[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);

            Object database = AssetDatabase.LoadAssetAtPath<Object>(UPGRADES_DATABASE);
            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty upgrades = serializedDatabase.FindProperty("upgrades");
            bool registered = false;
            for (int i = 0; i < upgrades.arraySize; i++)
                registered |= upgrades.GetArrayElementAtIndex(i).objectReferenceValue == upgrade;

            if (!registered)
            {
                upgrades.arraySize++;
                upgrades.GetArrayElementAtIndex(upgrades.arraySize - 1).objectReferenceValue = upgrade;
                serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(database);
            }
        }

        // icons used in texts without being currencies (kill missions)
        private static void BuildExtraIcons(Material material)
        {
            using (PirateIconRenderer renderer = new PirateIconRenderer())
            {
                Mesh skull = PirateArtAssets.SaveMesh(PirateModels.Skull());
                Texture2D texture = renderer.RenderMesh(skull, material, new Vector3(0, 200, 0));
                PirateIconRenderer.SaveSprite(texture, PirateArtAssets.ROOT + "/Icons/ic_skull.png", "Assets/Project Files/Game/Images/Drop/ic_stone.png");
            }

            World2ResourcesBuilder.RebuildSpriteAtlas();
        }
    }
}
