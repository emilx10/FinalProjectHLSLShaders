using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Hair Challenge/Challenge Map Library", fileName = "ChallengeMapLibrary")]
public class ChallengeMapLibrary : ScriptableObject
{
    [SerializeField] private List<ChallengeMapDefinition> challenges = new List<ChallengeMapDefinition>();

#if UNITY_EDITOR
    [SerializeField] private string challengeFolder = "Assets/ChallengeMaps";
#endif

    public IReadOnlyList<ChallengeMapDefinition> Challenges => challenges;
    public int Count => challenges != null ? challenges.Count : 0;

    public bool TryGetRandomChallenge(out ChallengeMapDefinition challenge)
    {
        return TryGetRandomChallenge(null, out challenge);
    }

    public bool TryGetRandomChallenge(ChallengeMapDefinition excludeChallenge, out ChallengeMapDefinition challenge)
    {
        challenge = null;

        if (challenges == null || challenges.Count == 0)
        {
            return false;
        }

        List<ChallengeMapDefinition> candidates = new List<ChallengeMapDefinition>();

        for (int i = 0; i < challenges.Count; i++)
        {
            ChallengeMapDefinition candidate = challenges[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate == excludeChallenge)
            {
                continue;
            }

            if (candidate.targetLengthMap == null || candidate.targetColorMap == null)
            {
                continue;
            }

            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        challenge = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh From Folder")]
    public void RefreshFromFolder()
    {
        challenges.Clear();

        string folder = string.IsNullOrWhiteSpace(challengeFolder) ? "Assets/ChallengeMaps" : challengeFolder;
        string[] guids = AssetDatabase.FindAssets("t:ChallengeMapDefinition", new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ChallengeMapDefinition definition = AssetDatabase.LoadAssetAtPath<ChallengeMapDefinition>(path);

            if (definition != null && !challenges.Contains(definition))
            {
                challenges.Add(definition);
            }
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}