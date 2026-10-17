using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Watermelon.World2
{
    // Builds "World 2 Pirate Cove" from a copy of World 1 (keeps the world systems, tent and cave sub-worlds)
    // and a text map with 5 unit cells. Re-runnable: everything generated is removed and placed again.
    public static class World2MapBuilder
    {
        public const string SCENE_PATH = "Assets/Project Files/Game/Scenes/Worlds/World 2 Pirate Cove.unity";
        private const string WORLD1_SCENE = "Assets/Project Files/Game/Scenes/Worlds/World 1 [Full Demo].unity";
        private const string WORLDS_DATABASE = "Assets/Project Files/Data/Worlds Database.asset";
        private const string PREFABS = "Assets/Project Files/Game/Prefabs/";
        private const string WORLD2_SAVE_NAME = "world_pirate_cove";
        private const float CELL = 5f;

        // . water  s sand  l land  t plateau (tall)  R land with the ramp  B water under the bridge
        // 0-9 u: cursed gate tiles opened by missions (u = plateau)   x y: optional cursed tiles (land / plateau)
        public static readonly string[] MAP =
        {
            ".............................",
            "..ss...ss....................",
            ".slll0llss...................",
            ".sllllllls...................",
            "..sssssss....................",
            "......9......................",
            "....slllls....ttttt..........",
            "...sllllllls..tttttt.........",
            "..slllll3ll4lRtuttt..........",
            "..slllllllls..ttttt..........",
            "...sllllllls..ttytt..........",
            "....sllllss..................",
            ".......2..........ssss.......",
            "......sll1s.......sllls......",
            ".....slllllls....sl5ll8s.....",
            ".....sllllllllBBlllllll......",
            "......slllllls....l7lll6.....",
            ".......ssssss.....sllls......",
            "...................sss.......",
            ".............................",
        };

        // torch cost and construction hits of every cursed gate
        private static readonly Dictionary<char, int> GATE_TORCHES = new Dictionary<char, int>
        {
            { '1', 3 }, { '2', 5 }, { '3', 6 }, { '4', 8 }, { 'u', 10 }, { '5', 10 }, { '6', 12 }, { '7', 12 }, { '8', 15 }, { '9', 15 }, { '0', 18 }, { 'x', 6 }, { 'y', 8 },
        };

        public static Vector3 CellPosition(int row, int column, float height = 0f)
        {
            return new Vector3((column - 12) * CELL, height, (10 - row) * CELL);
        }

        private static char Cell(int row, int column)
        {
            if (row < 0 || row >= MAP.Length || column < 0 || column >= MAP[row].Length) return '.';
            return MAP[row][column];
        }

        private static bool IsTall(char cell) => cell == 't' || cell == 'u' || cell == 'y';

        private static float CellHeight(char cell) => IsTall(cell) ? 4f : cell == 's' ? -0.5f : 0f;

        // ---------------------------------------------------------------------------------------------------

        private class Context
        {
            public Transform World, Buildings, Tiles, Resources, Obstacles, Missions, Characters, Special;
            public Material Pirate, Glow;
            public Dictionary<char, GroundTileComplexBehavior> Gates = new Dictionary<char, GroundTileComplexBehavior>();
            public Dictionary<string, BuildingComplexBehavior> BuildingsByName = new Dictionary<string, BuildingComplexBehavior>();
            public HashSet<Vector2Int> Reserved = new HashSet<Vector2Int>();
            public System.Random Random = new System.Random(2026);
            public List<Mission> MissionList = new List<Mission>();
            public SubworldEntrance CaveEntrance, TentEntrance;
            public SubworldBehavior CaveSubworld;
            public HelperBehavior Helper1, Helper2;
            public WorldChangeZoneBehavior ShipZone;
            public Transform BruteSpawn;
        }

        [MenuItem("Tools/World 2/4. Build Pirate Cove Scene", priority = 4)]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[World 2] Stop play mode first");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null)
                AssetDatabase.CopyAsset(WORLD1_SCENE, SCENE_PATH);

            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            Context context = new Context
            {
                World = GameObject.Find("World").transform,
                Pirate = AssetDatabase.LoadAssetAtPath<Material>(PirateArtAssets.MATERIAL_PATH),
                Glow = AssetDatabase.LoadAssetAtPath<Material>(PirateArtAssets.MATERIAL_GLOW_PATH),
            };
            context.Buildings = context.World.Find("Buildings");
            context.Tiles = context.World.Find("Environment/Ground Tiles");
            context.Resources = context.World.Find("Environment/Resources");
            context.Obstacles = context.World.Find("Environment/Obstacles");
            context.Missions = context.World.Find("Missions Holder");
            context.Characters = context.World.Find("Characters");
            context.Special = context.World.Find("Special Objects");

            ClearWorld(context);
            BuildTiles(context);
            BuildBuildings(context);
            BuildCharacters(context);
            BuildCrypt(context);
            BindToTiles();
            BuildResourcesAndProps(context);
            BindToTiles();
            BuildMissions(context);
            ConfigureWorld(context);
            RegenerateIds();

            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is ISceneSavingCallback callback) callback.OnSceneSaving();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RegisterWorld();

            Debug.Log("[World 2] Pirate Cove built: " + context.MissionList.Count + " missions");
        }

        // ---------------------------------------------------------------------------------------------------

        private static void ClearWorld(Context context)
        {
            // kept: tent building (linked to the tent sub-world), cave entrance (linked to the cave), helpers, rest zone, digging points
            foreach (Transform child in Children(context.Buildings))
            {
                if (child.GetComponentInChildren<SubworldEntrance>(true) != null && child.name.Contains("Tent")) continue;
                Object.DestroyImmediate(child.gameObject);
            }

            foreach (Transform parent in new[] { context.Tiles, context.Resources, context.Obstacles, context.Missions })
            {
                foreach (Transform child in Children(parent)) Object.DestroyImmediate(child.gameObject);
            }

            foreach (Transform child in Children(context.Characters))
            {
                if (child.GetComponent<HelperBehavior>() != null || child.name.Contains("Rest Zone")) continue;
                Object.DestroyImmediate(child.gameObject);
            }

            int diggingKept = 0;
            foreach (Transform child in Children(context.Special))
            {
                if (child.GetComponent<SubworldEntrance>() != null) continue;
                if (child.GetComponent<DiggingSpawnPoint>() != null && diggingKept++ < 4) continue;
                Object.DestroyImmediate(child.gameObject);
            }

            Transform decor = context.World.Find("Environment/Decor");
            if (decor != null) Object.DestroyImmediate(decor.gameObject);
        }

        private static List<Transform> Children(Transform parent)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in parent) children.Add(child);
            return children;
        }

        private static GameObject Spawn(string path, Transform parent, Vector3 position, float rotationY = 0f)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("[World 2] Missing prefab " + path);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0, rotationY, 0);

            return instance;
        }

        // ---------------------------------------------------------------------------------------------------

        private static void BuildTiles(Context context)
        {
            for (int row = 0; row < MAP.Length; row++)
            {
                for (int column = 0; column < MAP[row].Length; column++)
                {
                    char cell = MAP[row][column];
                    if (cell == '.' || cell == 'B') continue;

                    string prefab;
                    if (GATE_TORCHES.ContainsKey(cell))
                        prefab = IsTall(cell) ? World2BuildingsBuilder.CURSED_TALL_TILE : World2BuildingsBuilder.CURSED_TILE;
                    else if (cell == 's')
                        prefab = PREFABS + "Ground/Main Tiles/Sand Tile.prefab";
                    else if (cell == 't')
                        prefab = PREFABS + "Ground/Main Tiles/Land Tile Tall.prefab";
                    else
                        prefab = PREFABS + "Ground/Main Tiles/Land Tile.prefab";

                    GameObject tile = Spawn(prefab, context.Tiles, CellPosition(row, column, CellHeight(cell)));
                    tile.name = $"{tile.name} [{row},{column}]";

                    if (GATE_TORCHES.TryGetValue(cell, out int torches))
                    {
                        GroundTileComplexBehavior complex = tile.GetComponent<GroundTileComplexBehavior>();
                        SetCost(complex, 4 + torches / 2, (CurrencyType.Torch, torches));
                        if (cell != 'x' && cell != 'y') context.Gates[cell] = complex;
                        context.Reserved.Add(new Vector2Int(row, column));
                    }
                }
            }
        }

        private static void SetCost(Component complex, int hits, params (CurrencyType type, int amount)[] cost)
        {
            SerializedObject serialized = new SerializedObject(complex);
            SerializedProperty list = serialized.FindProperty("cost");
            list.arraySize = cost.Length;
            for (int i = 0; i < cost.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("currency").intValue = (int)cost[i].type;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = cost[i].amount;
            }
            serialized.FindProperty("constructionHitsRequired").intValue = hits;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // the purchase point canvas shows the cost
            if (complex is GroundTileComplexBehavior ground) ground.UpdatePurchaseCostInEditor();
            if (complex is BuildingComplexBehavior building) building.UpdatePurchaseCostInEditor();
        }

        private static BuildingComplexBehavior PlaceBuilding(Context context, string name, string prefab, int row, int column, float rotationY, int hits, params (CurrencyType type, int amount)[] cost)
        {
            char cell = Cell(row, column);
            GameObject instance = Spawn(prefab, context.Buildings, CellPosition(row, column, CellHeight(cell)), rotationY);
            instance.name = name;

            BuildingComplexBehavior complex = instance.GetComponent<BuildingComplexBehavior>();
            if (cost.Length > 0) SetCost(complex, hits, cost);

            context.BuildingsByName[name] = complex;
            context.Reserved.Add(new Vector2Int(row, column));

            return complex;
        }

        private static void BuildBuildings(Context context)
        {
            // A: wreck beach
            PlaceBuilding(context, "Torch Table", World2BuildingsBuilder.TORCH_TABLE, 14, 10, 0, 0, (CurrencyType.Wood, 8), (CurrencyType.Stone, 4));
            BuildingComplexBehavior campfire = PlaceBuilding(context, "Campfire", PREFABS + "Buildings/Cooking Place.prefab", 13, 9, 0, 0, (CurrencyType.Wood, 10), (CurrencyType.Stone, 6));
            SetFoodRecipe(campfire, CurrencyType.Jam, (CurrencyType.Berries, 8));

            // B: smuggler's woods
            PlaceBuilding(context, "Storage", PREFABS + "Buildings/Storage Building Complex.prefab", 9, 5, 0, 20, (CurrencyType.Wood, 15), (CurrencyType.Stone, 15));

            // ramp from the foot of the fort onto the plateau (the ramp climbs along its local +Z)
            PlaceBuilding(context, "Ramp", PREFABS + "Buildings/Ramp Building.prefab", 8, 13, 90, 30, (CurrencyType.Wood, 40), (CurrencyType.Stone, 30));

            // C: old fort
            PlaceBuilding(context, "Blacksmith Forge", World2BuildingsBuilder.FORGE, 8, 15, 0, 0, (CurrencyType.Wood, 30), (CurrencyType.Stone, 40), (CurrencyType.IronOre, 10));
            PlaceBuilding(context, "Planks Table", PREFABS + "Buildings/Planks Craft Table.prefab", 10, 17, 0, 0, (CurrencyType.Wood, 40), (CurrencyType.IronBar, 2));

            // the tent (its entrance leads to the tent sub-world with the global upgrades)
            Transform tent = null;
            foreach (Transform child in context.Buildings)
                if (child.name.Contains("Tent")) tent = child;
            if (tent != null)
            {
                tent.position = CellPosition(6, 17, 4f);
                tent.rotation = Quaternion.identity;
                tent.name = "Tent Building";
                BuildingComplexBehavior tentComplex = tent.GetComponent<BuildingComplexBehavior>();
                SetCost(tentComplex, 35, (CurrencyType.Wood, 30), (CurrencyType.Stone, 20), (CurrencyType.Fiber, 15));
                context.BuildingsByName["Tent"] = tentComplex;
                context.TentEntrance = tent.GetComponentInChildren<SubworldEntrance>(true);
                context.Reserved.Add(new Vector2Int(6, 17));
            }

            // bridge from the beach to the graveyard island (the bridge extends along its local +Z)
            BuildingComplexBehavior bridge = PlaceBuilding(context, "Bridge", PREFABS + "Buildings/Bridge Building.prefab", 15, 13, 90, 40, (CurrencyType.Wood, 60), (CurrencyType.IronBar, 4));
            // the template bridge is a little short for a 2 cell gap: shift it so both ends rest on the tiles
            bridge.transform.position += new Vector3(0.5f, 0, 0);

            // D: graveyard
            PlaceBuilding(context, "Ropes Table", PREFABS + "Buildings/Ropes Craft Table.prefab", 13, 20, 0, 0, (CurrencyType.Wood, 40), (CurrencyType.Fiber, 20));

            // F: harbor
            PlaceBuilding(context, "Sail Loom", World2BuildingsBuilder.SAIL_LOOM, 3, 3, 0, 0, (CurrencyType.Wood, 40), (CurrencyType.Rope, 10));
            BuildingComplexBehavior ship = PlaceBuilding(context, "Pirate Ship", World2BuildingsBuilder.PIRATE_SHIP, 2, 5, 90, 60,
                (CurrencyType.Planks, 40), (CurrencyType.Rope, 25), (CurrencyType.Sailcloth, 15), (CurrencyType.IronBar, 10));
            context.ShipZone = ship.GetComponentInChildren<WorldChangeZoneBehavior>(true);
        }

        private static void SetFoodRecipe(BuildingComplexBehavior building, CurrencyType result, params (CurrencyType type, int amount)[] components)
        {
            // the cooking place takes its recipe from the recipe upgrade: keep a single stage (no recipe swap in World 2)
            ConverterRecipeUpgrade upgrade = building.GetComponentInChildren<ConverterRecipeUpgrade>(true);
            if (upgrade == null) return;

            SerializedObject serialized = new SerializedObject(upgrade);
            SerializedProperty stages = serialized.FindProperty("stages");
            stages.arraySize = 1;
            SerializedProperty value = stages.GetArrayElementAtIndex(0).FindPropertyRelative("value");
            value.FindPropertyRelative("resultResourceType").intValue = (int)result;
            SerializedProperty list = value.FindPropertyRelative("components");
            list.arraySize = components.Length;
            for (int i = 0; i < components.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("resourceType").intValue = (int)components[i].type;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = components[i].amount;
            }
            stages.GetArrayElementAtIndex(0).FindPropertyRelative("cost").FindPropertyRelative("amount").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------------------------------------------

        private static void BuildCharacters(Context context)
        {
            List<HelperBehavior> helpers = new List<HelperBehavior>();
            foreach (Transform child in context.Characters)
            {
                HelperBehavior helper = child.GetComponent<HelperBehavior>();
                if (helper != null) helpers.Add(helper);
            }

            if (helpers.Count >= 2)
            {
                context.Helper1 = helpers[0];
                context.Helper2 = helpers[1];
                SetupHelper(context.Helper1, CellPosition(8, 8), context.Gates['3']);
                SetupHelper(context.Helper2, CellPosition(16, 19), context.Gates['7']);
            }

            Transform rest = context.Characters.Find("Helpers Rest Zone");
            if (rest != null) rest.position = CellPosition(15, 8) + new Vector3(2, 0, 2);

            // enemies: pirates in the woods, a brute on the fort, the restless dead on the graveyard
            AddEnemy(context, EnemyType.PirateSkeleton, 7, 5, 10f);
            AddEnemy(context, EnemyType.PirateSkeleton, 10, 6, 10f);
            AddEnemy(context, EnemyType.PirateSkeleton, 9, 9, 12f);
            AddEnemy(context, EnemyType.PirateSkeleton, 6, 7, 12f);
            context.BruteSpawn = AddEnemy(context, EnemyType.SkeletonBrute, 9, 16, 25f).transform;
            AddEnemy(context, EnemyType.PirateSkeleton, 7, 18, 15f);
            AddEnemy(context, EnemyType.PirateSkeleton, 13, 21, 10f);
            AddEnemy(context, EnemyType.PirateSkeleton, 15, 18, 10f);
            AddEnemy(context, EnemyType.PirateSkeleton, 15, 21, 12f);
            AddEnemy(context, EnemyType.SkeletonBrute, 17, 20, 40f);
        }

        private static void SetupHelper(HelperBehavior helper, Vector3 position, GroundTileComplexBehavior gate)
        {
            helper.transform.position = position;
            SerializedObject serialized = new SerializedObject(helper);
            serialized.FindProperty("specialOpeningLogic").boolValue = true;
            SerializedProperty tiles = serialized.FindProperty("linkedTiles");
            tiles.arraySize = 1;
            tiles.GetArrayElementAtIndex(0).objectReferenceValue = gate;
            serialized.FindProperty("linkedBuildings").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemySpawnPoint AddEnemy(Context context, EnemyType type, int row, int column, float respawn)
        {
            GameObject spawn = Spawn(PREFABS + "Enemies/Enemy Spawn Point.prefab", context.Characters, CellPosition(row, column, CellHeight(Cell(row, column))) + new Vector3(1.2f, 0, 0.8f), 180f);
            spawn.name = type + " Spawn";
            EnemySpawnPoint point = spawn.GetComponent<EnemySpawnPoint>();
            SerializedObject serialized = new SerializedObject(point);
            serialized.FindProperty("enemyType").intValue = (int)type;
            serialized.FindProperty("shouldRespawnAfterDeath").boolValue = true;
            serialized.FindProperty("respawnDuration").floatValue = respawn;
            serialized.FindProperty("playerDetectionRadius").floatValue = type == EnemyType.SkeletonBrute ? 12f : 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return point;
        }

        // ---------------------------------------------------------------------------------------------------

        private static void BuildCrypt(Context context)
        {
            // cave entrance -> on the graveyard crypt gate ("8"), appears when the gate is opened
            foreach (Transform child in context.Special)
            {
                SubworldEntrance entrance = child.GetComponent<SubworldEntrance>();
                if (entrance == null) continue;

                context.CaveEntrance = entrance;
                child.position = CellPosition(14, 22) + new Vector3(0, 0, 0.5f);
                child.rotation = Quaternion.Euler(0, 180, 0);
                child.name = "Crypt Entrance";
            }

            foreach (SubworldBehavior subworld in Object.FindObjectsByType<SubworldBehavior>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!subworld.name.Contains("Cave")) continue;
                context.CaveSubworld = subworld;
                subworld.name = "Cave Subworld";

                // the template cave skeletons and chest make way for the captain
                Transform enemies = subworld.transform.Find("Enemies");
                if (enemies != null) Object.DestroyImmediate(enemies.gameObject);
                foreach (ChestBehavior chest in subworld.GetComponentsInChildren<ChestBehavior>(true)) Object.DestroyImmediate(chest.gameObject);

                Transform old = subworld.transform.Find("Captain Arena");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                Transform spawnPoint = subworld.transform.Find("Spawn Point");

                // the arena is the largest platform of the cave (the deepest one, reached over the ramp)
                Bounds platform = new Bounds(spawnPoint.position, Vector3.zero);
                float bestArea = 0f;
                foreach (MeshRenderer renderer in subworld.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.name.StartsWith("Cave_platform")) continue;
                    float area = renderer.bounds.size.x * renderer.bounds.size.z;
                    if (area > bestArea) { bestArea = area; platform = renderer.bounds; }
                }

                Vector3 center = new Vector3(platform.center.x, spawnPoint.position.y, platform.center.z);
                // the player comes from the spawn side, the captain's hoard lies behind him
                Vector3 direction = (center - spawnPoint.position).SetY(0).normalized;
                float edge = Mathf.Min(platform.extents.x, platform.extents.z);

                GameObject arena = new GameObject("Captain Arena");
                arena.transform.SetParent(subworld.transform, false);
                arena.transform.position = center;

                Transform bossSpawn = new GameObject("Boss Spawn").transform;
                bossSpawn.SetParent(arena.transform, false);
                bossSpawn.position = center;
                bossSpawn.rotation = Quaternion.LookRotation(-direction);

                BossArena bossArena = arena.AddComponent<BossArena>();
                SerializedObject serialized = new SerializedObject(bossArena);
                serialized.FindProperty("bossSpawnPoint").objectReferenceValue = bossSpawn;
                serialized.FindProperty("triggerRadius").floatValue = edge + 1f;
                serialized.FindProperty("titleFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50 Outline.asset");
                serialized.FindProperty("barSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project Files/Game/Images/Base/UI Flat/pb_rect_rounded.png");
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // the captain's hoard and flags behind him, kept inside the platform
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                float back = edge * 0.62f;
                AddProp(context, "Treasure Pile", arena.transform, center + direction * back, 0f, 1.2f);
                AddProp(context, "Skull Flag", arena.transform, center + direction * back * 0.85f + side * edge * 0.5f, 200f);
                AddProp(context, "Skull Flag", arena.transform, center + direction * back * 0.85f - side * edge * 0.5f, 160f);
                AddProp(context, "Cannon", arena.transform, center + direction * back * 0.3f + side * edge * 0.62f, Quaternion.LookRotation(-direction).eulerAngles.y + 25f);
                AddProp(context, "Barrel", arena.transform, center + direction * back * 0.3f - side * edge * 0.62f, 0f);
            }
        }

        // ---------------------------------------------------------------------------------------------------

        private static GameObject AddProp(Context context, string name, Transform parent, Vector3 position, float rotationY, float scale = 1f)
        {
            GameObject prop = Spawn(World2BuildingsBuilder.PROPS_FOLDER + "/" + name + ".prefab", parent, position, rotationY);
            if (prop != null) prop.transform.localScale = Vector3.one * scale;

            return prop;
        }

        private enum Region { Beach, Woods, Fort, Graveyard, Harbor }

        private static Region RegionOf(int row, int column)
        {
            if (row <= 4) return Region.Harbor;
            if (column >= 16) return Region.Graveyard;
            if (column >= 13 && row <= 11) return Region.Fort;
            if (row >= 12) return Region.Beach;
            return Region.Woods;
        }

        // footprints already taken on the ground (x, z, radius)
        private class Footprints
        {
            private readonly List<Vector3> circles = new List<Vector3>();
            private readonly List<Bounds> boxes = new List<Bounds>();

            public void AddCircle(Vector3 position, float radius) => circles.Add(new Vector3(position.x, radius, position.z));

            public void AddBox(Bounds bounds, float margin)
            {
                bounds.Expand(new Vector3(margin * 2f, 0, margin * 2f));
                boxes.Add(bounds);
            }

            public bool IsFree(Vector3 position, float radius)
            {
                foreach (Vector3 circle in circles)
                {
                    float dx = circle.x - position.x, dz = circle.z - position.z;
                    float need = circle.y + radius;
                    if (dx * dx + dz * dz < need * need) return false;
                }

                foreach (Bounds box in boxes)
                {
                    float dx = Mathf.Max(box.min.x - position.x, 0, position.x - box.max.x);
                    float dz = Mathf.Max(box.min.z - position.z, 0, position.z - box.max.z);
                    if (dx * dx + dz * dz < radius * radius) return false;
                }

                return true;
            }
        }

        private static Bounds VisualBounds(GameObject root)
        {
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
            bool first = true;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer.GetComponentInParent<Canvas>(true) != null) continue;
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        // ground radius of every resource / prop so nothing overlaps
        private static float FootprintRadius(string item)
        {
            if (item.Contains("Coconut")) return 1.25f;
            if (item.Contains("Tree Resource")) return 1.15f;
            if (item.Contains("Stone") || item.Contains("Iron")) return 1.0f;
            if (item.Contains("Berry")) return 0.95f;
            if (item.Contains("Pumpkin")) return 0.7f;
            if (item.Contains("Grass")) return 0.75f;
            if (item.Contains("Dead Tree")) return 0.8f;
            if (item.Contains("Tombstone")) return 0.6f;
            return 0.8f;
        }

        private static GameObject AddFixedProp(Context context, Footprints footprints, string name, Transform parent, Vector3 position, float rotationY, float scale = 1f)
        {
            GameObject prop = AddProp(context, name, parent, position, rotationY, scale);
            if (prop != null)
            {
                Bounds bounds = VisualBounds(prop);
                footprints.AddCircle(position, Mathf.Max(0.5f, Mathf.Min(bounds.extents.x, bounds.extents.z) + 0.3f));
            }

            return prop;
        }

        private static void BuildResourcesAndProps(Context context)
        {
            Transform decor = new GameObject("Decor").transform;
            decor.SetParent(context.World.Find("Environment"), false);

            Footprints footprints = new Footprints();

            // digging points on the graveyard
            int dig = 0;
            Vector2Int[] digCells = { new Vector2Int(13, 19), new Vector2Int(15, 20), new Vector2Int(16, 21), new Vector2Int(14, 20) };
            foreach (DiggingSpawnPoint point in context.Special.GetComponentsInChildren<DiggingSpawnPoint>(true))
            {
                if (dig >= digCells.Length) break;
                point.transform.position = CellPosition(digCells[dig].x, digCells[dig].y) + new Vector3(-1.3f, 0, -1.1f);
                dig++;
            }

            // buildings (incl. their construction zone and purchase area) with a walking margin around them
            foreach (Transform building in context.Buildings)
                footprints.AddBox(VisualBounds(building.gameObject), building.name == "Bridge" || building.name == "Ramp" ? 1.4f : 1.0f);

            // enemies, helpers, digging points and sub-world entrances
            foreach (Transform character in context.Characters)
                footprints.AddCircle(character.position, character.GetComponent<HelperBehavior>() != null ? 1.2f : 1.4f);
            foreach (Transform special in context.Special)
                footprints.AddCircle(special.position, special.GetComponent<SubworldEntrance>() != null ? 2.5f : 1.0f);

            // cells kept clear: start area, ramp and bridge ends, the cells in front of every gate
            HashSet<Vector2Int> clear = new HashSet<Vector2Int>();
            for (int r = 14; r <= 16; r++)
                for (int c = 7; c <= 9; c++) clear.Add(new Vector2Int(r, c));
            clear.Add(new Vector2Int(8, 12));
            clear.Add(new Vector2Int(8, 14));
            clear.Add(new Vector2Int(15, 12));
            clear.Add(new Vector2Int(15, 16));
            for (int row = 0; row < MAP.Length; row++)
            {
                for (int column = 0; column < MAP[row].Length; column++)
                {
                    char cell = MAP[row][column];
                    if (!GATE_TORCHES.ContainsKey(cell) || cell == 'x' || cell == 'y') continue;
                    foreach (Vector2Int step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        clear.Add(new Vector2Int(row + step.x, column + step.y));
                }
            }

            // wreck of the raft on the beach where the player wakes up
            Vector3 wreck = CellPosition(16, 9, 0f) + new Vector3(0.5f, 0, -0.5f);
            GameObject raftModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Models/Buildings/raft.fbx");
            if (raftModel != null)
            {
                GameObject raft = (GameObject)PrefabUtility.InstantiatePrefab(raftModel, decor);
                raft.name = "Raft Wreck";
                raft.transform.position = wreck + new Vector3(0, -0.15f, 0);
                raft.transform.rotation = Quaternion.Euler(8, 35, -6);
                raft.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
                foreach (Renderer renderer in raft.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Project Files/Game/Materials/Buildings.mat");
                footprints.AddCircle(wreck, 1.8f);
            }
            AddFixedProp(context, footprints, "Barrel", decor, wreck + new Vector3(2.2f, 0, 0.8f), 30f);
            AddFixedProp(context, footprints, "Crate", decor, wreck + new Vector3(-2.1f, 0, 0.5f), 15f);
            AddFixedProp(context, footprints, "Crate", decor, CellPosition(17, 10, -0.5f) + new Vector3(0.4f, 0, 0.6f), 70f, 0.8f);

            // landmarks
            AddFixedProp(context, footprints, "Skull Flag", decor, CellPosition(7, 10) + new Vector3(1.5f, 0, -1f), 220f);
            AddFixedProp(context, footprints, "Lantern Post", decor, CellPosition(8, 12) + new Vector3(0, 0, 2.0f), 180f);
            AddFixedProp(context, footprints, "Lantern Post", decor, CellPosition(15, 12) + new Vector3(-1.5f, 0, -2.0f), 90f);
            AddFixedProp(context, footprints, "Cannon", decor, CellPosition(7, 14, 4f) + new Vector3(-1.2f, 0, 1.2f), 300f);
            AddFixedProp(context, footprints, "Cannon", decor, CellPosition(9, 18, 4f) + new Vector3(1.4f, 0, 0f), 90f);
            AddFixedProp(context, footprints, "Skull Flag", decor, CellPosition(6, 15, 4f) + new Vector3(0, 0, 1.5f), 180f);
            AddFixedProp(context, footprints, "Barrel", decor, CellPosition(3, 6) + new Vector3(1.5f, 0, -1.4f), 0f);
            AddFixedProp(context, footprints, "Crate", decor, CellPosition(3, 7) + new Vector3(0.8f, 0, -1.6f), 20f);
            AddFixedProp(context, footprints, "Lantern Post", decor, CellPosition(2, 4) + new Vector3(-1.5f, 0, 1.6f), 90f);

            // gate pockets: decor shown when the gate opens (children of the tile's opened visuals)
            AddPocketDecor(context, '5', "Tombstone", 3);
            AddPocketDecor(context, '6', "Treasure Pile", 1);
            AddPocketDecor(context, '6', "Skull Flag", 1);

            // resources and scattered decor: up to 4 slots per cell (2x2), one object per slot, no overlaps
            Vector2[] slots = { new Vector2(-1.25f, -1.25f), new Vector2(1.25f, -1.25f), new Vector2(-1.25f, 1.25f), new Vector2(1.25f, 1.25f) };
            for (int row = 0; row < MAP.Length; row++)
            {
                for (int column = 0; column < MAP[row].Length; column++)
                {
                    char cell = MAP[row][column];
                    if (cell != 'l' && cell != 's' && cell != 't') continue;

                    Vector2Int key = new Vector2Int(row, column);
                    if (context.Reserved.Contains(key) || clear.Contains(key)) continue;

                    Region region = RegionOf(row, column);
                    Vector3 center = CellPosition(row, column, CellHeight(cell));

                    List<int> freeSlots = new List<int> { 0, 1, 2, 3 };
                    foreach ((string item, float chance) in Spawnables(region, cell, column))
                    {
                        if (context.Random.NextDouble() > chance || freeSlots.Count == 0) continue;

                        float radius = FootprintRadius(item);
                        for (int attempt = 0; attempt < freeSlots.Count; attempt++)
                        {
                            int slotIndex = context.Random.Next(freeSlots.Count);
                            Vector2 slot = slots[freeSlots[slotIndex]];
                            Vector3 position = center + new Vector3(slot.x + ((float)context.Random.NextDouble() - 0.5f) * 0.5f, 0, slot.y + ((float)context.Random.NextDouble() - 0.5f) * 0.5f);
                            if (!footprints.IsFree(position, radius)) continue;

                            float rotation = (float)context.Random.NextDouble() * 360f;
                            if (item.StartsWith("prop:"))
                                AddProp(context, item.Substring(5), decor, position, rotation, 0.9f + (float)context.Random.NextDouble() * 0.2f);
                            else
                                Spawn(item, context.Resources, position, rotation);

                            footprints.AddCircle(position, radius);
                            freeSlots.RemoveAt(slotIndex);
                            break;
                        }
                    }
                }
            }

            // fishing spots along the shores
            foreach (Vector2Int water in new[] { new Vector2Int(18, 10), new Vector2Int(17, 5), new Vector2Int(0, 4), new Vector2Int(18, 22), new Vector2Int(12, 3) })
                Spawn(PREFABS + "Drop/Fish Resource Source.prefab", context.Resources, CellPosition(water.x, water.y, 0f), 0f);

        }

        private static IEnumerable<(string, float)> Spawnables(Region region, char cell, int column)
        {
            string tree = PREFABS + "Drop/Tree Resource Source.prefab";
            string coconut = PREFABS + "Drop/Coconut Resource Source.prefab";
            string stone = PREFABS + "Drop/Stone Resource Source.prefab";
            string berry = PREFABS + "Drop/Berry Resource Source.prefab";
            string grass = PREFABS + "Drop/Grass Resource Source.prefab";
            string pumpkin = PREFABS + "Drop/Pumpkin Resource Source.prefab";
            string iron = World2ResourcesBuilder.IRON_ORE_SOURCE;

            switch (region)
            {
                case Region.Beach:
                    if (cell == 's') return new[] { (tree, 0.45f), (coconut, 0.3f) };
                    return new[] { (grass, 0.55f), (stone, 0.45f), (berry, 0.35f), (tree, 0.3f) };
                case Region.Woods:
                    if (cell == 's') return new[] { (coconut, 0.35f), (tree, 0.3f) };
                    return new[] { (tree, 0.75f), (grass, 0.5f), (column <= 5 ? iron : stone, 0.35f), (berry, 0.15f) };
                case Region.Fort:
                    return new[] { (stone, 0.45f), (iron, 0.4f), (pumpkin, 0.2f), ("prop:Dead Tree", 0.12f) };
                case Region.Graveyard:
                    if (cell == 's') return new[] { ("prop:Dead Tree", 0.2f) };
                    return new[] { (grass, 0.45f), (stone, 0.25f), ("prop:Tombstone", 0.35f), ("prop:Dead Tree", 0.2f), (berry, 0.15f) };
                default:
                    if (cell == 's') return new[] { (tree, 0.4f), (coconut, 0.3f) };
                    return new[] { (grass, 0.5f), (tree, 0.45f), (stone, 0.25f) };
            }
        }

        private static void AddPocketDecor(Context context, char gate, string prop, int count)
        {
            GroundTileComplexBehavior tile = context.Gates[gate];
            GroundTileBehavior ground = tile.GetComponentInChildren<GroundTileBehavior>(true);
            GameObject opened = ground.OpenedVisuals;
            Transform parent = opened != null ? opened.transform : ground.transform;

            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.1f;
                Vector3 position = tile.transform.position + new Vector3(Mathf.Cos(angle) * 1.4f, 0, Mathf.Sin(angle) * 1.4f);
                GameObject decor = AddProp(context, prop, parent, position, 180f + i * 25f);
                if (decor != null)
                {
                    // tombstone model has no obstacle of its own
                    if (decor.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null) decor.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------

        // links every IGroundOpenable to the tile under it (the template does this in edit mode updates)
        private static void BindToTiles()
        {
            foreach (PlacementHelper helper in Object.FindObjectsByType<PlacementHelper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                helper.FindChunk();
                EditorUtility.SetDirty(helper);
            }
        }

        // ---------------------------------------------------------------------------------------------------

        private static T AddMission<T>(Context context, string name, string title) where T : Mission
        {
            GameObject missionObject = new GameObject($"M #{context.MissionList.Count + 1:00} - {name}");
            missionObject.transform.SetParent(context.Missions, false);
            T mission = missionObject.AddComponent<T>();

            SerializedObject serialized = new SerializedObject(mission);
            serialized.FindProperty("title").stringValue = title;
            serialized.FindProperty("showPositionPointer").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            context.MissionList.Add(mission);

            return mission;
        }

        private static void Reward(Mission mission, CurrencyType type, int amount)
        {
            SerializedObject serialized = new SerializedObject(mission);
            serialized.FindProperty("rewardType").intValue = (int)MissionRewardType.Resources;
            serialized.FindProperty("resourceReward").FindPropertyRelative("currencyType").intValue = (int)type;
            serialized.FindProperty("resourceReward").FindPropertyRelative("amount").intValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Target(Mission mission, Vector3 position)
        {
            Transform pointer = new GameObject("Pointer").transform;
            pointer.SetParent(mission.transform, false);
            pointer.position = position;

            SerializedObject serialized = new SerializedObject(mission);
            serialized.FindProperty("useCustomTargetPosition").boolValue = true;
            serialized.FindProperty("customTargetPosition").objectReferenceValue = pointer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InSubworld(Context context, Mission mission)
        {
            SerializedObject serialized = new SerializedObject(mission);
            serialized.FindProperty("isTargetInSubworld").boolValue = true;
            serialized.FindProperty("subworldEntrance").objectReferenceValue = context.CaveEntrance;
            serialized.FindProperty("subworldBehavior").objectReferenceValue = context.CaveSubworld;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string property, object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            switch (value)
            {
                case int i: field.intValue = i; break;
                case bool b: field.boolValue = b; break;
                case Object o: field.objectReferenceValue = o; break;
                case System.Enum e: field.intValue = System.Convert.ToInt32(e); break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Collect(Context context, string name, string title, CurrencyType type, int amount, CurrencyType rewardType, int rewardAmount, Vector3? target = null)
        {
            CollectMission mission = AddMission<CollectMission>(context, name, title);
            Set(mission, "currencyType", type);
            Set(mission, "amount", amount);
            Reward(mission, rewardType, rewardAmount);
            if (target.HasValue) Target(mission, target.Value);
        }

        private static void Build(Context context, string name, string title, string building, CurrencyType rewardType, int rewardAmount)
        {
            BuildingMission mission = AddMission<BuildingMission>(context, name, title);
            Set(mission, "buildingComplex", context.BuildingsByName[building]);
            Reward(mission, rewardType, rewardAmount);
        }

        private static void Unlock(Context context, string name, string title, char gate, CurrencyType rewardType, int rewardAmount)
        {
            GroundUnlockMission mission = AddMission<GroundUnlockMission>(context, name, title);
            Set(mission, "groundTile", context.Gates[gate]);
            Reward(mission, rewardType, rewardAmount);
        }

        private static void Kill(Context context, string name, string title, EnemyType? type, int amount, CurrencyType rewardType, int rewardAmount, Vector3 target)
        {
            KillEnemiesMission mission = AddMission<KillEnemiesMission>(context, name, title);
            Set(mission, "anyEnemyType", !type.HasValue);
            if (type.HasValue) Set(mission, "enemyType", type.Value);
            Set(mission, "amount", amount);
            Reward(mission, rewardType, rewardAmount);
            Target(mission, target);
        }

        private static void BuildMissions(Context context)
        {
            Vector3 beach = CellPosition(14, 9);
            Vector3 woods = CellPosition(9, 6);

            // Act 1: wreck beach
            Collect(context, "wood", "Gather driftwood", CurrencyType.Wood, 10, CurrencyType.Coins, 10, CellPosition(16, 11));
            Collect(context, "stone", "Collect stone", CurrencyType.Stone, 8, CurrencyType.Stone, 6, CellPosition(14, 7));
            Build(context, "torch table", "Build a torch table", "Torch Table", CurrencyType.Coins, 15);
            Collect(context, "torches", "Craft torches", CurrencyType.Torch, 3, CurrencyType.Torch, 2, context.BuildingsByName["Torch Table"].transform.position);
            Unlock(context, "first thorns", "Burn the cursed thorns", '1', CurrencyType.Coins, 15);
            Build(context, "campfire", "Build a campfire", "Campfire", CurrencyType.Coins, 20);
            Collect(context, "berries", "Pick berries", CurrencyType.Berries, 8, CurrencyType.Berries, 8, beach);
            Collect(context, "jam", "Cook some jam", CurrencyType.Jam, 1, CurrencyType.Coins, 20, context.BuildingsByName["Campfire"].transform.position);

            // Act 2: smuggler's woods
            Unlock(context, "woods", "Clear the way inland", '2', CurrencyType.Coins, 25);
            Kill(context, "pirates", "Fight the skeleton pirates", EnemyType.PirateSkeleton, 3, CurrencyType.Coins, 40, woods);
            HelperMission helper1 = AddMission<HelperMission>(context, "castaway 1", "Save a castaway");
            Set(helper1, "helperBehavior", context.Helper1);
            Reward(helper1, CurrencyType.Coins, 30);
            Build(context, "storage", "Build a storage", "Storage", CurrencyType.Coins, 30);
            Collect(context, "iron ore", "Mine iron ore", CurrencyType.IronOre, 10, CurrencyType.IronOre, 5, CellPosition(9, 3));
            Unlock(context, "fort path", "Clear the path to the fort", '4', CurrencyType.Coins, 30);

            // Act 3: old fort
            Build(context, "ramp", "Build the ramp", "Ramp", CurrencyType.Coins, 40);
            Unlock(context, "forge site", "Explore the old fort", 'u', CurrencyType.Coins, 40);
            Build(context, "forge", "Restore the blacksmith forge", "Blacksmith Forge", CurrencyType.Coins, 50);
            Collect(context, "iron bars", "Smelt iron bars", CurrencyType.IronBar, 4, CurrencyType.IronBar, 2, context.BuildingsByName["Blacksmith Forge"].transform.position);
            UpgradeMission sword = AddMission<UpgradeMission>(context, "sword", "Upgrade your sword");
            sword.upgradeType = UpgradeType.Global;
            Set(sword, "globalUpgradeType", GlobalUpgradeType.Sword);
            Set(sword, "upgradeLevel", 2);
            Set(sword, "highlightInTheList", true);
            Reward(sword, CurrencyType.Coins, 60);
            Target(sword, context.BuildingsByName["Blacksmith Forge"].transform.position);
            Kill(context, "brute", "Defeat the Skeleton Brute", EnemyType.SkeletonBrute, 1, CurrencyType.MapPiece, 1, context.BruteSpawn.position);
            Build(context, "tent", "Set up camp", "Tent", CurrencyType.Coins, 50);
            Build(context, "planks table", "Build a planks table", "Planks Table", CurrencyType.Coins, 40);

            // Act 4: graveyard
            Build(context, "bridge", "Build the bridge", "Bridge", CurrencyType.Coins, 60);
            Unlock(context, "old grave", "Open the old grave", '5', CurrencyType.MapPiece, 1);
            Kill(context, "restless dead", "Calm the restless dead", null, 5, CurrencyType.Coins, 80, CellPosition(15, 20));
            HelperMission helper2 = AddMission<HelperMission>(context, "castaway 2", "Save another castaway");
            Set(helper2, "helperBehavior", context.Helper2);
            Reward(helper2, CurrencyType.Coins, 50);
            Build(context, "ropes table", "Build a rope table", "Ropes Table", CurrencyType.Coins, 40);
            Unlock(context, "marked grave", "Dig up the marked grave", '6', CurrencyType.MapPiece, 1);

            // Act 5: the captain's crypt
            Unlock(context, "crypt", "Find the captain's crypt", '8', CurrencyType.Coins, 60);
            SubworldEnterMission enter = AddMission<SubworldEnterMission>(context, "enter crypt", "Enter the crypt");
            Set(enter, "subworldEntranceRef", context.CaveEntrance);
            // the template mission also reads the base class entrance (pointer and enter event)
            Set(enter, "subworldEntrance", context.CaveEntrance);
            Reward(enter, CurrencyType.Coins, 10);
            KillEnemiesMission boss = AddMission<KillEnemiesMission>(context, "ghost captain", "Defeat the Ghost Captain");
            Set(boss, "anyEnemyType", false);
            Set(boss, "enemyType", EnemyType.GhostCaptain);
            Set(boss, "amount", 1);
            Reward(boss, CurrencyType.MapPiece, 1);
            InSubworld(context, boss);
            OwnCurrencyMission map = AddMission<OwnCurrencyMission>(context, "treasure map", "Assemble the treasure map");
            Set(map, "currencyType", CurrencyType.MapPiece);
            Set(map, "amount", 4);
            Reward(map, CurrencyType.Coins, 300);

            // Act 6: harbor and escape
            Unlock(context, "harbor", "Head to the harbor", '9', CurrencyType.Coins, 60);
            Build(context, "sail loom", "Build a sail loom", "Sail Loom", CurrencyType.Coins, 50);
            Collect(context, "sailcloth", "Weave sailcloth", CurrencyType.Sailcloth, 15, CurrencyType.Coins, 50, context.BuildingsByName["Sail Loom"].transform.position);
            Unlock(context, "dock", "Clear the dock", '0', CurrencyType.Coins, 60);
            Build(context, "ship", "Repair the pirate ship", "Pirate Ship", CurrencyType.Coins, 150);
            WorldChangeMission sail = AddMission<WorldChangeMission>(context, "set sail", "Set sail!");
            Set(sail, "worldChangeZone", context.ShipZone);
            Reward(sail, CurrencyType.Coins, 0);
        }

        // ---------------------------------------------------------------------------------------------------

        private static void ConfigureWorld(Context context)
        {
            WorldBehavior world = context.World.GetComponent<WorldBehavior>();
            SerializedObject serialized = new SerializedObject(world);
            serialized.FindProperty("worldSaveName").stringValue = WORLD2_SAVE_NAME;
            serialized.FindProperty("resetCurrencyOnFirstTimeLoad").boolValue = true;
            serialized.FindProperty("keepCoinsOnReset").boolValue = true;
            serialized.FindProperty("defaultCurrency").arraySize = 0;
            serialized.FindProperty("restoreHungerPointsOnFirstTimeLoad").boolValue = true;
            serialized.FindProperty("environmentPresetType").intValue = (int)EnvironmentPresetType.World2;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Transform spawn = context.World.Find("Spawn Point");
            spawn.position = CellPosition(15, 8) + new Vector3(0.5f, 0, -0.5f);
            spawn.rotation = Quaternion.Euler(0, 20, 0);

            // the ship sails into the ending (no world after this one)
            if (context.ShipZone != null) Set(context.ShipZone, "worldIndex", -1);

            // story: storm arrival cinematic and the ending
            World2Story story = context.World.GetComponent<World2Story>() ?? context.World.gameObject.AddComponent<World2Story>();
            Transform endingPoint = context.World.Find("Ending Respawn Point") ?? new GameObject("Ending Respawn Point").transform;
            endingPoint.SetParent(context.World, false);
            endingPoint.position = CellPosition(3, 5) + new Vector3(0, 0, -1.5f);
            endingPoint.rotation = Quaternion.Euler(0, 180, 0);

            SerializedObject storySerialized = new SerializedObject(story);
            storySerialized.FindProperty("stormWeather").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnvironmentWeatherPreset>("Assets/Project Files/Data/Environment/Weather/Storm.asset");
            storySerialized.FindProperty("arrivalWeather").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnvironmentWeatherPreset>("Assets/Project Files/Data/Environment/Weather/Clouds.asset");
            storySerialized.FindProperty("raftModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Models/Buildings/raft.fbx");
            storySerialized.FindProperty("raftMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Project Files/Game/Materials/Buildings.mat");
            storySerialized.FindProperty("splashParticle").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "Particles/Characters/Water Splash.prefab");
            storySerialized.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50 Outline.asset");
            storySerialized.FindProperty("buttonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project Files/Game/Images/Base/UI Flat/btn_green_flat.png");
            storySerialized.FindProperty("endingRespawnPoint").objectReferenceValue = endingPoint;
            storySerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // every [UniqueID] field gets an id derived from the object's place in the hierarchy: never equal to World 1,
        // and stable between rebuilds so existing World 2 saves keep working
        private static void RegenerateIds()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;

                SerializedObject serialized = null;
                for (System.Type type = behaviour.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
                {
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    {
                        if (field.FieldType != typeof(string) || field.GetCustomAttribute<UniqueIDAttribute>() == null) continue;
                        if (field.Name == "worldSaveName") continue;

                        serialized ??= new SerializedObject(behaviour);
                        SerializedProperty property = serialized.FindProperty(field.Name);
                        if (property != null) property.stringValue = StableId(behaviour, field.Name);
                    }
                }

                serialized?.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static string StableId(Component component, string field)
        {
            System.Text.StringBuilder path = new System.Text.StringBuilder(WORLD2_SAVE_NAME);
            for (Transform current = component.transform; current != null; current = current.parent)
                path.Insert(0, "/" + current.name + "#" + current.GetSiblingIndex());
            path.Append("|" + component.GetType().Name + "|" + field);

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                return new System.Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(path.ToString()))).ToString();
        }

        // World 2 in the worlds database now points to this scene (same world id, so existing saves continue here)
        private static void RegisterWorld()
        {
            Object database = AssetDatabase.LoadAssetAtPath<Object>(WORLDS_DATABASE);
            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty world = serialized.FindProperty("worlds").GetArrayElementAtIndex(1).FindPropertyRelative("scene");
            world.FindPropertyRelative("scene").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);
            world.FindPropertyRelative("path").stringValue = SCENE_PATH;
            world.FindPropertyRelative("name").stringValue = System.IO.Path.GetFileNameWithoutExtension(SCENE_PATH);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == SCENE_PATH))
            {
                scenes.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.SaveAssets();
        }
    }
}
