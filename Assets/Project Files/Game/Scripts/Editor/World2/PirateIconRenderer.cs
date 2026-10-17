using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IslandEscape.World2
{
    // Renders resource icons in the style of Images/Drop/ic_*.png: soft 3D render, thick dark outline, no background.
    public class PirateIconRenderer : IDisposable
    {
        public const int SIZE = 512;
        public static readonly Color OUTLINE_COLOR = new Color32(0x30, 0x30, 0x30, 0xff);

        private readonly Scene scene;
        private readonly Camera camera;
        private readonly RenderTexture target;

        public PirateIconRenderer()
        {
            scene = EditorSceneManager.NewPreviewScene();

            GameObject lightObject = new GameObject("Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            SceneManager.MoveGameObjectToScene(lightObject, scene);

            // soft fill so the shadow side keeps its colour (preview scenes have no ambient light)
            GameObject fillObject = new GameObject("Fill");
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.85f, 0.9f, 1f);
            fill.shadows = LightShadows.None;
            fillObject.transform.rotation = Quaternion.Euler(10f, 150f, 0f);
            SceneManager.MoveGameObjectToScene(fillObject, scene);

            GameObject cameraObject = new GameObject("Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            SceneManager.MoveGameObjectToScene(cameraObject, scene);

            // rendered at twice the size and filtered down instead of MSAA (MSAA breaks preview-scene renders in URP)
            target = new RenderTexture(SIZE * 2, SIZE * 2, 24, RenderTextureFormat.ARGB32);
        }

        public Texture2D RenderMesh(Mesh mesh, Material material, Vector3 euler)
        {
            GameObject subject = new GameObject("Icon Subject");
            subject.AddComponent<MeshFilter>().sharedMesh = mesh;
            subject.AddComponent<MeshRenderer>().sharedMaterial = IconMaterial(material);
            subject.transform.rotation = Quaternion.Euler(euler);
            SceneManager.MoveGameObjectToScene(subject, scene);

            Texture2D texture = Render(subject);
            UnityEngine.Object.DestroyImmediate(subject);

            return texture;
        }

        public Texture2D Render(GameObject subject)
        {
            Bounds bounds = new Bounds(subject.transform.position, Vector3.zero);
            bool first = true;
            foreach (Renderer renderer in subject.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer) continue;
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }

            // slightly from above, like the template icons
            Vector3 direction = Quaternion.Euler(28f, 0f, 0f) * Vector3.forward;
            // fit the projected silhouette, not the bounding sphere
            float radius = 0f;
            Matrix4x4 view = Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(direction), Vector3.one).inverse;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = view.MultiplyPoint3x4(corner - bounds.center);
                radius = Mathf.Max(radius, Mathf.Abs(local.x), Mathf.Abs(local.y));
            }
            radius *= 1.04f;
            camera.orthographic = true;
            camera.orthographicSize = radius;
            camera.transform.position = bounds.center - direction * radius * 4f;
            camera.transform.LookAt(bounds.center);

            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = null;

            RenderTexture small = RenderTexture.GetTemporary(SIZE, SIZE, 0, RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Bilinear;
            Graphics.Blit(target, small);

            RenderTexture.active = small;
            Texture2D texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(small);

            return Outline(Shrink(texture, 0.9f), 11);
        }

        private Material iconMaterial;

        // icons use smooth lit shading (like the template's rendered icons), the game shader is too flat without its globals
        private Material IconMaterial(Material source)
        {
            if (iconMaterial == null)
            {
                iconMaterial = new Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
                iconMaterial.SetFloat("_Smoothness", 0.35f);
                iconMaterial.SetFloat("_Metallic", 0f);
            }

            iconMaterial.SetTexture("_BaseMap", source.mainTexture);
            iconMaterial.SetColor("_BaseColor", Color.white);

            return iconMaterial;
        }

        public void Dispose()
        {
            target.Release();
            EditorSceneManager.ClosePreviewScene(scene);
        }

        // leaves room for the outline
        private static Texture2D Shrink(Texture2D source, float factor)
        {
            int size = source.width;
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float offset = size * (1f - factor) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x - offset) / (size * factor);
                    float v = (y - offset) / (size * factor);
                    pixels[y * size + x] = u < 0 || v < 0 || u > 1 || v > 1 ? Color.clear : source.GetPixelBilinear(u, v);
                }
            }

            result.SetPixels(pixels);
            result.Apply();

            return result;
        }

        // thick dark outline around the silhouette (anti-aliased)
        public static Texture2D Outline(Texture2D source, int width)
        {
            int size = source.width;
            Color[] pixels = source.GetPixels();
            float[] distance = new float[pixels.Length];
            for (int i = 0; i < distance.Length; i++) distance[i] = pixels[i].a > 0.5f ? 0f : float.MaxValue;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (pixels[y * size + x].a <= 0.5f) continue;

                    bool edge = false;
                    for (int k = 0; k < 4 && !edge; k++)
                    {
                        int ex = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ey = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        edge = ex < 0 || ey < 0 || ex >= size || ey >= size || pixels[ey * size + ex].a <= 0.5f;
                    }
                    if (!edge) continue;

                    for (int dy = -width - 1; dy <= width + 1; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= size) continue;
                        for (int dx = -width - 1; dx <= width + 1; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= size) continue;
                            int index = ny * size + nx;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            if (d < distance[index]) distance[index] = d;
                        }
                    }
                }
            }

            Color[] result = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color colour = pixels[i];
                Color under = OUTLINE_COLOR;
                under.a = distance[i] <= width ? 1f : Mathf.Clamp01(width + 1f - distance[i]);

                result[i] = Color.Lerp(under, new Color(colour.r, colour.g, colour.b, 1f), colour.a);
                result[i].a = Mathf.Max(colour.a, under.a);
            }

            // a thin dark rim inside the silhouette, like the hand drawn outline of the template icons
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0.5f && distance[i] == 0f)
                {
                    int x = i % size, y = i / size;
                    bool nearEdge = false;
                    for (int k = 0; k < 4 && !nearEdge; k++)
                    {
                        int ex = x + (k == 0 ? 3 : k == 1 ? -3 : 0), ey = y + (k == 2 ? 3 : k == 3 ? -3 : 0);
                        nearEdge = ex < 0 || ey < 0 || ex >= size || ey >= size || pixels[ey * size + ex].a <= 0.5f;
                    }
                    if (nearEdge) result[i] = Color.Lerp(result[i], OUTLINE_COLOR, 0.85f);
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(result);
            texture.Apply();

            return texture;
        }

        // writes a PNG and copies the import settings of an existing template icon
        public static Sprite SaveSprite(Texture2D texture, string path, string importTemplate)
        {
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), path), texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            TextureImporter source = (TextureImporter)AssetImporter.GetAtPath(importTemplate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            TextureImporterSettings settings = new TextureImporterSettings();
            source.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.isReadable = source.isReadable;
            foreach (string platform in new[] { "Android", "iPhone", "Standalone" })
                importer.SetPlatformTextureSettings(source.GetPlatformTextureSettings(platform));
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
