using UnityEngine;

public class ChallengeMapCustomAuthoring : MonoBehaviour
{
    [Header("Main Map Size Reference")]
    [SerializeField] private Texture mainLengthMapReference;
    [SerializeField] private Texture mainColorMapReference;

    [Header("Maps to Save")]
    [SerializeField] private Texture customLengthMap;
    [SerializeField] private Texture customColorMap;

    [Header("Naming")]
    [SerializeField] private string challengeRootFolder = "Assets/ChallengeMaps";
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
        string challengeFolderName = "CustomChallenge_" + challengeNumber.ToString();
        string fullFolder = ChallengeMapSaveUtility.BuildChallengeFolder(challengeRootFolder, challengeFolderName);

        ChallengeMapSaveUtility.EnsureProjectFolder(fullFolder);

        Texture2D lengthAsset = ChallengeMapSaveUtility.SaveTextureAsPng(
            customLengthMap,
            fullFolder + "/CustomChallengeLengthMap_" + challengeNumber.ToString() + ".png",
            true);

        Texture2D colorAsset = ChallengeMapSaveUtility.SaveTextureAsPng(
            customColorMap,
            fullFolder + "/CustomChallengeColorMap_" + challengeNumber.ToString() + ".png",
            false);

        SavedDefinition = ChallengeMapSaveUtility.CreateOrUpdateDefinitionAsset(
            fullFolder,
            challengeFolderName,
            ChallengeMapDefinition.ChallengeSource.Custom,
            lengthAsset,
            colorAsset,
            challengeNumber);

        Debug.Log("Saved custom challenge in folder: " + fullFolder);
#else
        Debug.LogWarning("Challenge saving is editor-only.");
#endif
    }

    private bool TryValidateSizes()
    {
        Texture reference = mainLengthMapReference != null ? mainLengthMapReference : mainColorMapReference;
        if (reference == null)
        {
            return true;
        }

        if (customLengthMap != null &&
            (customLengthMap.width != reference.width || customLengthMap.height != reference.height))
        {
            Debug.LogError("Custom LengthMap size does not match the main map reference size.");
            return false;
        }

        if (customColorMap != null &&
            (customColorMap.width != reference.width || customColorMap.height != reference.height))
        {
            Debug.LogError("Custom ColorMap size does not match the main map reference size.");
            return false;
        }

        if (mainLengthMapReference != null &&
            (mainLengthMapReference.width != reference.width || mainLengthMapReference.height != reference.height))
        {
            Debug.LogWarning("Custom authoring: main LengthMap reference size does not match.");
        }

        if (mainColorMapReference != null &&
            (mainColorMapReference.width != reference.width || mainColorMapReference.height != reference.height))
        {
            Debug.LogWarning("Custom authoring: main ColorMap reference size does not match.");
        }

        return true;
    }
}