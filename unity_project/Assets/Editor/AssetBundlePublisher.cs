#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace IOSVN.TuTien.Editor
{
    /// <summary>Builds iOS AssetBundles and the SHA-256 manifest consumed by the startup patcher.</summary>
    public static class AssetBundlePublisher
    {
        [Serializable]
        private sealed class ManifestFile
        {
            public int version;
            public long totalBytes;
            public List<ManifestBundle> bundles = new List<ManifestBundle>();
        }

        [Serializable]
        private sealed class ManifestBundle
        {
            public string bundleName;
            public string sha256;
            public long size;
            public bool isRequired;
        }

        [MenuItem("iOSVN/Assets/Build iOS downloadable bundles")]
        public static void BuildIosBundles()
        {
            var bundleNames = AssetDatabase.GetAllAssetBundleNames();
            if (bundleNames.Length == 0)
                throw new InvalidOperationException("Assign AssetBundle names to assets in the Inspector before publishing downloadable content.");

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new InvalidOperationException("Could not resolve the Unity project directory.");
            var outputPath = Path.GetFullPath(Path.Combine(projectRoot, "../build/AssetBundles/iOS"));
            Directory.CreateDirectory(outputPath);

            var built = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.iOS);
            if (built == null) throw new InvalidOperationException("Unity failed to build iOS AssetBundles.");

            var result = new ManifestFile { version = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            foreach (var name in bundleNames)
            {
                if (!IsSafeBundleName(name))
                    throw new InvalidOperationException("AssetBundle names must be flat filenames with letters, digits, dot, dash, or underscore: " + name);
                var path = Path.Combine(outputPath, name);
                if (!File.Exists(path)) throw new FileNotFoundException("Unity did not create an AssetBundle output.", path);
                var fileInfo = new FileInfo(path);
                result.totalBytes += fileInfo.Length;
                result.bundles.Add(new ManifestBundle
                {
                    bundleName = name,
                    sha256 = ComputeSha256(path),
                    size = fileInfo.Length,
                    isRequired = true,
                });
            }

            var manifestPath = Path.Combine(outputPath, "version_manifest.json");
            File.WriteAllText(manifestPath, JsonUtility.ToJson(result, true), new UTF8Encoding(false));
            Debug.Log($"Built {result.bundles.Count} iOS AssetBundles ({result.totalBytes / (1024f * 1024f):F1} MB) at {outputPath}. Upload this directory to the configured HTTPS CDN.");
            EditorUtility.RevealInFinder(outputPath);
        }

        private static bool IsSafeBundleName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "." || value == ".." || value.Length > 128) return false;
            foreach (var character in value)
                if (!(char.IsLetterOrDigit(character) || character == '.' || character == '-' || character == '_')) return false;
            return true;
        }

        private static string ComputeSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                var hash = sha.ComputeHash(stream);
                var result = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) result.Append(value.ToString("x2"));
                return result.ToString();
            }
        }
    }
}
#endif
