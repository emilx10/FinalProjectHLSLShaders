using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChallengeComparisonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChallengeComparisonManager comparisonManager;
    [SerializeField] private TMP_Text challengeNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private Image scoreFillImage;
    [SerializeField] private RawImage targetLengthPreview;
    [SerializeField] private RawImage targetColorPreview;
    [SerializeField] private RawImage mismatchOverlayImage;

    [ContextMenu("Refresh UI")]
    public void RefreshUI()
    {
        if (comparisonManager == null)
        {
            return;
        }

        ChallengeMapDefinition challenge = comparisonManager.ActiveChallenge;

        if (challengeNameText != null)
        {
            challengeNameText.text = challenge != null ? challenge.challengeName : "-";
        }

        if (comparisonManager.LastResultIsNone)
        {
            if (scoreText != null)
            {
                scoreText.text = "None";
            }

            if (rankText != null)
            {
                rankText.text = "None";
            }

            if (scoreFillImage != null)
            {
                scoreFillImage.fillAmount = 0f;
            }
        }
        else
        {
            if (scoreText != null)
            {
                scoreText.text = (comparisonManager.LastScore01 * 100f).ToString("0.00") + "%";
            }

            if (rankText != null)
            {
                rankText.text = comparisonManager.LastRank;
            }

            if (scoreFillImage != null)
            {
                scoreFillImage.fillAmount = comparisonManager.LastScore01;
            }
        }

        if (targetLengthPreview != null)
        {
            targetLengthPreview.texture = challenge != null ? challenge.targetLengthMap : null;
        }

        if (targetColorPreview != null)
        {
            targetColorPreview.texture = challenge != null ? challenge.targetColorMap : null;
        }

        if (mismatchOverlayImage != null)
        {
            mismatchOverlayImage.texture = comparisonManager.MismatchOverlay;
        }
    }

    private void Update()
    {
        RefreshUI();
    }
}