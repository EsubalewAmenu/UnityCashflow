using UnityEditor;
using UnityEngine;

public class BoardArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/GameBoard/")) return;
        var importer=(TextureImporter)assetImporter;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=4096;
        importer.mipmapEnabled=false;
        importer.filterMode=FilterMode.Bilinear;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.alphaIsTransparency=true;
    }
}
