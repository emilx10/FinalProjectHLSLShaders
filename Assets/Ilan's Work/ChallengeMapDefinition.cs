using UnityEngine;

[CreateAssetMenu(menuName = "Hair Challenge/Challenge Map Definition", fileName = "ChallengeMapDefinition")]
public class ChallengeMapDefinition : ScriptableObject
{
    public enum ChallengeSource
    {
        Random,
        Custom
    }

    [Header("Identity")]
    public string challengeName = "Challenge 1";
    public string challengeId = "Challenge_1";
    public ChallengeSource source = ChallengeSource.Random;

    [Header("Target Data")]
    public Texture2D targetLengthMap;
    public Texture2D targetColorMap;

    [Header("Scoring")]
    [Range(0f, 1f)]
    public float lengthWeight = 0.5f;

    [Range(0f, 1f)]
    public float colorWeight = 0.5f;

    [Header("Difficulty")]
    public bool scaleDifficultyByTextureSize = true;

    [Tooltip("At or above this resolution, the comparison becomes strict.")]
    [Min(1)]
    public int highResolutionThreshold = 4096;

    [Tooltip("Tolerance used for high resolution targets.")]
    [Range(0.001f, 1f)]
    public float highResolutionTolerance = 0.05f;

    [Tooltip("Tolerance used for lower resolution targets.")]
    [Range(0.001f, 1f)]
    public float lowResolutionTolerance = 0.25f;

    [Header("Optional Rating Thresholds")]
    [Range(0f, 1f)] public float sRankThreshold = 0.95f;
    [Range(0f, 1f)] public float aRankThreshold = 0.85f;
    [Range(0f, 1f)] public float bRankThreshold = 0.70f;
    [Range(0f, 1f)] public float cRankThreshold = 0.50f;
    [Range(0f, 1f)] public float dRankThreshold = 0.40f;
    [Range(0f, 1f)] public float eRankThreshold = 0.20f;

    public bool ValidateTargetSizes(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (targetLengthMap == null)
        {
            errorMessage = "Target LengthMap is missing.";
            return false;
        }

        if (targetColorMap == null)
        {
            errorMessage = "Target ColorMap is missing.";
            return false;
        }

        if (targetLengthMap.width != targetColorMap.width || targetLengthMap.height != targetColorMap.height)
        {
            errorMessage = "Target LengthMap and ColorMap sizes do not match.";
            return false;
        }

        return true;
    }

    public float GetDifficultyTolerance()
    {
        if (!scaleDifficultyByTextureSize)
        {
            return highResolutionTolerance;
        }

        int largestSize = 0;

        if (targetLengthMap != null)
        {
            largestSize = Mathf.Max(largestSize, targetLengthMap.width, targetLengthMap.height);
        }

        if (targetColorMap != null)
        {
            largestSize = Mathf.Max(largestSize, targetColorMap.width, targetColorMap.height);
        }

        return largestSize >= highResolutionThreshold
            ? highResolutionTolerance
            : lowResolutionTolerance;
    }
}