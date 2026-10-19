using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IslandEscape
{
    // Yandex Games build: WebGL with the IslandEscape page template (SDK init, layout, cloud save),
    // Gzip with decompression fallback (works whatever headers the host sends), zipped with index.html at
    // the archive root. Output: Builds/Yandex (folder) and Builds/IslandEscape_Yandex.zip.
    public static class YandexBuild
    {
        public const string OUTPUT = "Builds/Yandex";
        public const string ARCHIVE = "Builds/IslandEscape_Yandex.zip";

        [MenuItem("Tools/Yandex Games/Apply WebGL Settings", priority = 1)]
        public static void ApplySettings()
        {
            PlayerSettings.WebGL.template = "PROJECT:IslandEscape";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            AssetDatabase.SaveAssets();
            Debug.Log("[Yandex] WebGL settings applied");
        }

        [MenuItem("Tools/Yandex Games/Build + Zip", priority = 2)]
        public static void Build()
        {
            ApplySettings();

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (Directory.Exists(OUTPUT)) Directory.Delete(OUTPUT, true);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions()
            {
                scenes = scenes,
                locationPathName = OUTPUT,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[Yandex] Build failed: " + report.summary.result + " errors " + report.summary.totalErrors);
                return;
            }

            if (File.Exists(ARCHIVE)) File.Delete(ARCHIVE);
            ZipFile.CreateFromDirectory(OUTPUT, ARCHIVE, System.IO.Compression.CompressionLevel.Optimal, false);

            long folderSize = 0;
            foreach (FileInfo file in new DirectoryInfo(OUTPUT).GetFiles("*", SearchOption.AllDirectories)) folderSize += file.Length;
            Debug.Log($"[Yandex] Build ok: {folderSize / 1048576f:0.0} MB in {OUTPUT}, archive {new FileInfo(ARCHIVE).Length / 1048576f:0.0} MB");
        }
    }
}
