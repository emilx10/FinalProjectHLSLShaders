using UnityEngine;

public class HairChallengeSession : MonoBehaviour
{
    public static HairChallengeSession Instance { get; private set; }

    public ChallengeMapDefinition ActiveChallenge { get; private set; }

    public Texture PlayerLengthMap { get; private set; }
    public Texture PlayerColorMap { get; private set; }

    public Texture BaselineLengthMap { get; private set; }
    public Texture BaselineColorMap { get; private set; }

    private RenderTexture playerLengthCopy;
    private RenderTexture playerColorCopy;
    private RenderTexture baselineLengthCopy;
    private RenderTexture baselineColorCopy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        ClearAllCopies();
    }

    public void SetActiveChallenge(ChallengeMapDefinition challenge)
    {
        ActiveChallenge = challenge;
    }

    public void SetPlayerMaps(Texture lengthMap, Texture colorMap)
    {
        PlayerLengthMap = lengthMap;
        PlayerColorMap = colorMap;

        ReplacePlayerCopies(lengthMap, colorMap);
    }

    public void SetBaselineMaps(Texture lengthMap, Texture colorMap)
    {
        BaselineLengthMap = lengthMap;
        BaselineColorMap = colorMap;

        ReplaceBaselineCopies(lengthMap, colorMap);
    }

    public bool TryGetComparisonData(
        out ChallengeMapDefinition challenge,
        out Texture lengthMap,
        out Texture colorMap,
        out Texture baselineLengthMap,
        out Texture baselineColorMap)
    {
        challenge = ActiveChallenge;
        lengthMap = PlayerLengthMap;
        colorMap = PlayerColorMap;
        baselineLengthMap = BaselineLengthMap;
        baselineColorMap = BaselineColorMap;

        return challenge != null &&
               lengthMap != null &&
               colorMap != null &&
               baselineLengthMap != null &&
               baselineColorMap != null;
    }

    public void ClearPlayerMaps()
    {
        PlayerLengthMap = null;
        PlayerColorMap = null;

        ReleaseAndNull(ref playerLengthCopy);
        ReleaseAndNull(ref playerColorCopy);
    }

    public void ClearBaselineMaps()
    {
        BaselineLengthMap = null;
        BaselineColorMap = null;

        ReleaseAndNull(ref baselineLengthCopy);
        ReleaseAndNull(ref baselineColorCopy);
    }

    public void ClearAllCopies()
    {
        ClearPlayerMaps();
        ClearBaselineMaps();
    }

    private void ReplacePlayerCopies(Texture lengthMap, Texture colorMap)
    {
        ReleaseAndNull(ref playerLengthCopy);
        ReleaseAndNull(ref playerColorCopy);

        playerLengthCopy = HairChallengeTextureSnapshotUtility.CloneToRenderTexture(lengthMap, "PlayerLengthMap_Copy");
        playerColorCopy = HairChallengeTextureSnapshotUtility.CloneToRenderTexture(colorMap, "PlayerColorMap_Copy");
    }

    private void ReplaceBaselineCopies(Texture lengthMap, Texture colorMap)
    {
        ReleaseAndNull(ref baselineLengthCopy);
        ReleaseAndNull(ref baselineColorCopy);

        baselineLengthCopy = HairChallengeTextureSnapshotUtility.CloneToRenderTexture(lengthMap, "BaselineLengthMap_Copy");
        baselineColorCopy = HairChallengeTextureSnapshotUtility.CloneToRenderTexture(colorMap, "BaselineColorMap_Copy");
    }

    private void ReleaseAndNull(ref RenderTexture texture)
    {
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
            texture = null;
        }
    }
}