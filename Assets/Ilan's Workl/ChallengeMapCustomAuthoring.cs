using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChallengeMapCustomAuthoring : MonoBehaviour
{
    [Header("Source Size Check")]
    [SerializeField] private Texture sourceLengthMap;
    [SerializeField] private Texture sourceColorMap;

    [Header("Maps to Save")]
    [SerializeField] private Texture customLengthMap;
    [SerializeField] private Texture customColorMap;
    [SerializeField] private Texture2D referenceImage;
    [SerializeField] private Material targetMaterial;

    [Header("Naming")]
    [SerializeField] private string challengeFolder = "Assets/ChallengeMaps";
    [SerializeField] private string challengeNamePrefix = "CustomChallenge";
    [SerializeField, Min(1)] private int challengeNumber = 1;

    [Header("Save")]
    [SerializeField] private bool saveCustomChallenge = true;

    public ChallengeMapDefinition SavedDefinition { get; private set; }

    [ContextMenu("Save Custom Challenge")]
    public void SaveCustomChallenge()
    {
        if (customLengthMap == null || customColorMap == null)
        {
            Debug.LogError("Custom authoring needs both a LengthMap and a ColorMap.");
            return;
        }

        if (!TryValidateSizes())
        {
            return;
        }

        if (!saveCustomChallenge)
        {
            Debug.Log("Custom challenge save is disabled. Nothing was written to disk.");
            return;
        }

#if UNITY_EDITOR
        string folder = challengeFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            folder = "Assets/ChallengeMaps";
        }

        string challengeBaseName = challengeNamePrefix + "_" + challengeNumber.ToString();

        Texture2D lengthAsset = ChallengeMapSaveUtility.SaveTextureAsPng(
            customLengthMap,
            Path.Combine(folder, "CustomChallengeLengthMap_" + challengeNumber.ToString() + ".png"),
            true);

        Texture2D colorAsset = ChallengeMapSaveUtility.SaveTextureAsPng(
            customColorMap,
            Path.Combine(folder, "CustomChallengeColorMap_" + challengeNumber.ToString() + ".png"),
            false);

        ChallengeMapDefinition definition = ChallengeMapSaveUtility.CreateOrUpdateDefinitionAsset(
            folder,
            challengeBaseName,
            ChallengeMapDefinition.ChallengeSource.Custom,
            referenceImage,
            targetMaterial,
            lengthAsset,
            colorAsset,
            challengeNumber);

        SavedDefinition = definition;
        Debug.Log("Saved custom challenge: " + challengeBaseName);
#else
        Debug.LogWarning("Challenge saving is editor-only.");
#endif
    }

    private bool TryValidateSizes()
    {
        Texture reference = sourceLengthMap != null ? sourceLengthMap : sourceColorMap;
        if (reference == null)
        {
            return true;
        }

        if (customLengthMap != null && (customLengthMap.width != reference.width || customLengthMap.height != reference.height))
        {
            Debug.LogError("Custom LengthMap size does not match the reference main map size.");
            return false;
        }

        if (customColorMap != null && (customColorMap.width != reference.width || customColorMap.height != reference.height))
        {
            Debug.LogError("Custom ColorMap size does not match the reference main map size.");
            return false;
        }

        if (sourceLengthMap != null && (sourceLengthMap.width != reference.width || sourceLengthMap.height != reference.height))
        {
            Debug.LogWarning("Custom authoring: source LengthMap size does not match the reference size.");
        }

        if (sourceColorMap != null && (sourceColorMap.width != reference.width || sourceColorMap.height != reference.height))
        {
            Debug.LogWarning("Custom authoring: source ColorMap size does not match the reference size.");
        }

        return true;
    }
}
