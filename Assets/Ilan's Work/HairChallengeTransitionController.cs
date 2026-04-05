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
    }

    public bool PickRandomChallenge()
    {
        if (challengeLibrary == null)
        {
            Debug.LogError("Challenge library is missing.");
            return false;
        }

        if (!challengeLibrary.TryGetRandomChallenge(out ChallengeMapDefinition challenge))
        {
            Debug.LogError("Challenge library does not contain any valid challenges.");
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

        if (HairChallengeSession.Instance == null)
        {
            Debug.LogError("HairChallengeSession is missing from the scene.");
            return;
        }

        HairChallengeSession.Instance.SetActiveChallenge(selectedChallenge);
        HairChallengeSession.Instance.SetPlayerMaps(playerLengthMap, playerColorMap);

        SceneManager.LoadScene(comparisonSceneName);
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