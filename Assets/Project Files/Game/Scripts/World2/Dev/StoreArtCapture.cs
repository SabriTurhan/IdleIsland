#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IslandEscape
{
    // Development only: renders the running game from a free camera (no UI) into a PNG, for the store icon,
    // cover and loader logo. The game's own camera settings (URP renderer, post-processing) are copied.
    public static class StoreArtCapture
    {
        public static string Root => Path.Combine(Application.dataPath, "../Temp/StoreArt");
        public static string ShotsRoot => Path.Combine(Application.dataPath, "../Temp/StoreShots");

        public static bool IsShooting { get; private set; }

        /// <summary>Real gameplay screenshot (with the game UI) in portrait 720x1280 and landscape 1280x720, rendered at 2x.</summary>
        public static void Screenshot(string name)
        {
            IsShooting = true;
            GameObject runner = new GameObject("Store Screenshot");
            runner.AddComponent<ShotRunner>().StartCoroutine(ShotRoutine(runner, name));
        }

        private class ShotRunner : MonoBehaviour { }

        private static System.Collections.IEnumerator ShotRoutine(GameObject runner, string name)
        {
            Directory.CreateDirectory(ShotsRoot);
            foreach (Vector2Int size in new[] { new Vector2Int(720, 1280), new Vector2Int(1280, 720) })
            {
                SetGameViewResolution(size.x, size.y);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return new WaitForEndOfFrame();
                string orientation = size.x < size.y ? "portrait" : "landscape";
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(ShotsRoot, name + "_" + orientation + ".png")), 2);
                yield return new WaitForSecondsRealtime(0.4f);
            }

            IsShooting = false;
            Object.Destroy(runner);
        }

        public static bool IsRecording { get; private set; }

        /// <summary>
        /// Gameplay video frames: fixed 30 fps game time (every rendered frame is one video frame), JPG files
        /// Temp/StoreVideo/name/00000.jpg..., assembled with ffmpeg afterwards.
        /// </summary>
        public static void Record(string name, int width, int height, float seconds)
        {
            IsRecording = true;
            GameObject runner = new GameObject("Store Video");
            runner.AddComponent<ShotRunner>().StartCoroutine(RecordRoutine(runner, name, width, height, seconds));
        }

        private static System.Collections.IEnumerator RecordRoutine(GameObject runner, string name, int width, int height, float seconds)
        {
            string folder = Path.Combine(Application.dataPath, "../Temp/StoreVideo", name);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);

            SetGameViewResolution(width, height);
            yield return new WaitForSecondsRealtime(0.6f);

            Time.captureFramerate = 30;
            int frames = Mathf.RoundToInt(seconds * 30f);
            for (int i = 0; i < frames; i++)
            {
                yield return new WaitForEndOfFrame();
                Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, i.ToString("00000") + ".jpg"), texture.EncodeToJPG(92));
                Object.Destroy(texture);
            }
            Time.captureFramerate = 0;

            IsRecording = false;
            Object.Destroy(runner);
        }

        public static void SetGameViewResolution(int width, int height)
        {
            System.Reflection.Assembly editor = typeof(UnityEditor.Editor).Assembly;
            System.Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            System.Type singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            System.Type groupType = group.GetType();

            string label = "QA " + width + "x" + height;
            int index = -1;
            string[] texts = (string[])groupType.GetMethod("GetDisplayTexts").Invoke(group, null);
            for (int i = 0; i < texts.Length; i++)
                if (texts[i].StartsWith(label)) index = i;

            if (index < 0)
            {
                System.Type sizeType = editor.GetType("UnityEditor.GameViewSize");
                System.Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
                object size = System.Activator.CreateInstance(sizeType, System.Enum.Parse(sizeKind, "FixedResolution"), width, height, label);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }

            System.Type gameViewType = editor.GetType("UnityEditor.GameView");
            UnityEditor.EditorWindow gameView = UnityEditor.EditorWindow.GetWindow(gameViewType, false, null, false);
            gameViewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(gameView, index);
            gameView.Repaint();
        }

        public static string Capture(string name, int width, int height, Vector3 position, Vector3 lookAt, float fieldOfView, int supersample = 2)
        {
            Camera source = Camera.main;
            GameObject cameraObject = new GameObject("Store Art Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(source);
            camera.enabled = false;

            UniversalAdditionalCameraData sourceData = source.GetComponent<UniversalAdditionalCameraData>();
            UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            if (sourceData != null)
            {
                data.renderPostProcessing = sourceData.renderPostProcessing;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }

            // the game camera never sees the sky: a flat key colour, replaced by a sky gradient afterwards
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0f, 1f, 1f);

            camera.transform.position = position;
            camera.transform.LookAt(lookAt);
            camera.fieldOfView = fieldOfView;
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 400f);

            // the world curves away from the player (CurvatureManager): flat for a free camera, restored after
            string[] curveProperties = { "_CurveForwardOffset", "_CurveForwardPower", "_CurveSideOffset", "_CurveSidePower" };
            float[] curveValues = new float[curveProperties.Length];
            for (int i = 0; i < curveProperties.Length; i++)
            {
                curveValues[i] = Shader.GetGlobalFloat(curveProperties[i]);
                Shader.SetGlobalFloat(curveProperties[i], 0f);
            }

            RenderTexture target = new RenderTexture(width * supersample, height * supersample, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = null;

            for (int i = 0; i < curveProperties.Length; i++) Shader.SetGlobalFloat(curveProperties[i], curveValues[i]);

            RenderTexture small = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Bilinear;
            Graphics.Blit(target, small);
            RenderTexture.active = small;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(small);
            target.Release();
            Object.Destroy(cameraObject);

            Directory.CreateDirectory(Root);
            string path = Path.GetFullPath(Path.Combine(Root, name + ".png"));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.Destroy(texture);

            return path;
        }
    }
}
#endif
