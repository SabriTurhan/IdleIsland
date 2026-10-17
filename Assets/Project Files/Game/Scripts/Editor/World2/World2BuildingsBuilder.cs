using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Watermelon.World2
{
    // World 2 buildings made from the template buildings (same purchase/construction/converter logic, new visuals)
    public static class World2BuildingsBuilder
    {
        private const string TEMPLATES = "Assets/Project Files/Game/Prefabs/Buildings/";
        public const string FOLDER = PirateArtAssets.ROOT + "/Prefabs/Buildings";
        public const string PROPS_FOLDER = PirateArtAssets.ROOT + "/Prefabs/Props";

        public const string TORCH_TABLE = FOLDER + "/Torch Table.prefab";
        public const string FORGE = FOLDER + "/Blacksmith Forge.prefab";
        public const string SAIL_LOOM = FOLDER + "/Sail Loom.prefab";
        public const string PIRATE_SHIP = FOLDER + "/Pirate Ship Building.prefab";
        public const string CURSED_TILE = FOLDER + "/Cursed Land Tile.prefab";
        public const string CURSED_TALL_TILE = FOLDER + "/Cursed Land Tile Tall.prefab";

        [MenuItem("Tools/World 2/3. Build Buildings And Props", priority = 3)]
        public static void BuildAll()
        {
            foreach (string folder in new[] { FOLDER, PROPS_FOLDER })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder(PirateArtAssets.ROOT + "/Prefabs", System.IO.Path.GetFileName(folder));
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(PirateArtAssets.MATERIAL_PATH);
            Material glow = AssetDatabase.LoadAssetAtPath<Material>(PirateArtAssets.MATERIAL_GLOW_PATH);

            FixTemplateUpgradeIcons();
            BuildTorchTable(material, glow);
            BuildForge(material, glow);
            BuildSailLoom(material);
            BuildShip(material);
            BuildCursedTile(material, "Land Tile With Obstacle", CURSED_TILE, 3);
            BuildCursedTile(material, "Land Tile Tall With Obstacle", CURSED_TALL_TILE, 4);
            BuildProps(material, glow);

            AssetDatabase.SaveAssets();
            Debug.Log("[World 2] Buildings and props built");
        }

        private static GameObject Instantiate(string template, string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TEMPLATES + template + ".prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = name;

            return instance;
        }

        private static GameObject Save(GameObject instance, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);

            return prefab;
        }

        private static void SetRecipe(ResourceConverterBuildingBehavior converter, CurrencyType result, params (CurrencyType type, int amount)[] components)
        {
            SerializedObject serialized = new SerializedObject(converter);
            SerializedProperty recipe = serialized.FindProperty("recipe");
            recipe.FindPropertyRelative("resultResourceType").intValue = (int)result;
            SerializedProperty list = recipe.FindPropertyRelative("components");
            list.arraySize = components.Length;
            for (int i = 0; i < components.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("resourceType").intValue = (int)components[i].type;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = components[i].amount;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 euler)
        {
            Transform old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localRotation = Quaternion.Euler(euler);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = material;

            return child;
        }

        private static void BuildTorchTable(Material material, Material glow)
        {
            // from the planks table: unlike the scissors table it has a purchase point, so it has to be built
            GameObject instance = Instantiate("Planks Craft Table", "Torch Table");
            ResourceConverterBuildingBehavior converter = instance.GetComponentInChildren<ResourceConverterBuildingBehavior>(true);
            converter.name = "Torch Converter";
            SetRecipe(converter, CurrencyType.Torch, (CurrencyType.Wood, 1), (CurrencyType.Fiber, 1));

            Transform table = converter.transform.Find("Opened Visuals/Table");
            for (int i = table.childCount - 1; i >= 0; i--) Object.DestroyImmediate(table.GetChild(i).gameObject);
            Mesh barrel = PirateArtAssets.SaveMesh(PirateModels.TorchBarrel());
            AddMesh(table, "Torch Barrel", barrel, material, new Vector3(0.0f, 0.0f, -0.85f), Vector3.zero);

            Save(instance, TORCH_TABLE);
        }

        private static void BuildForge(Material material, Material glow)
        {
            GameObject instance = Instantiate("Bricks Craft Table", "Blacksmith Forge");
            ResourceConverterBuildingBehavior converter = instance.GetComponentInChildren<ResourceConverterBuildingBehavior>(true);
            converter.name = "Forge Converter";

            // converter -> forge (sells sword levels on its upgrade button), every serialized field is kept
            MonoScript forgeScript = MonoScript.FromMonoBehaviour(converter.gameObject.AddComponent<BlacksmithForgeBehavior>());
            Object.DestroyImmediate(converter.gameObject.GetComponent<BlacksmithForgeBehavior>());
            SerializedObject serialized = new SerializedObject(converter);
            serialized.FindProperty("m_Script").objectReferenceValue = forgeScript;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            converter = instance.GetComponentInChildren<ResourceConverterBuildingBehavior>(true);

            SetRecipe(converter, CurrencyType.IronBar, (CurrencyType.IronOre, 3), (CurrencyType.Wood, 2));

            Transform table = converter.transform.Find("Opened Visuals/Table");
            for (int i = table.childCount - 1; i >= 0; i--) Object.DestroyImmediate(table.GetChild(i).gameObject);
            table.GetComponent<MeshFilter>().sharedMesh = PirateArtAssets.SaveMesh(PirateModels.Forge());
            table.GetComponent<MeshRenderer>().sharedMaterial = material;
            AddMesh(table, "Fire", PirateArtAssets.SaveMesh(PirateModels.ForgeFire()), glow, Vector3.zero, Vector3.zero);

            SetUpgradeInfo(instance, "Forge", PirateArtAssets.ROOT + "/Icons/ic_forge_cap.png", PirateArtAssets.ROOT + "/Icons/ic_forge_spd.png");

            Save(instance, FORGE);
        }

        private static void BuildSailLoom(Material material)
        {
            GameObject instance = Instantiate("Ropes Craft Table", "Sail Loom");
            ResourceConverterBuildingBehavior converter = instance.GetComponentInChildren<ResourceConverterBuildingBehavior>(true);
            converter.name = "Sail Converter";
            SetRecipe(converter, CurrencyType.Sailcloth, (CurrencyType.Fiber, 10));

            Transform table = converter.transform.Find("Opened Visuals/Table");
            for (int i = table.childCount - 1; i >= 0; i--) Object.DestroyImmediate(table.GetChild(i).gameObject);
            table.GetComponent<MeshFilter>().sharedMesh = PirateArtAssets.SaveMesh(PirateModels.SailLoom());
            table.GetComponent<MeshRenderer>().sharedMaterial = material;

            SetUpgradeInfo(instance, "Sail Loom", PirateArtAssets.ROOT + "/Icons/ic_loom_cap.png", PirateArtAssets.ROOT + "/Icons/ic_loom_spd.png");

            Save(instance, SAIL_LOOM);
        }

        private static void BuildShip(Material material)
        {
            GameObject instance = Instantiate("Raft Building", "Pirate Ship Building");
            WorldChangeRaftBehavior raft = instance.GetComponentInChildren<WorldChangeRaftBehavior>(true);
            raft.name = "Ship Behavior";
            raft.transform.localPosition = new Vector3(-6.2f, -0.85f, 0.5f);
            raft.transform.localRotation = Quaternion.Euler(0, 180, 0);

            Transform graphics = raft.transform.Find("Graphics");
            Transform sail = graphics.Find("Sail");
            if (sail != null) sail.gameObject.SetActive(false);

            Transform hull = graphics.Find("Base");
            hull.name = "Ship";
            hull.GetComponent<MeshFilter>().sharedMesh = PirateArtAssets.SaveMesh(PirateModels.PirateShip());
            hull.GetComponent<MeshRenderer>().sharedMaterial = material;
            // the template moves the raft along +X, the ship model is built along +Z
            hull.localRotation = Quaternion.Euler(0, 90, 0);

            Transform holder = graphics.Find("Player Holder");
            holder.localPosition = new Vector3(-0.6f, 1.55f, 0f);

            NavMeshObstacle obstacle = raft.GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                obstacle.center = new Vector3(0, 0.5f, 0);
                obstacle.size = new Vector3(8.5f, 2f, 3f);
            }

            Save(instance, PIRATE_SHIP);
        }

        // obstacle tile whose closed visuals are purple cursed thorns, opened with torches
        private static void BuildCursedTile(Material material, string template, string path, int seed)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Prefabs/Ground/Main Tiles/" + template + ".prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            instance.name = System.IO.Path.GetFileNameWithoutExtension(path);

            Transform closed = null;
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Closed Visuals") closed = child;
            }

            for (int i = closed.childCount - 1; i >= 0; i--) Object.DestroyImmediate(closed.GetChild(i).gameObject);
            GameObject thorns = AddMesh(closed, "Cursed Thorns", PirateArtAssets.SaveMesh(PirateModels.CursedThorns(seed)), material, new Vector3(0, -0.05f, 0), Vector3.zero);

            // the unlock animation shrinks the thorns away (it referenced the template's plant obstacles)
            foreach (ScaleAnimationForUnlockable animation in instance.GetComponentsInChildren<ScaleAnimationForUnlockable>(true))
            {
                SerializedObject serialized = new SerializedObject(animation);
                SerializedProperty hide = serialized.FindProperty("objectsToHide");
                hide.arraySize = 1;
                hide.GetArrayElementAtIndex(0).objectReferenceValue = thorns.transform;
                SerializedProperty appear = serialized.FindProperty("objectsToAppear");
                for (int i = appear.arraySize - 1; i >= 0; i--)
                {
                    if (appear.GetArrayElementAtIndex(i).objectReferenceValue == null) appear.DeleteArrayElementAtIndex(i);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            Save(instance, path);
        }

        private const string UPGRADE_ICON_TEMPLATE = "Assets/Project Files/Game/Images/Upgrades/ic_scissors_craft_cap.png";
        private const string UPGRADE_DESCRIPTION = "Increase from <color=#FF4233>{0}</color> to <color=#56D103>{1}";

        // capacity / speed upgrades of a converter: icon (product + badge), title and description like the scissors table
        private static void SetUpgradeInfo(GameObject root, string name, string capacityIcon, string speedIcon)
        {
            foreach (AbstractLocalUpgrade upgrade in root.GetComponentsInChildren<AbstractLocalUpgrade>(true))
            {
                bool capacity = upgrade is ConverterCapacityUpgrade;
                bool speed = upgrade is SimpleFloatUpgrade;
                if (!capacity && !speed) continue;

                SerializedObject serialized = new SerializedObject(upgrade);
                serialized.FindProperty("title").stringValue = name + (capacity ? " Capacity" : " Speed");
                serialized.FindProperty("icon").objectReferenceValue = ImportUpgradeIcon(capacity ? capacityIcon : speedIcon);
                serialized.FindProperty("descriptionFormat").stringValue = UPGRADE_DESCRIPTION;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Sprite ImportUpgradeIcon(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            TextureImporter template = (TextureImporter)AssetImporter.GetAtPath(UPGRADE_ICON_TEMPLATE);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                template.ReadTextureSettings(settings);
                importer.SetTextureSettings(settings);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // the template's bricks and ropes tables shipped without upgrade icons (ropes icons existed but were not assigned)
        private static void FixTemplateUpgradeIcons()
        {
            (string prefab, string name, string capacity, string speed)[] tables =
            {
                ("Bricks Craft Table", "Bricks Craft", "Assets/Project Files/Game/Images/Upgrades/ic_bricks_craft_cap.png", "Assets/Project Files/Game/Images/Upgrades/ic_bricks_craft_spd.png"),
                ("Ropes Craft Table", "Ropes Craft", "Assets/Project Files/Game/Images/Upgrades/ic_ropes_craft_cap.png", "Assets/Project Files/Game/Images/Upgrades/ic_ropes_craft_spd.png"),
            };

            foreach (var table in tables)
            {
                string path = TEMPLATES + table.prefab + ".prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                SetUpgradeInfo(contents, table.name, table.capacity, table.speed);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private class PropDefinition
        {
            public string Name;
            public Mesh Mesh;
            public bool Glow;
            public Vector3 ObstacleSize;
        }

        private static void BuildProps(Material material, Material glow)
        {
            List<PropDefinition> props = new List<PropDefinition>
            {
                new PropDefinition { Name = "Barrel", Mesh = PirateModels.BarrelProp(), ObstacleSize = new Vector3(0.8f, 1f, 0.8f) },
                new PropDefinition { Name = "Crate", Mesh = PirateModels.Crate(), ObstacleSize = new Vector3(0.9f, 1f, 0.9f) },
                new PropDefinition { Name = "Cannon", Mesh = PirateModels.Cannon(), ObstacleSize = new Vector3(0.9f, 1f, 1.5f) },
                new PropDefinition { Name = "Skull Flag", Mesh = PirateModels.SkullFlag(), ObstacleSize = new Vector3(0.3f, 1f, 0.3f) },
                new PropDefinition { Name = "Lantern Post", Mesh = PirateModels.LanternPost(), ObstacleSize = new Vector3(0.3f, 1f, 0.3f) },
                new PropDefinition { Name = "Treasure Pile", Mesh = PirateModels.TreasurePile(), ObstacleSize = new Vector3(1.4f, 1f, 1.2f) },
                new PropDefinition { Name = "Dead Tree", Mesh = PirateModels.DeadTree(5), ObstacleSize = new Vector3(0.6f, 1f, 0.6f) },
                new PropDefinition { Name = "Cursed Thorns", Mesh = PirateModels.CursedThorns(9), ObstacleSize = new Vector3(4.5f, 1f, 4.5f) },
            };

            // tombstone from the template's unused model
            Mesh tombstone = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Project Files/Game/Models/Buildings/tombstone.fbx"))
                if (asset is Mesh mesh) tombstone = mesh;
            if (tombstone != null)
            {
                GameObject prop = new GameObject("Tombstone");
                prop.AddComponent<MeshFilter>().sharedMesh = tombstone;
                prop.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Project Files/Game/Materials/Buildings.mat");
                NavMeshObstacle obstacle = prop.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.size = Vector3.Max(tombstone.bounds.size, new Vector3(0.4f, 1f, 0.4f));
                obstacle.center = tombstone.bounds.center;
                Save(prop, PROPS_FOLDER + "/Tombstone.prefab");
            }

            foreach (PropDefinition definition in props)
            {
                GameObject prop = new GameObject(definition.Name);
                prop.AddComponent<MeshFilter>().sharedMesh = PirateArtAssets.SaveMesh(definition.Mesh);
                prop.AddComponent<MeshRenderer>().sharedMaterial = definition.Glow ? glow : material;

                NavMeshObstacle obstacle = prop.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.size = definition.ObstacleSize;
                obstacle.center = new Vector3(0, definition.ObstacleSize.y * 0.5f, 0);

                Save(prop, PROPS_FOLDER + "/" + definition.Name + ".prefab");
            }
        }
    }
}
