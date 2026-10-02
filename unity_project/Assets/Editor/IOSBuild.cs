#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using IOSVN.TuTien.Core;

namespace IOSVN.TuTien.Editor
{
    /// <summary>Creates a deterministic iOS Xcode project in GitHub Actions.</summary>
    public static class IOSBuild
    {
        public static void Build() => BuildTo(null);

        /// <summary>Editor menu entry for local exports; CI keeps using Build() with command-line arguments.</summary>
        [MenuItem("iOSVN/Build/Export iOS Xcode project to build/iOS-local")]
        private static void BuildFromMenu() => BuildTo("../build/iOS-local");

        private static void BuildTo(string outputOverride)
        {
            OnlinePrototypeProjectSetup.EnsurePrototypeScene();
            var bundleId = Argument("-iosBundleId", "com.iosvn.tutiengioi");
            var outputPath = outputOverride ?? Argument("-iosBuildPath", "../build/iOS");
            if (!IsValidBundleId(bundleId)) throw new BuildFailedException("IOS_BUNDLE_ID must be a reverse-DNS identifier, for example com.studio.game.");

            PlayerSettings.companyName = "iOSVN";
            PlayerSettings.productName = "Tu Tiên Giới";
            PlayerSettings.iOS.applicationDisplayName = "Tu Tiên Giới";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundleId);
            // "Faster (smaller) builds": far less generated C++ so IL2CPP does not run out of memory on 16 GB machines.
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, Il2CppCodeGeneration.OptimizeSize);
            var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Brand/TuTienGioi_AppIcon.png");
            if (appIcon == null) throw new BuildFailedException("Missing iOS app icon at Assets/Resources/Brand/TuTienGioi_AppIcon.png.");
            var iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, IconKind.Application);
            if (iconSizes.Length == 0) throw new BuildFailedException("Unity did not expose iOS application icon slots.");
            var appIcons = new Texture2D[iconSizes.Length];
            for (var i = 0; i < appIcons.Length; i++) appIcons[i] = appIcon;
            PlayerSettings.SetIcons(NamedBuildTarget.iOS, appIcons, IconKind.Application);
            var configuredUrl = Argument("-ipaServerUrl", null);
            var serverConfig = AssetDatabase.LoadAssetAtPath<GameServerConfig>("Assets/Resources/GameServerConfig.asset");
            if (serverConfig != null && configuredUrl != null)
            {
                serverConfig.apiBaseUrl = string.IsNullOrWhiteSpace(configuredUrl) ? GameServerConfig.DefaultApiBaseUrl : configuredUrl.Trim().TrimEnd('/');
                var assetCdnUrl = Argument("-assetCdnUrl", null);
                if (assetCdnUrl != null) serverConfig.assetCdnBaseUrl = assetCdnUrl.Trim().TrimEnd('/');
                EditorUtility.SetDirty(serverConfig);
                AssetDatabase.SaveAssets();
            }
            var scenes = Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
            if (scenes.Length == 0) throw new BuildFailedException("No Unity scenes are enabled for the iOS build.");

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new BuildFailedException("Could not resolve the Unity project directory.");
            var resolvedOutputPath = Path.GetFullPath(Path.Combine(projectRoot, outputPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = resolvedOutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"iOS export failed: {report.summary.result} ({report.summary.totalErrors} errors).");
        }

        private static string Argument(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1].StartsWith("-", StringComparison.Ordinal) ? fallback : args[i + 1];
            return fallback;
        }

        private static bool IsValidBundleId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 255) return false;
            foreach (var part in value.Split('.'))
            {
                if (part.Length == 0 || !(char.IsLetter(part[0]) || part[0] == '_')) return false;
                foreach (var c in part) if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-')) return false;
            }
            return value.Contains(".");
        }
    }
}
#endif
