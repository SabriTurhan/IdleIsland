using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace IslandEscape.World2
{
    // Builds the new World 2 resources: models, pickable/flying prefabs, the Iron Ore rock, icons,
    // currency database entries and the TMP sprite atlas used by "<sprite name=...>" texts.
    public static class World2ResourcesBuilder
    {
        private const string PREFABS = "Assets/Project Files/Game/Prefabs/Drop/";
        private const string CURRENCY_DATABASE = "Assets/Project Files/Data/Currencies Database.asset";
        private const string ICON_TEMPLATE = "Assets/Project Files/Game/Images/Drop/ic_stone.png";
        private const string DEFAULT_SPRITE_ASSET = "Assets/Project Files/Game/Images/Drop/Generated Atlas.asset";
        private const string WORLD2_SPRITE_ASSET = PirateArtAssets.ROOT + "/Icons/World2 Atlas.asset";
        private const string WORLD2_SPRITE_TEXTURE = PirateArtAssets.ROOT + "/Icons/world2_atlas.png";

        public const string IRON_ORE_SOURCE = PirateArtAssets.ROOT + "/Prefabs/Iron Ore Resource Source.prefab";
        private const int IRON_ORE_HITTABLE_ID = 21;

        private class ResourceDefinition
        {
            public CurrencyType Type;
            public string Name;
            public System.Func<Mesh> Mesh;
            public Vector3 IconEuler;
            public CurrencyType Template;
            public bool UseInventory;
            public int SellPrice;
        }

        private static readonly ResourceDefinition[] RESOURCES =
        {
            new ResourceDefinition { Type = CurrencyType.IronOre, Name = "Iron Ore", Mesh = PirateModels.IronOre, IconEuler = new Vector3(0, 25, 0), Template = CurrencyType.Stone, UseInventory = true, SellPrice = 3 },
            new ResourceDefinition { Type = CurrencyType.IronBar, Name = "Iron Bar", Mesh = PirateModels.IronBar, IconEuler = new Vector3(0, -20, 0), Template = CurrencyType.Bricks, UseInventory = true, SellPrice = 25 },
            new ResourceDefinition { Type = CurrencyType.Sailcloth, Name = "Sailcloth", Mesh = PirateModels.Sailcloth, IconEuler = new Vector3(0, -25, 0), Template = CurrencyType.Rope, UseInventory = true, SellPrice = 30 },
            new ResourceDefinition { Type = CurrencyType.Torch, Name = "Torch", Mesh = PirateModels.Torch, IconEuler = new Vector3(0, 0, -28), Template = CurrencyType.Scissors, UseInventory = false, SellPrice = 0 },
            new ResourceDefinition { Type = CurrencyType.MapPiece, Name = "Map Piece", Mesh = PirateModels.MapPiece, IconEuler = new Vector3(-55, 10, 0), Template = CurrencyType.Scissors, UseInventory = false, SellPrice = 0 },
        };

        [MenuItem("Tools/World 2/1. Build Resources", priority = 1)]
        public static void BuildAll()
        {
            PirateArtAssets.BuildAtlas();
            Material material = PirateArtAssets.BuildMaterials();

            Dictionary<CurrencyType, Sprite> icons = new Dictionary<CurrencyType, Sprite>();
            Dictionary<CurrencyType, GameObject[]> prefabs = new Dictionary<CurrencyType, GameObject[]>();

            using (PirateIconRenderer iconRenderer = new PirateIconRenderer())
            {
                foreach (ResourceDefinition resource in RESOURCES)
                {
                    Mesh mesh = PirateArtAssets.SaveMesh(resource.Mesh());

                    prefabs[resource.Type] = new[]
                    {
                        ClonePickable("Pickable Resources", resource.Template, "Drop Resource", resource.Name + " Drop Resource", mesh, material, resource.Type),
                        ClonePickable("Flying Resources", resource.Template, "Fly Resourse", resource.Name + " Fly Resourse", mesh, material, resource.Type),
                    };

                    Texture2D icon = iconRenderer.RenderMesh(mesh, material, resource.IconEuler);
                    string iconPath = PirateArtAssets.ROOT + "/Icons/ic_" + resource.Name.ToLower().Replace(' ', '_') + ".png";
                    icons[resource.Type] = PirateIconRenderer.SaveSprite(icon, iconPath, ICON_TEMPLATE);
                }
            }

            UpdateCurrencyDatabase(icons, prefabs);
            BuildIronOreSource(material);
            RebuildSpriteAtlas();

            AssetDatabase.SaveAssets();
            Debug.Log("[World 2] Resources built");
        }

        // copies the template currency prefab (behaviour, collider, animation) and swaps the model
        private static GameObject ClonePickable(string folder, CurrencyType template, string suffix, string name, Mesh mesh, Material material, CurrencyType type)
        {
            string templateName = template == CurrencyType.Bricks ? "Brick" : template.ToString();
            // template prefabs are named "<Name> Fly Resource" or "<Name> Fly Resourse"
            string templatePath = PREFABS + folder + "/" + templateName + " " + suffix + ".prefab";
            string alternativePath = templatePath.Replace("Resourse", "Resource");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(templatePath) == null) templatePath = alternativePath;
            GameObject templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (templatePrefab == null)
            {
                Debug.LogError("[World 2] Missing template " + templatePath);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(templatePrefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = name;

            MeshFilter filter = instance.GetComponentInChildren<MeshFilter>(true);
            filter.sharedMesh = mesh;
            filter.name = name.Replace(" " + suffix, "");
            filter.GetComponent<MeshRenderer>().sharedMaterial = material;

            // the template prefab names its own currency: the picked up / arriving item would be added as that currency
            foreach (MonoBehaviour behaviour in instance.GetComponents<MonoBehaviour>())
            {
                SerializedObject serialized = new SerializedObject(behaviour);
                foreach (string field in new[] { "resourceType", "currencyType" })
                {
                    SerializedProperty property = serialized.FindProperty(field);
                    if (property != null) property.intValue = (int)type;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            BoxCollider collider = instance.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.center = mesh.bounds.center;
                collider.size = Vector3.Max(mesh.bounds.size, Vector3.one * 0.3f);
            }

            string path = PirateArtAssets.ROOT + "/Prefabs/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);

            return prefab;
        }

        private static void UpdateCurrencyDatabase(Dictionary<CurrencyType, Sprite> icons, Dictionary<CurrencyType, GameObject[]> prefabs)
        {
            Object database = AssetDatabase.LoadAssetAtPath<Object>(CURRENCY_DATABASE);
            SerializedObject serializedObject = new SerializedObject(database);
            SerializedProperty currencies = serializedObject.FindProperty("currencies");

            foreach (ResourceDefinition resource in RESOURCES)
            {
                int index = FindCurrency(currencies, resource.Type);
                if (index < 0)
                {
                    int templateIndex = FindCurrency(currencies, resource.Template);
                    currencies.InsertArrayElementAtIndex(templateIndex);
                    currencies.MoveArrayElement(templateIndex, currencies.arraySize - 1);
                    index = currencies.arraySize - 1;
                }

                SerializedProperty element = currencies.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("currencyType").intValue = (int)resource.Type;
                element.FindPropertyRelative("defaultAmount").intValue = 0;
                element.FindPropertyRelative("icon").objectReferenceValue = icons[resource.Type];

                SerializedProperty data = element.FindPropertyRelative("data");
                data.FindPropertyRelative("flyingResPrefab").objectReferenceValue = prefabs[resource.Type][1];
                data.FindPropertyRelative("dropResPrefab").objectReferenceValue = prefabs[resource.Type][0];
                data.FindPropertyRelative("useInventory").boolValue = resource.UseInventory;
                data.FindPropertyRelative("moneyConversionRate").intValue = resource.SellPrice;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static int FindCurrency(SerializedProperty currencies, CurrencyType type)
        {
            for (int i = 0; i < currencies.arraySize; i++)
            {
                if (currencies.GetArrayElementAtIndex(i).FindPropertyRelative("currencyType").intValue == (int)type)
                    return i;
            }

            return -1;
        }

        // Iron ore rock: the stone rock (keeps its layered harvest stages), darkened, with ore nuggets in every layer
        private static void BuildIronOreSource(Material pirateMaterial)
        {
            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "Stone Resource Source.prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            instance.name = "Iron Ore Resource Source";

            Material ironMaterial = IronRockMaterial();
            foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
                renderer.sharedMaterial = ironMaterial;

            int seed = 1;
            foreach (ResourceVisualStage stage in instance.GetComponentsInChildren<ResourceVisualStage>(true))
            {
                Bounds bounds = new Bounds(stage.transform.position, Vector3.zero);
                foreach (MeshRenderer renderer in stage.GetComponentsInChildren<MeshRenderer>(true)) bounds.Encapsulate(renderer.bounds);

                Transform old = stage.transform.Find("Ore Nuggets");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                GameObject nuggets = new GameObject("Ore Nuggets");
                nuggets.transform.SetParent(stage.transform, false);
                nuggets.transform.position = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.35f, bounds.center.z);
                float radius = Mathf.Max(0.2f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.85f);
                nuggets.AddComponent<MeshFilter>().sharedMesh = PirateArtAssets.SaveMesh(PirateModels.OreNuggets(seed++, radius, 4));
                nuggets.AddComponent<MeshRenderer>().sharedMaterial = pirateMaterial;
            }

            ResourceSourceBehavior source = instance.GetComponent<ResourceSourceBehavior>();
            SerializedObject serializedSource = new SerializedObject(source);
            serializedSource.FindProperty("hittableId").intValue = IRON_ORE_HITTABLE_ID;
            serializedSource.FindProperty("hitsToDestroy").intValue = 7;
            serializedSource.FindProperty("respawnDuration").floatValue = 60f;
            SerializedProperty drop = serializedSource.FindProperty("drop");
            drop.arraySize = 1;
            drop.GetArrayElementAtIndex(0).FindPropertyRelative("currency").intValue = (int)CurrencyType.IronOre;
            drop.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 5;
            serializedSource.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, IRON_ORE_SOURCE);
            Object.DestroyImmediate(instance);
        }

        // the rock texture tinted towards dark iron
        private static Material IronRockMaterial()
        {
            const string path = PirateArtAssets.ROOT + "/Materials/Resources Iron.mat";
            Material template = AssetDatabase.LoadAssetAtPath<Material>("Assets/Project Files/Game/Materials/Resources.mat");
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

            material.SetColor("_Color", new Color(0.62f, 0.66f, 0.76f));
            EditorUtility.SetDirty(material);

            return material;
        }

        // icons that appear in texts without being currencies
        private static readonly string[] EXTRA_ICONS = { "Skull", "Sword" };

        // second TMP sprite asset with the new icons, registered as a fallback of the default one
        public static void RebuildSpriteAtlas()
        {
            List<KeyValuePair<string, Sprite>> icons = new List<KeyValuePair<string, Sprite>>();
            foreach (ResourceDefinition resource in RESOURCES)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PirateArtAssets.ROOT + "/Icons/ic_" + resource.Name.ToLower().Replace(' ', '_') + ".png");
                if (sprite != null) icons.Add(new KeyValuePair<string, Sprite>(resource.Type.ToString(), sprite));
            }

            foreach (string extra in EXTRA_ICONS)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PirateArtAssets.ROOT + "/Icons/ic_" + extra.ToLower() + ".png");
                if (sprite != null) icons.Add(new KeyValuePair<string, Sprite>(extra, sprite));
            }

            BuildSpriteAtlas(icons);
        }

        private static void BuildSpriteAtlas(List<KeyValuePair<string, Sprite>> icons)
        {
            const int CELL = 128;
            int columns = 4;
            int rows = Mathf.CeilToInt(icons.Count / (float)columns);
            int width = columns * CELL, height = Mathf.NextPowerOfTwo(rows * CELL);

            Texture2D sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            sheet.SetPixels(new Color[width * height]);

            List<Rect> rects = new List<Rect>();
            for (int i = 0; i < icons.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(icons[i].Value);
                Texture2D source = new Texture2D(2, 2);
                source.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), path)));

                int x = (i % columns) * CELL;
                int y = height - CELL - (i / columns) * CELL;
                for (int py = 0; py < CELL; py++)
                    for (int px = 0; px < CELL; px++)
                        sheet.SetPixel(x + px, y + py, source.GetPixelBilinear((px + 0.5f) / CELL, (py + 0.5f) / CELL));

                rects.Add(new Rect(x, y, CELL, CELL));
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), WORLD2_SPRITE_TEXTURE), sheet.EncodeToPNG());
            AssetDatabase.ImportAsset(WORLD2_SPRITE_TEXTURE);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(WORLD2_SPRITE_TEXTURE);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WORLD2_SPRITE_TEXTURE);

            TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(WORLD2_SPRITE_ASSET);
            if (spriteAsset == null)
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, WORLD2_SPRITE_ASSET);

                Material spriteMaterial = new Material(UnityEngine.Shader.Find("TextMeshPro/Sprite"));
                spriteMaterial.name = "World2 Atlas Material";
                spriteMaterial.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(spriteMaterial, spriteAsset);
                spriteAsset.material = spriteMaterial;
            }

            // a fresh sprite asset has no version yet, TMP would try to "upgrade" it from the legacy format
            SerializedObject serializedSpriteAsset = new SerializedObject(spriteAsset);
            serializedSpriteAsset.FindProperty("m_Version").stringValue = "1.1.0";
            serializedSpriteAsset.ApplyModifiedPropertiesWithoutUndo();

            spriteAsset.spriteSheet = texture;
            spriteAsset.material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteCharacterTable.Clear();

            for (int i = 0; i < icons.Count; i++)
            {
                Rect rect = rects[i];
                TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(CELL, CELL, -11, 108, 140),
                    glyphRect = new GlyphRect((int)rect.x, (int)rect.y, CELL, CELL),
                    scale = 1.2f,
                    atlasIndex = 0,
                };
                spriteAsset.spriteGlyphTable.Add(glyph);

                TMP_SpriteCharacter character = new TMP_SpriteCharacter(0xFFFE, glyph) { name = icons[i].Key, scale = 1f };
                spriteAsset.spriteCharacterTable.Add(character);
            }

            spriteAsset.UpdateLookupTables();
            EditorUtility.SetDirty(spriteAsset);

            TMP_SpriteAsset defaultAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(DEFAULT_SPRITE_ASSET);
            if (defaultAsset.fallbackSpriteAssets == null) defaultAsset.fallbackSpriteAssets = new List<TMP_SpriteAsset>();
            if (!defaultAsset.fallbackSpriteAssets.Contains(spriteAsset))
            {
                defaultAsset.fallbackSpriteAssets.Add(spriteAsset);
                EditorUtility.SetDirty(defaultAsset);
            }
        }
    }
}
