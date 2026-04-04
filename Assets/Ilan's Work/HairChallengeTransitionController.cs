using UnityEngine;
using UnityEngine.SceneManagement;

public class HairChallengeTransitionController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string comparisonSceneName = "HairComparisonScene";

    [Header("Current Run")]
    [SerializeField] private ChallengeMapDefinition selectedChallenge;
    [SerializeField] private Texture playerLengthMap;
    [SerializeField] private Texture playerColorMap;

    public void SetSelectedChallenge(ChallengeMapDefinition challenge)
    {
        selectedChallenge = challenge;
    }

    public void SetPlayerMaps(Texture lengthMap, Texture colorMap)
    {
        playerLengthMap = lengthMap;
        playerColorMap = colorMap;
    }

    public void FinishAndOpenComparison()
    {
        if (HairChallengeSession.Instance == null)
        {
            Debug.LogError("HairChallengeSession is missing from the scene.");
            return;
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

        HairChallengeSession.Instance.SetActiveChallenge(selectedChallenge);
        HairChallengeSession.Instance.SetPlayerMaps(playerLengthMap, playerColorMap);

        SceneManager.LoadScene(comparisonSceneName);
    }
}