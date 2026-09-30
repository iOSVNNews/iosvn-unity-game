using UnityEditor;
using UnityEngine;

public sealed class GameAudioImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.68f;
        settings.loadType = assetPath.Contains("/Music/") ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
        importer.defaultSampleSettings = settings;
    }
}
