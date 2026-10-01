#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IOSVN.TuTien.Editor
{
    /// <summary>
    /// Keeps the pixel art imported from the Telegram game lossless on device. The PNGs are
    /// 4x upscales of a 64x64 grid (HUD icons: native 32x32), so importing them at the native
    /// grid with point filtering, no mipmaps and no block compression reproduces every source
    /// pixel exactly instead of blurring it with ASTC artefacts.
    /// </summary>
    internal sealed class PixelArtImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/PixelArt/";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var size = assetPath.StartsWith(Root + "UI/") ? 32 : 64;
            importer.maxTextureSize = size;
            var settings = importer.GetDefaultPlatformTextureSettings();
            settings.maxTextureSize = size;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            // Bilinear sampling at an exact 4:1 ratio lands inside each replicated 4x4 block.
            settings.resizeAlgorithm = TextureResizeAlgorithm.Bilinear;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
#endif
