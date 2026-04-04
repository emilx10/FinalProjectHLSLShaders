using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChallengeComparisonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChallengeComparisonManager comparisonManager;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private Image scoreFillImage;
    [SerializeField] private RawImage mismatchOverlayImage;

    [ContextMenu("Refresh UI")]
    public void RefreshUI()
    {
        if (comparisonManager == null)
            return;

        if (scoreText != null)
            scoreText.text = (comparisonManager.LastScore01 * 100f).ToString("0.00") + "%";

        if (rankText != null)
            rankText.text = comparisonManager.LastRank;

        if (scoreFillImage != null)
            scoreFillImage.fillAmount = comparisonManager.LastScore01;

        if (mismatchOverlayImage != null)
            mismatchOverlayImage.texture = comparisonManager.MismatchOverlay;
    }

    private void Update()
    {
        RefreshUI();
    }
}