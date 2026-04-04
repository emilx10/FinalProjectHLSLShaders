using UnityEngine;

public class HairChallengeSession : MonoBehaviour
{
    public static HairChallengeSession Instance { get; private set; }

    public ChallengeMapDefinition ActiveChallenge { get; private set; }
    public Texture PlayerLengthMap { get; private set; }
    public Texture PlayerColorMap { get; private set; }

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

    public void SetActiveChallenge(ChallengeMapDefinition challenge)
    {
        ActiveChallenge = challenge;
    }

    public void SetPlayerMaps(Texture lengthMap, Texture colorMap)
    {
        PlayerLengthMap = lengthMap;
        PlayerColorMap = colorMap;
    }

    public bool TryGetComparisonData(
        out ChallengeMapDefinition challenge,
        out Texture lengthMap,
        out Texture colorMap)
    {
        challenge = ActiveChallenge;
        lengthMap = PlayerLengthMap;
        colorMap = PlayerColorMap;

        return challenge != null && lengthMap != null && colorMap != null;
    }

    public void ClearPlayerMaps()
    {
        PlayerLengthMap = null;
        PlayerColorMap = null;
    }
}