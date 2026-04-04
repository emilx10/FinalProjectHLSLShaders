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

    [Header("Result")]
    [SerializeField, Range(0f, 1f)] private float lastScore01;
    [SerializeField] private string lastRank = "F";

    private ComputeBuffer groupScoreBuffer;
    private int compareKernel;

    public ChallengeMapDefinition ActiveChallenge
    {
        get { return activeChallenge; }
    }

    public float LastScore01
    {
        get { return lastScore01; }
    }

    public string LastRank
    {
        get { return lastRank; }
    }

    public RenderTexture MismatchOverlay
    {
        get { return mismatchOverlay; }
    }

    private void Awake()
    {
        if (comparisonShader != null)
        {
            compareKernel = comparisonShader.FindKernel("CompareHairMaps");
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

        if (!ValidatePlayerAgainstChallenge())
        {
            return;
        }

        PrepareMismatchOverlay(activeChallenge.targetLengthMap.width, activeChallenge.targetLengthMap.height);

        float tolerance = activeChallenge.GetDifficultyTolerance();

        comparisonShader.SetTexture(compareKernel, "_PlayerLengthMap", playerLengthMap);
        comparisonShader.SetTexture(compareKernel, "_PlayerColorMap", playerColorMap);
        comparisonShader.SetTexture(compareKernel, "_TargetLengthMap", activeChallenge.targetLengthMap);
        comparisonShader.SetTexture(compareKernel, "_TargetColorMap", activeChallenge.targetColorMap);
        comparisonShader.SetTexture(compareKernel, "_MismatchOverlay", mismatchOverlay);
        comparisonShader.SetInt("_Width", activeChallenge.targetLengthMap.width);
        comparisonShader.SetInt("_Height", activeChallenge.targetLengthMap.height);
        comparisonShader.SetFloat("_LengthWeight", Mathf.Max(0f, activeChallenge.lengthWeight));
        comparisonShader.SetFloat("_ColorWeight", Mathf.Max(0f, activeChallenge.colorWeight));
        comparisonShader.SetFloat("_LengthTolerance", tolerance);
        comparisonShader.SetFloat("_ColorTolerance", tolerance);

        int groupsX = Mathf.CeilToInt(activeChallenge.targetLengthMap.width / 8.0f);
        int groupsY = Mathf.CeilToInt(activeChallenge.targetLengthMap.height / 8.0f);
        int totalGroups = groupsX * groupsY;

        PrepareGroupScoreBuffer(totalGroups);
        comparisonShader.SetBuffer(compareKernel, "_GroupScores", groupScoreBuffer);
        comparisonShader.SetInt("_GroupCountX", groupsX);
        comparisonShader.Dispatch(compareKernel, groupsX, groupsY, 1);

        uint[] scoreResult = new uint[totalGroups];
        groupScoreBuffer.GetData(scoreResult);

        ulong totalScore = 0;
        for (int i = 0; i < scoreResult.Length; i++)
        {
            totalScore += scoreResult[i];
        }

        float maxPossible = activeChallenge.targetLengthMap.width * activeChallenge.targetLengthMap.height * 1000.0f;
        lastScore01 = Mathf.Clamp01((float)totalScore / maxPossible);
        lastRank = EvaluateRank(lastScore01);

        Debug.Log("Challenge score: " + (lastScore01 * 100f).ToString("0.00") + "% Rank: " + lastRank);
    }

    private bool ValidatePlayerAgainstChallenge()
    {
        if (playerLengthMap.width != activeChallenge.targetLengthMap.width || playerLengthMap.height != activeChallenge.targetLengthMap.height)
        {
            Debug.LogError("Player LengthMap size does not match the challenge LengthMap size.");
            return false;
        }

        if (playerColorMap.width != activeChallenge.targetColorMap.width || playerColorMap.height != activeChallenge.targetColorMap.height)
        {
            Debug.LogError("Player ColorMap size does not match the challenge ColorMap size.");
            return false;
        }

        return true;
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

    private void PrepareMismatchOverlay(int width, int height)
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

    private void PrepareGroupScoreBuffer(int groupCount)
    {
        ReleaseGroupScoreBuffer();
        groupScoreBuffer = new ComputeBuffer(groupCount, sizeof(uint), ComputeBufferType.Structured);

        uint[] initial = new uint[groupCount];
        groupScoreBuffer.SetData(initial);
    }

    private void ReleaseBuffers()
    {
        ReleaseGroupScoreBuffer();
        ReleaseMismatchOverlay();
    }

    private void ReleaseGroupScoreBuffer()
    {
        if (groupScoreBuffer != null)
        {
            groupScoreBuffer.Release();
            groupScoreBuffer = null;
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