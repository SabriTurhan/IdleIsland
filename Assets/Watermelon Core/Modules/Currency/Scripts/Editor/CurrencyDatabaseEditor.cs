using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using System;




#if MODULE_TMP
using TMPro;
#endif

namespace Watermelon
{
    [CustomEditor(typeof(CurrencyDatabase))]
    public class CurrencyDatabaseEditor : CustomInspector
    {
        private CurrencyDatabase currencyDatabase;

        protected override void OnEnable()
        {
            base.OnEnable();

            currencyDatabase = (CurrencyDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

#if MODULE_TMP
            if (GUILayout.Button("Create Sprite Atlas"))
            {
                CreateAtlas();
            }
#endif
        }

#if MODULE_TMP
        private void CreateAtlas()
        {
            if (currencyDatabase == null) return;

            List<TMPAtlasGenerator.SpriteData> atlasElements = new List<TMPAtlasGenerator.SpriteData>();

            Currency[] currencies = currencyDatabase.Currencies;

            //Set Full Rect Type manualy
            for (int i = 0; i < currencies.Length; i++)
            {
                if (currencies[i].Icon != null)
                {
                    TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(currencies[i].Icon));

                    TextureImporterSettings settings = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    textureImporter.SetTextureSettings(settings);
                    textureImporter.isReadable = true;
                    textureImporter.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();

            for (int i = 0; i < currencies.Length; i++)
            {
                if (currencies[i].Icon != null)
                {
                    atlasElements.Add(new TMPAtlasGenerator.SpriteData(currencies[i].Icon, currencies[i].CurrencyType.ToString()));
                }
            }

            TMPAtlasGenerator.Create(atlasElements);
        }

        public class TMPAtlasGenerator
        {
            private const string FILE_PATH_SAVE = "atlas_generator_file_path";
            private List<SpriteData> elements;
            private string filePath;
            private int atlasWidth;
            private int atlasHeight;

            public TMPAtlasGenerator()
            {
                elements = new List<SpriteData>();
            }

            private void AddDefaultMaterial(TMP_SpriteAsset spriteAsset)
            {
                UnityEngine.Shader shader = UnityEngine.Shader.Find("TextMeshPro/Sprite");
                Material material = new Material(shader);
                material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

                spriteAsset.material = material;
                material.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(material, spriteAsset);
            }

            public static TMPAtlasGenerator Create(List<SpriteData> atlasElements)
            {
                TMPAtlasGenerator atlasGenerator = new TMPAtlasGenerator();

                EditorCoroutines.Execute(atlasGenerator.AtlasCoroutine(atlasElements));

                return atlasGenerator;
            }

            public IEnumerator AtlasCoroutine(List<SpriteData> atlasElements)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    string savedPath = EditorPrefs.GetString(FILE_PATH_SAVE);
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        savedPath = Path.GetDirectoryName(savedPath);
                    }
                    else
                    {
                        savedPath = "Assets";
                    }

                    filePath = EditorUtility.SaveFilePanelInProject("Generated Atlas", "Generated Atlas", "asset", "Select atlas path", savedPath);

                    if (string.IsNullOrEmpty(filePath))
                    {
                        Debug.LogError("[Atlas Generator]: Path can't be empty!");

                        yield break;
                    }

                    EditorPrefs.SetString(FILE_PATH_SAVE, Path.GetDirectoryName(filePath).Replace('\\','/') + '/');
                }

                if (atlasElements.IsNullOrEmpty())
                {
                    Debug.LogError("[Atlas Generator]: Sprites list is empty!");

                    yield break;
                }

                for (int i = 0; i < atlasElements.Count; i++)
                {
                    elements.Add(atlasElements[i]);
                }

                //Handle possible override
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(filePath, AssetPathToGUIDOptions.OnlyExistingAssets)))
                {
                    TMP_SpriteAsset TMP_AssetFile = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(filePath);

                    if ((TMP_AssetFile != null) && (EditorUtility.DisplayDialog("Asset override", "Do you want to copy sprite GlyphMetrics and scale from overriden asset?", "Yes", "No")))
                    {
                        for (int i = 0; i < TMP_AssetFile.spriteCharacterTable.Count; i++)
                        {
                            for (int j = 0; j < elements.Count; j++)
                            {
                                if (!elements[j].OverrideDataSet)
                                {
                                    if (elements[j].Name.Equals(TMP_AssetFile.spriteCharacterTable[i].name))
                                    {
                                        elements[j].GlyphMetrics = TMP_AssetFile.spriteCharacterTable[i].glyph.metrics;
                                        elements[j].Scale = TMP_AssetFile.spriteCharacterTable[i].glyph.scale;
                                        elements[j].OverrideDataSet = true;
                                    }
                                }
                            }
                        }
                    }
                }

                string texturePath = filePath.Replace(".asset", ".png");
                texturePath = ChangeFileName(texturePath);
                CreateAtlasPNG(texturePath);
                RemoveReadWriteForSprites();

                CreateSpriteAsset(texturePath);

                //Applying Ref to TMP settings
                TMP_Settings settings = EditorUtils.GetAsset<TMP_Settings>();

                if (settings != null)
                {
                    if (EditorUtility.DisplayDialog("Linking asset to TMP Settings", "Do you want to add created \"Sprite Atlas\" to \"TMP Settings\"?", "Yes", "Cancel"))
                    {
                        SerializedObject settingsSerializedObject = new SerializedObject(settings);
                        SerializedProperty defaultAssetProperty = settingsSerializedObject.FindProperty("m_defaultSpriteAsset");
                        string TMP_AssetFilePath = filePath.Replace(".spriteatlasv2", ".asset");
                        TMP_SpriteAsset TMP_AssetFile = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(TMP_AssetFilePath);

                        if ((defaultAssetProperty.objectReferenceValue == null) || EditorUtility.DisplayDialog("Linking asset to TMP Settings", "Do you want to override \"Default Sprite Asset\" reference with created \"Sprite Atlas\" or add reference to fallback list of current \"Default Sprite Asset\" ?", "Override", "Add to fallback list"))
                        {
                            defaultAssetProperty.objectReferenceValue = TMP_AssetFile;
                            settingsSerializedObject.ApplyModifiedProperties();
                        }
                        else
                        {
                            SerializedObject spriteAssetSerializedObject = new SerializedObject(defaultAssetProperty.objectReferenceValue);
                            SerializedProperty fallbackAssetsProperty = spriteAssetSerializedObject.FindProperty("fallbackSpriteAssets");
                            fallbackAssetsProperty.arraySize++;
                            fallbackAssetsProperty.GetArrayElementAtIndex(fallbackAssetsProperty.arraySize - 1).objectReferenceValue = TMP_AssetFile;
                            spriteAssetSerializedObject.ApplyModifiedProperties();
                        }
                    }
                }
                
            }

            private string ChangeFileName(string texturePath)
            {
                string originalFileName = Path.GetFileName(texturePath);
                string newFileName = originalFileName.ToLower().Replace(' ', '_');
                return texturePath.Replace(originalFileName, newFileName);
            }

            private void CreateSpriteAsset(string texturePath)
            {
                // Get the path to the selected asset.
                string fileNameWithExtension = Path.GetFileName(this.filePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(this.filePath);
                string filePath = this.filePath.Replace(fileNameWithExtension, "");

                // Create new Sprite Asset
                TMP_SpriteAsset spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, filePath + fileNameWithoutExtension + ".asset");

                ReflectionUtils.InjectInstanceComponent(spriteAsset, "m_Version", "1.1.0");

                // Compute the hash code for the sprite asset.
                spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);

                List<TMP_SpriteGlyph> spriteGlyphTable = new List<TMP_SpriteGlyph>();
                List<TMP_SpriteCharacter> spriteCharacterTable = new List<TMP_SpriteCharacter>();

                for (int i = 0; i < elements.Count; i++)
                {
                    Sprite sprite = elements[i].Sprite;
                    TMP_SpriteGlyph spriteGlyph = new TMP_SpriteGlyph();
                    spriteGlyph.index = (uint)i;

                    if (elements[i].OverrideDataSet)
                    {
                        spriteGlyph.metrics = elements[i].GlyphMetrics;
                        spriteGlyph.scale = elements[i].Scale;
                    }
                    else
                    {
                        spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width);
                        spriteGlyph.scale = 1.0f;
                    }

                    float x = elements[i].TextureRect.x * atlasWidth;
                    float y = elements[i].TextureRect.y * atlasHeight;
                    float width = elements[i].TextureRect.width * atlasWidth;
                    float height = elements[i].TextureRect.height * atlasHeight;
                    Rect elementRect = new Rect(x, y, width, height);

                    spriteGlyph.glyphRect = new GlyphRect(elementRect);
                    spriteGlyph.sprite = sprite;
                    spriteGlyphTable.Add(spriteGlyph);

                    TMP_SpriteCharacter spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph);
                    spriteCharacter.name = elements[i].Name;
                    spriteCharacter.scale = 1.0f;

                    spriteCharacterTable.Add(spriteCharacter);
                }

                string glyphTableName = "m_SpriteGlyphTable";
                if (ReflectionUtils.FieldExists(spriteAsset, "m_GlyphTable"))
                    glyphTableName = "m_GlyphTable";

                ReflectionUtils.InjectInstanceComponent(spriteAsset, "m_SpriteCharacterTable", spriteCharacterTable);
                ReflectionUtils.InjectInstanceComponent(spriteAsset, glyphTableName, spriteGlyphTable);

                spriteAsset.spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                // Add new default material for sprite asset.
                AddDefaultMaterial(spriteAsset);

                // Update Lookup tables.
                spriteAsset.UpdateLookupTables();

                // Get the Sprites contained in the Sprite Sheet
                EditorUtility.SetDirty(spriteAsset);

                AssetDatabase.SaveAssets();

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));
            }

            private void CreateAtlasPNG(string texturePath)
            {
                Texture2D[] textures = new Texture2D[elements.Count];

                for (int i = 0; i < elements.Count; i++)
                {
                    Texture2D sourceTex = elements[i].Sprite.texture;
                    int width = Mathf.RoundToInt(elements[i].Sprite.rect.width);
                    int height = Mathf.RoundToInt(elements[i].Sprite.rect.height);
                    int x = Mathf.RoundToInt(elements[i].Sprite.rect.x);
                    int y = Mathf.RoundToInt(elements[i].Sprite.rect.y);
                    Texture2D cropped = new Texture2D(width, height);
                    cropped.SetPixels(sourceTex.GetPixels(x,y,width,height));
                    cropped.Apply();
                    textures[i] = cropped;
                }


                Texture2D atlas = new Texture2D(4096, 4096);
                Rect[] rects = atlas.PackTextures(textures, 0, 4096);

                for (int i = 0; i < elements.Count; i++)
                {
                    elements[i].TextureRect = rects[i];
                }

                atlas.Apply();
                atlasWidth = atlas.width;
                atlasHeight = atlas.height;
                File.WriteAllBytes(texturePath, atlas.EncodeToPNG());

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(texturePath);
                textureImporter.alphaIsTransparency = true;
                textureImporter.SaveAndReimport();
                AssetDatabase.Refresh();
            }

            private void RemoveReadWriteForSprites()
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(elements[i].Sprite));
                    textureImporter.isReadable = false;
                    textureImporter.SaveAndReimport();
                    
                }

                AssetDatabase.Refresh();
            }

            public class SpriteData
            {
                private Sprite sprite;
                private string name;
                private bool overrideDataSet;
                private GlyphMetrics glyphMetrics;
                private float scale;
                private Rect textureRect;

                public Sprite Sprite => sprite;
                public string Name => name;

                public bool OverrideDataSet { get => overrideDataSet; set => overrideDataSet = value; }
                public GlyphMetrics GlyphMetrics { get => glyphMetrics; set => glyphMetrics = value; }
                public float Scale { get => scale; set => scale = value; }
                public Rect TextureRect { get => textureRect; set => textureRect = value; }

                public SpriteData(Sprite sprite, string name)
                {
                    this.sprite = sprite;
                    this.name = name;
                    overrideDataSet = false;
                }
            }
        }
#endif
    }
}
