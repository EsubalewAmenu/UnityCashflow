using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class KingdomBranding
{
    static KingdomBranding(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(!File.Exists("work/run-kingdom-branding")||EditorApplication.isCompiling)return;
        File.Delete("work/run-kingdom-branding");
        Apply();
    }
    public static void Apply(){
        foreach(string name in new[]{"kingdom-icon"}){
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Branding/"+name+".png");
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.isReadable=true;importer.SaveAndReimport();
        }
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Branding/kingdom-icon.png");
        PlayerSettings.productName="Cashflow Kingdom Mobile";
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon},IconKind.Any);
        int count=PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android,IconKind.Any).Length;
        if(count>0)PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android,Enumerable.Repeat(icon,count).ToArray(),IconKind.Any);
        foreach(var kind in new[]{UnityEditor.Android.AndroidPlatformIconKind.Legacy,UnityEditor.Android.AndroidPlatformIconKind.Round}){
            var icons=PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android,kind);
            foreach(var slot in icons)slot.SetTexture(icon);
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android,kind,icons);
        }
        Directory.CreateDirectory("work/play-store");
        Export(icon,512,512,"work/play-store/app-icon.png");
        AssetDatabase.SaveAssets();File.WriteAllText("work/play-store/branding-results.txt","PASS: finance icon assigned to default and Android legacy/round icons; Google Play icon exported as 512x512 RGBA PNG.");
    }
    // Platform-required sizing only; the supplied artwork is not redrawn.
    static void Export(Texture2D source,int width,int height,string path){
        var output=new Texture2D(width,height,TextureFormat.RGBA32,false);
        float ratio=width/(float)height,sourceRatio=source.width/(float)source.height;
        float sx=Mathf.Min(1,ratio/sourceRatio),sy=Mathf.Min(1,sourceRatio/ratio);
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)output.SetPixel(x,y,source.GetPixelBilinear((1-sx)/2+(x+.5f)/width*sx,(1-sy)/2+(y+.5f)/height*sy));
        output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());Object.DestroyImmediate(output);
    }
}
