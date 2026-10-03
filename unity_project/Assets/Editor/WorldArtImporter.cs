#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IOSVN.TuTien.Editor
{
    internal sealed class WorldArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath == null) return;
            var worldArt = assetPath.Contains("/Resources/World/Art/");
            var battleArt = assetPath.Contains("/Resources/BattleMaps/");
            if (!worldArt && !battleArt) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = worldArt;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = battleArt ? 1024 : 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
        }
    }
}
#endif
