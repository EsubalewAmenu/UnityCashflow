using UnityEditor;
using UnityEngine;

public sealed class GameAudioImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if(!assetPath.StartsWith("Assets/Resources/Audio/"))return;
        var importer=(AudioImporter)assetImporter;
        // Assets are already mono; avoid Unity's downmix normalization.
        importer.forceToMono=false;importer.loadInBackground=false;
        var settings=importer.defaultSampleSettings;
        settings.preloadAudioData=true;
        settings.loadType=AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat=AudioCompressionFormat.PCM;
        importer.defaultSampleSettings=settings;
    }
}
