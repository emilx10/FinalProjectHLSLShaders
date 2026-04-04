using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ChallengeMapSaveUtility
{
    public static string BuildChallengeFolder(string baseFolder, string challengeFolderName)
    {
        string folder = (baseFolder ?? "Assets").Replace('\\', '/').TrimEnd('/');
        if (!folder.StartsWith("Assets"))
        {
            folder = "Assets/" + folder;
        }

        return folder + "/" + challengeFolderName;
    }

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

        if (source is RenderTexture sourceRT)
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
    public static Texture2D SaveTextureAsPng(Texture source, string projectRelativePath, bool linear)
    {
        Texture2D texture = CopyTextureToTexture2D(source, linear);
        if (texture == null)
        {
            return null;
        }

        string absolutePath = ToAbsolutePath(projectRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        AssetDatabase.Refresh();

        ConfigureImporter(projectRelativePath, linear);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(NormalizeProjectPath(projectRelativePath));
    }

    public static ChallengeMapDefinition CreateOrUpdateDefinitionAsset(
        string folderPath,
        string assetName,
        ChallengeMapDefinition.ChallengeSource source,
        Texture2D lengthMap,
        Texture2D colorMap,
        int challengeIndex,
        Texture2D referenceImage = null,
        Material targetMaterial = null)
    {
        EnsureProjectFolder(folderPath);

        string assetPath = NormalizeProjectPath(folderPath) + "/" + assetName + ".asset";
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

    public static void EnsureProjectFolder(string folderPath)
    {
        string normalized = NormalizeProjectPath(folderPath);
        if (AssetDatabase.IsValidFolder(normalized))
        {
            return;
        }

        string[] segments = normalized.Split('/');
        if (segments.Length <= 1)
        {
            return;
        }

        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }
#endif

    private static string ToAbsolutePath(string projectRelativePath)
    {
        string normalized = NormalizeProjectPath(projectRelativePath);
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, normalized).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string NormalizeProjectPath(string path)
    {
        string normalized = (path ?? string.Empty).Replace('\\', '/');
        if (!normalized.StartsWith("Assets"))
        {
            normalized = "Assets/" + normalized.TrimStart('/');
        }

        return normalized.TrimEnd('/');
    }

#if UNITY_EDITOR
    private static void ConfigureImporter(string projectRelativePath, bool linear)
    {
        TextureImporter importer = AssetImporter.GetAtPath(NormalizeProjectPath(projectRelativePath)) as TextureImporter;
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