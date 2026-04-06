using UnityEngine;
using UnityEngine.SceneManagement;

public class HairChallengeTransitionController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string comparisonSceneName = "HairComparisonScene";

    [Header("Challenge Selection")]
    [SerializeField] private ChallengeMapLibrary challengeLibrary;
    [SerializeField] private bool pickRandomChallengeOnStart = true;
    [SerializeField] private bool pickRandomChallengeIfMissingOnFinish = true;

    [Header("Current Run")]
    [SerializeField] private ChallengeMapDefinition selectedChallenge;
    [SerializeField] private Texture playerLengthMap;
    [SerializeField] private Texture playerColorMap;

    [Header("Baseline")]
    [SerializeField] private Texture baselineLengthMap;
    [SerializeField] private Texture baselineColorMap;
    [SerializeField] private bool baselineCaptured;

    private void Start()
    {
        if (pickRandomChallengeOnStart)
        {
            PickRandomChallenge();
        }
        else
        {
            SyncSelectedChallengeToSession();
        }

        CaptureBaselineIfPossible();
    }

    public void SetSelectedChallenge(ChallengeMapDefinition challenge)
    {
        selectedChallenge = challenge;
        SyncSelectedChallengeToSession();
    }

    public void SetPlayerMaps(Texture lengthMap, Texture colorMap)
    {
        playerLengthMap = lengthMap;
        playerColorMap = colorMap;
        SyncPlayerMapsToSession();

        CaptureBaselineIfPossible();
    }

    public bool PickRandomChallenge()
    {
        if (challengeLibrary == null)
        {
            Debug.LogError("Challenge library is missing.");
            return false;
        }

        if (!challengeLibrary.TryGetRandomChallenge(selectedChallenge, out ChallengeMapDefinition challenge))
        {
            Debug.LogError("No different valid challenge could be found.");
            return false;
        }

        selectedChallenge = challenge;
        SyncSelectedChallengeToSession();

        Debug.Log("Picked random challenge: " + selectedChallenge.challengeName);
        return true;
    }

    public void FinishAndOpenComparison()
    {
        if (selectedChallenge == null && pickRandomChallengeIfMissingOnFinish)
        {
            if (!PickRandomChallenge())
            {
                return;
            }
        }

        if (selectedChallenge == null)
        {
            Debug.LogError("No challenge has been selected.");
            return;
        }

        if (playerLengthMap == null || playerColorMap == null)
        {
            Debug.LogError("Player LengthMap and ColorMap are missing.");
            return;
        }

        if (!baselineCaptured)
        {
            CaptureBaselineIfPossible();
        }

        if (!baselineCaptured || baselineLengthMap == null || baselineColorMap == null)
        {
            Debug.LogError("Baseline maps were not captured.");
            return;
        }

        if (HairChallengeSession.Instance == null)
        {
            Debug.LogError("HairChallengeSession is missing from the scene.");
            return;
        }

        HairChallengeSession.Instance.SetActiveChallenge(selectedChallenge);
        HairChallengeSession.Instance.SetBaselineMaps(baselineLengthMap, baselineColorMap);
        HairChallengeSession.Instance.SetPlayerMaps(playerLengthMap, playerColorMap);

        SceneManager.LoadScene(comparisonSceneName);
    }

    private void CaptureBaselineIfPossible()
    {
        if (baselineCaptured)
        {
            return;
        }

        if (playerLengthMap == null || playerColorMap == null)
        {
            return;
        }

        baselineLengthMap = HairChallengeTextureSnapshotUtility.CloneToRenderTexture(playerLengthMap, "BaselineLengthMap_Snapshot");
        baselineColorMap = HairChallengeTextureSnapshotUtility.CloneToRenderTexture(playerColorMap, "BaselineColorMap_Snapshot");
        baselineCaptured = baselineLengthMap != null && baselineColorMap != null;

        if (baselineCaptured && HairChallengeSession.Instance != null)
        {
            HairChallengeSession.Instance.SetBaselineMaps(baselineLengthMap, baselineColorMap);
        }
    }

    private void SyncSelectedChallengeToSession()
    {
        if (HairChallengeSession.Instance == null)
        {
            return;
        }

        if (selectedChallenge != null)
        {
            HairChallengeSession.Instance.SetActiveChallenge(selectedChallenge);
        }
    }

    private void SyncPlayerMapsToSession()
    {
        if (HairChallengeSession.Instance == null)
        {
            return;
        }

        if (playerLengthMap != null && playerColorMap != null)
        {
            HairChallengeSession.Instance.SetPlayerMaps(playerLengthMap, playerColorMap);
        }
    }
}