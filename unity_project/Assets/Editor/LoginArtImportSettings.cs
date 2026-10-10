#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IOSVN.TuTien.Editor
{
    internal sealed class LoginArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/Brand/LoginLandscapePixel.png") return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Bilinear;   // painted backdrop (was pixel art)
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
#endif
