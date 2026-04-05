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
        challenge = null;

        if (challenges == null || challenges.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < challenges.Count; i++)
        {
            ChallengeMapDefinition candidate = challenges[Random.Range(0, challenges.Count)];
            if (candidate != null && candidate.targetLengthMap != null && candidate.targetColorMap != null)
            {
                challenge = candidate;
                return true;
            }
        }

        return false;
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