using UnityEngine;

public class ChallengeComparisonManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComputeShader comparisonShader;
    [SerializeField] private ChallengeMapDefinition activeChallenge;

    [Header("Player Maps")]
    [SerializeField] private Texture playerLengthMap;
    [SerializeField] private Texture playerColorMap;

    [Header("Output")]
    [SerializeField] private RenderTexture mismatchOverlay;

    [Header("Null Detection")]
    [SerializeField] private float emptyLengthValue = 0f;
    [SerializeField] private Color emptyColorValue = Color.white;

    private float lastScore01;
    private string lastRank = "F";

    private ComputeBuffer groupScoreBuffer;
    private ComputeBuffer groupWeightBuffer;
    private ComputeBuffer changeFlagBuffer;
    private int compareKernel;

    public ChallengeMapDefinition ActiveChallenge => activeChallenge;
    public float LastScore01 => lastScore01;
    public string LastRank => lastRank;
    public RenderTexture MismatchOverlay => mismatchOverlay;
    public bool LastResultIsNone => lastRank == "None";

    private void Awake()
    {
        if (comparisonShader != null)
        {
            compareKernel = comparisonShader.FindKernel("CompareHairMaps");
        }
        else
        {
            Debug.LogWarning("ChallengeComparisonManager: comparison shader is not assigned.");
        }
    }

    private void Start()
    {
        HydrateFromSession();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    public void SetActiveChallenge(ChallengeMapDefinition challenge)
    {
        activeChallenge = challenge;
    }

    public void SetPlayerMaps(Texture lengthMap, Texture colorMap)
    {
        playerLengthMap = lengthMap;
        playerColorMap = colorMap;
    }

    public void HydrateFromSession()
    {
        if (HairChallengeSession.Instance == null)
        {
            return;
        }

        if (activeChallenge == null)
        {
            activeChallenge = HairChallengeSession.Instance.ActiveChallenge;
        }

        if (playerLengthMap == null || playerColorMap == null)
        {
            HairChallengeSession.Instance.TryGetComparisonData(
                out ChallengeMapDefinition sessionChallenge,
                out Texture sessionLengthMap,
                out Texture sessionColorMap);

            if (activeChallenge == null)
            {
                activeChallenge = sessionChallenge;
            }

            if (playerLengthMap == null)
            {
                playerLengthMap = sessionLengthMap;
            }

            if (playerColorMap == null)
            {
                playerColorMap = sessionColorMap;
            }
        }
    }

    [ContextMenu("Evaluate Challenge")]
    public void EvaluateChallenge()
    {
        Debug.Log("ChallengeComparisonManager: EvaluateChallenge started.");

        HydrateFromSession();

        if (comparisonShader == null)
        {
            Debug.LogError("Comparison shader is missing.");
            return;
        }

        if (activeChallenge == null)
        {
            Debug.LogError("No active challenge has been assigned.");
            return;
        }

        if (!activeChallenge.ValidateTargetSizes(out string challengeError))
        {
            Debug.LogError(challengeError);
            return;
        }

        if (playerLengthMap == null || playerColorMap == null)
        {
            Debug.LogError("Player LengthMap and ColorMap must both be assigned.");
            return;
        }

        LogCurrentMapSizes();

        EnsureMismatchOverlay(activeChallenge.targetLengthMap.width, activeChallenge.targetLengthMap.height);

        float tolerance = activeChallenge.GetDifficultyTolerance();

        comparisonShader.SetTexture(compareKernel, "_PlayerLengthMap", playerLengthMap);
        comparisonShader.SetTexture(compareKernel, "_PlayerColorMap", playerColorMap);
        comparisonShader.SetTexture(compareKernel, "_TargetLengthMap", activeChallenge.targetLengthMap);
        comparisonShader.SetTexture(compareKernel, "_TargetColorMap", activeChallenge.targetColorMap);
        comparisonShader.SetTexture(compareKernel, "_MismatchOverlay", mismatchOverlay);
        comparisonShader.SetInt("_Width", activeChallenge.targetLengthMap.width);
        comparisonShader.SetInt("_Height", activeChallenge.targetLengthMap.height);
        comparisonShader.SetInt("_GroupCountX", Mathf.CeilToInt(activeChallenge.targetLengthMap.width / 8.0f));
        comparisonShader.SetFloat("_LengthWeight", Mathf.Max(0f, activeChallenge.lengthWeight));
        comparisonShader.SetFloat("_ColorWeight", Mathf.Max(0f, activeChallenge.colorWeight));
        comparisonShader.SetFloat("_LengthTolerance", tolerance);
        comparisonShader.SetFloat("_ColorTolerance", tolerance);
        comparisonShader.SetFloat("_EmptyLengthValue", emptyLengthValue);
        comparisonShader.SetVector("_EmptyColorValue", emptyColorValue);

        int groupsX = Mathf.CeilToInt(activeChallenge.targetLengthMap.width / 8.0f);
        int groupsY = Mathf.CeilToInt(activeChallenge.targetLengthMap.height / 8.0f);
        int totalGroups = groupsX * groupsY;

        PrepareGroupBuffers(totalGroups);
        comparisonShader.SetBuffer(compareKernel, "_GroupScoreSums", groupScoreBuffer);
        comparisonShader.SetBuffer(compareKernel, "_GroupWeightSums", groupWeightBuffer);
        comparisonShader.SetBuffer(compareKernel, "_ChangeFlag", changeFlagBuffer);

        comparisonShader.Dispatch(compareKernel, groupsX, groupsY, 1);

        uint[] changeFlag = new uint[1];
        changeFlagBuffer.GetData(changeFlag);

        if (changeFlag[0] == 0u)
        {
            lastScore01 = 0f;
            lastRank = "None";
            Debug.Log("Challenge score: None");
            return;
        }

        uint[] scoreSums = new uint[totalGroups];
        uint[] weightSums = new uint[totalGroups];
        groupScoreBuffer.GetData(scoreSums);
        groupWeightBuffer.GetData(weightSums);

        ulong totalScore = 0;
        ulong totalWeight = 0;

        for (int i = 0; i < totalGroups; i++)
        {
            totalScore += scoreSums[i];
            totalWeight += weightSums[i];
        }

        if (totalWeight == 0)
        {
            lastScore01 = 0f;
            lastRank = "F";
            Debug.LogWarning("ChallengeComparisonManager: total comparison weight was zero.");
            return;
        }

        lastScore01 = Mathf.Clamp01((float)totalScore / (float)totalWeight / 1000f);
        lastRank = EvaluateRank(lastScore01);

        Debug.Log("Challenge score: " + (lastScore01 * 100f).ToString("0.00") + "% Rank: " + lastRank);
    }

    private void LogCurrentMapSizes()
    {
        string playerLength = playerLengthMap != null ? playerLengthMap.width + "x" + playerLengthMap.height : "null";
        string playerColor = playerColorMap != null ? playerColorMap.width + "x" + playerColorMap.height : "null";
        string targetLength = activeChallenge != null && activeChallenge.targetLengthMap != null ? activeChallenge.targetLengthMap.width + "x" + activeChallenge.targetLengthMap.height : "null";
        string targetColor = activeChallenge != null && activeChallenge.targetColorMap != null ? activeChallenge.targetColorMap.width + "x" + activeChallenge.targetColorMap.height : "null";

        Debug.Log("Player LengthMap: " + playerLength + " | Player ColorMap: " + playerColor);
        Debug.Log("Target LengthMap: " + targetLength + " | Target ColorMap: " + targetColor);
    }

    private string EvaluateRank(float score01)
    {
        if (activeChallenge == null)
        {
            return "F";
        }

        if (score01 >= activeChallenge.sRankThreshold) return "S";
        if (score01 >= activeChallenge.aRankThreshold) return "A";
        if (score01 >= activeChallenge.bRankThreshold) return "B";
        if (score01 >= activeChallenge.cRankThreshold) return "C";
        if (score01 >= activeChallenge.dRankThreshold) return "D";
        if (score01 >= activeChallenge.eRankThreshold) return "E";
        return "F";
    }

    private void EnsureMismatchOverlay(int width, int height)
    {
        if (mismatchOverlay != null && mismatchOverlay.width == width && mismatchOverlay.height == height)
        {
            return;
        }

        ReleaseMismatchOverlay();

        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0);
        descriptor.enableRandomWrite = true;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;

        mismatchOverlay = new RenderTexture(descriptor);
        mismatchOverlay.name = "HairChallenge_MismatchOverlay";
        mismatchOverlay.wrapMode = TextureWrapMode.Clamp;
        mismatchOverlay.filterMode = FilterMode.Bilinear;
        mismatchOverlay.Create();
    }

    private void PrepareGroupBuffers(int groupCount)
    {
        ReleaseGroupBuffers();

        groupScoreBuffer = new ComputeBuffer(groupCount, sizeof(uint), ComputeBufferType.Structured);
        groupWeightBuffer = new ComputeBuffer(groupCount, sizeof(uint), ComputeBufferType.Structured);
        changeFlagBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);

        groupScoreBuffer.SetData(new uint[groupCount]);
        groupWeightBuffer.SetData(new uint[groupCount]);
        changeFlagBuffer.SetData(new uint[] { 0u });
    }

    private void ReleaseBuffers()
    {
        ReleaseGroupBuffers();
        ReleaseMismatchOverlay();
    }

    private void ReleaseGroupBuffers()
    {
        if (groupScoreBuffer != null)
        {
            groupScoreBuffer.Release();
            groupScoreBuffer = null;
        }

        if (groupWeightBuffer != null)
        {
            groupWeightBuffer.Release();
            groupWeightBuffer = null;
        }

        if (changeFlagBuffer != null)
        {
            changeFlagBuffer.Release();
            changeFlagBuffer = null;
        }
    }

    private void ReleaseMismatchOverlay()
    {
        if (mismatchOverlay != null)
        {
            mismatchOverlay.Release();
            Destroy(mismatchOverlay);
            mismatchOverlay = null;
        }
    }
}