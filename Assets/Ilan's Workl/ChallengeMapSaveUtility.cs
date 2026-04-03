using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ChallengeMapSaveUtility
{
    public static Texture2D CopyTextureToTexture2D(Texture source, bool linear)
    {
        if (source == null)
        {
            return null;
        }

        int width = source.width;
        int height = source.height;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);

        RenderTexture previous = RenderTexture.active;
        RenderTexture sourceRT = source as RenderTexture;

        if (sourceRT != null)
        {
            RenderTexture.active = sourceRT;
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
        }
        else
        {
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.ReleaseTemporary(temporary);
        }

        result.Apply(false, false);
        RenderTexture.active = previous;
        return result;
    }

#if UNITY_EDITOR
    public static Texture2D SaveTextureAsPng(Texture source, string absoluteOrProjectPath, bool linear)
    {
        Texture2D texture = CopyTextureToTexture2D(source, linear);
        if (texture == null)
        {
            return null;
        }

        string path = ToAbsoluteOrProjectPath(absoluteOrProjectPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.Refresh();

        ConfigureImporter(path, linear);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ToProjectRelativePath(path));
    }

    public static ChallengeMapDefinition CreateOrUpdateDefinitionAsset(
        string assetFolder,
        string assetName,
        ChallengeMapDefinition.ChallengeSource source,
        Texture2D referenceImage,
        Material targetMaterial,
        Texture2D lengthMap,
        Texture2D colorMap,
        int challengeIndex)
    {
        string folderAbsolute = ToAbsoluteOrProjectPath(assetFolder);
        Directory.CreateDirectory(folderAbsolute);

        string assetPath = CombineProjectPath(assetFolder, assetName + ".asset");
        ChallengeMapDefinition definition = AssetDatabase.LoadAssetAtPath<ChallengeMapDefinition>(assetPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ChallengeMapDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.challengeName = assetName;
        definition.challengeId = assetName + "_" + challengeIndex.ToString();
        definition.source = source;
        definition.referenceImage = referenceImage;
        definition.targetMaterial = targetMaterial;
        definition.targetLengthMap = lengthMap;
        definition.targetColorMap = colorMap;

        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return definition;
    }
#endif

    private static string ToAbsoluteOrProjectPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        string normalized = path.Replace('\\', '/');
        if (!normalized.StartsWith("Assets"))
        {
            normalized = "Assets/" + normalized;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, normalized).Replace('/', Path.DirectorySeparatorChar);
    }

#if UNITY_EDITOR
    private static string ToProjectRelativePath(string absolutePath)
    {
        string normalized = absolutePath.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');

        if (normalized.StartsWith(dataPath))
        {
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        return absolutePath;
    }

    private static string CombineProjectPath(string folder, string fileName)
    {
        string cleanFolder = folder.Replace('\\', '/').TrimEnd('/');
        if (!cleanFolder.StartsWith("Assets"))
        {
            cleanFolder = "Assets/" + cleanFolder;
        }

        return cleanFolder + "/" + fileName;
    }

    private static void ConfigureImporter(string absolutePath, bool linear)
    {
        string projectRelativePath = ToProjectRelativePath(absolutePath);
        TextureImporter importer = AssetImporter.GetAtPath(projectRelativePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.sRGBTexture = !linear;
        importer.mipmapEnabled = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
#endif
}
