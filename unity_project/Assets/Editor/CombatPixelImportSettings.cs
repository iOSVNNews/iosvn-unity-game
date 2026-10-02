#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IOSVN.TuTien.Editor
{
    /// <summary>Keep combat sprites crisp and readable for generated pose frames.</summary>
    internal sealed class CombatPixelImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/CombatPixel/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = assetPath.StartsWith(Root + "Monsters/") || assetPath.StartsWith(Root + "Characters/") ? 256 : 64;
            var settings = importer.GetDefaultPlatformTextureSettings();
            settings.maxTextureSize = importer.maxTextureSize;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
#endif
