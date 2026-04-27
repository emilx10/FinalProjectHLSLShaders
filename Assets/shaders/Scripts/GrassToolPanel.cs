using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrassToolPanel : MonoBehaviour
{
    [System.Serializable]
    public struct BrushSliderRange
    {
        [Min(0f)] public float valueAtZero;
        [Min(0f)] public float valueAtOne;

        public BrushSliderRange(float valueAtZero, float valueAtOne)
        {
            this.valueAtZero = valueAtZero;
            this.valueAtOne = valueAtOne;
        }

        public float Evaluate(float normalizedValue)
        {
            return Mathf.Lerp(valueAtZero, valueAtOne, Mathf.Clamp01(normalizedValue));
        }

        public float InverseEvaluate(float actualValue)
        {
            if (Mathf.Approximately(valueAtZero, valueAtOne))
            {
                return 0f;
            }

            return Mathf.Clamp01((actualValue - valueAtZero) / (valueAtOne - valueAtZero));
        }
    }

    [System.Serializable]
    public struct ColorSwatch
    {
        public Button button;
        public Image image;
        public Color color;
    }

    [Header("References")]
    [SerializeField] private PaintController paintController;

    [Header("Tool Controls")]
    [SerializeField] private Button growButton;
    [SerializeField] private Button cutButton;
    [SerializeField] private Button paintButton;
    [SerializeField] private TMP_Text activeToolText;

    [Header("Brush Sliders")]
    [SerializeField] private Slider radiusSlider;
    [SerializeField] private TMP_Text radiusValueText;
    [SerializeField] private Slider strengthSlider;
    [SerializeField] private TMP_Text strengthValueText;
    [SerializeField] private Slider falloffSlider;
    [SerializeField] private TMP_Text falloffValueText;

    [Header("Brush Slider Mapping")]
    [SerializeField, Range(0f, 1f)] private float startupSliderValue = 0.5f;
    [SerializeField] private BrushSliderRange radiusRange = new BrushSliderRange(0.005f, 0.18f);
    [SerializeField] private BrushSliderRange strengthRange = new BrushSliderRange(0.01f, 0.25f);
    [SerializeField] private BrushSliderRange falloffRange = new BrushSliderRange(0f, 1f);

    [Header("Color and Utility")]
    [SerializeField] private ColorSwatch[] colorSwatches;
    [SerializeField] private Button solidPaintButton;
    [SerializeField] private Button stripePaintButton;
    [SerializeField] private Button ombrePaintButton;
    [SerializeField] private Button resetGrassButton;
    [SerializeField] private Button clearPaintButton;

    [Header("Style")]
    [SerializeField] private Color selectedToolColor = new Color(0.25f, 0.63f, 0.36f, 1f);
    [SerializeField] private Color idleToolColor = new Color(0.18f, 0.2f, 0.19f, 1f);

    public PaintController PaintController => paintController;

    private bool hasAppliedStartupSliderValues;

    private void Awake()
    {
        if (paintController == null)
        {
            paintController = FindFirstObjectByType<PaintController>();
        }

        EnsureChallengeController();
    }

    private void OnEnable()
    {
        ConfigureNormalizedSliders();

        if (Application.isPlaying && !hasAppliedStartupSliderValues)
        {
            ApplyStartupSliderValues();
            hasAppliedStartupSliderValues = true;
        }
        else
        {
            SyncSlidersFromController();
        }

        BindControls();
        RefreshVisualState();
    }

    private void OnDisable()
    {
        UnbindControls();
    }

    private void Update()
    {
        RefreshVisualState();
    }

    public void SetPaintController(PaintController controller)
    {
        bool shouldRebind = isActiveAndEnabled;
        if (shouldRebind)
        {
            UnbindControls();
        }

        paintController = controller;
        EnsureChallengeController();
        ConfigureNormalizedSliders();

        if (Application.isPlaying && shouldRebind && !hasAppliedStartupSliderValues)
        {
            ApplyStartupSliderValues();
            hasAppliedStartupSliderValues = true;
        }
        else
        {
            SyncSlidersFromController();
        }

        if (shouldRebind)
        {
            BindControls();
        }

        RefreshVisualState();
    }

    private void EnsureChallengeController()
    {
        if (paintController == null)
        {
            return;
        }

        FinalProjectChallengeController challengeController = FindFirstObjectByType<FinalProjectChallengeController>();
        if (challengeController == null)
        {
            challengeController = paintController.gameObject.AddComponent<FinalProjectChallengeController>();
        }

        challengeController.SetPaintController(paintController);
    }

    private void BindControls()
    {
        if (paintController == null)
        {
            return;
        }

        if (growButton != null)
        {
            growButton.onClick.AddListener(paintController.SetToolGrow);
        }

        if (cutButton != null)
        {
            cutButton.onClick.AddListener(paintController.SetToolCut);
        }

        if (paintButton != null)
        {
            paintButton.onClick.AddListener(paintController.SetToolColor);
        }

        BindSlider(radiusSlider, radiusValueText, radiusRange, paintController.SetBrushRadius);
        BindSlider(strengthSlider, strengthValueText, strengthRange, paintController.SetBrushStrength);
        BindSlider(falloffSlider, falloffValueText, falloffRange, paintController.SetBrushFalloff);

        if (solidPaintButton != null)
        {
            solidPaintButton.onClick.AddListener(() =>
            {
                paintController.SetPaintModeSolid();
                paintController.SetToolColor();
                RefreshVisualState();
            });
        }

        if (stripePaintButton != null)
        {
            stripePaintButton.onClick.AddListener(() =>
            {
                paintController.SetPaintModeStripes();
                paintController.SetToolColor();
                RefreshVisualState();
            });
        }

        if (ombrePaintButton != null)
        {
            ombrePaintButton.onClick.AddListener(() =>
            {
                paintController.SetPaintModeOmbre();
                paintController.SetToolColor();
                RefreshVisualState();
            });
        }

        if (colorSwatches != null)
        {
            for (int i = 0; i < colorSwatches.Length; i++)
            {
                ColorSwatch swatch = colorSwatches[i];
                if (swatch.image != null)
                {
                    swatch.image.color = swatch.color;
                }

                if (swatch.button != null)
                {
                    Color selectedColor = swatch.color;
                    swatch.button.onClick.AddListener(() =>
                    {
                        paintController.SetBrushColor(selectedColor);
                        paintController.SetToolColor();
                        RefreshVisualState();
                    });
                }
            }
        }

        if (resetGrassButton != null)
        {
            resetGrassButton.onClick.AddListener(paintController.ClearLengthMapToBlack);
        }

        if (clearPaintButton != null)
        {
            clearPaintButton.onClick.AddListener(paintController.ClearColorMapToWhite);
        }
    }

    private void UnbindControls()
    {
        if (growButton != null) growButton.onClick.RemoveAllListeners();
        if (cutButton != null) cutButton.onClick.RemoveAllListeners();
        if (paintButton != null) paintButton.onClick.RemoveAllListeners();
        if (radiusSlider != null) radiusSlider.onValueChanged.RemoveAllListeners();
        if (strengthSlider != null) strengthSlider.onValueChanged.RemoveAllListeners();
        if (falloffSlider != null) falloffSlider.onValueChanged.RemoveAllListeners();
        if (solidPaintButton != null) solidPaintButton.onClick.RemoveAllListeners();
        if (stripePaintButton != null) stripePaintButton.onClick.RemoveAllListeners();
        if (ombrePaintButton != null) ombrePaintButton.onClick.RemoveAllListeners();
        if (resetGrassButton != null) resetGrassButton.onClick.RemoveAllListeners();
        if (clearPaintButton != null) clearPaintButton.onClick.RemoveAllListeners();

        if (colorSwatches == null)
        {
            return;
        }

        for (int i = 0; i < colorSwatches.Length; i++)
        {
            if (colorSwatches[i].button != null)
            {
                colorSwatches[i].button.onClick.RemoveAllListeners();
            }
        }
    }

    private void BindSlider(Slider slider, TMP_Text valueText, BrushSliderRange range, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.AddListener(normalizedValue =>
        {
            onValueChanged(range.Evaluate(normalizedValue));

            if (valueText != null)
            {
                valueText.text = normalizedValue.ToString("0.00");
            }
        });
    }

    private void ConfigureNormalizedSliders()
    {
        ConfigureNormalizedSlider(radiusSlider);
        ConfigureNormalizedSlider(strengthSlider);
        ConfigureNormalizedSlider(falloffSlider);
    }

    private void ConfigureNormalizedSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void ApplyStartupSliderValues()
    {
        if (paintController == null)
        {
            return;
        }

        float normalizedValue = Mathf.Clamp01(startupSliderValue);
        SetMappedSliderValue(radiusSlider, radiusValueText, normalizedValue, radiusRange, paintController.SetBrushRadius);
        SetMappedSliderValue(strengthSlider, strengthValueText, normalizedValue, strengthRange, paintController.SetBrushStrength);
        SetMappedSliderValue(falloffSlider, falloffValueText, normalizedValue, falloffRange, paintController.SetBrushFalloff);
    }

    private void SyncSlidersFromController()
    {
        if (paintController == null)
        {
            return;
        }

        SetSliderValue(radiusSlider, radiusValueText, radiusRange.InverseEvaluate(paintController.BrushRadius));
        SetSliderValue(strengthSlider, strengthValueText, strengthRange.InverseEvaluate(paintController.BrushStrength));
        SetSliderValue(falloffSlider, falloffValueText, falloffRange.InverseEvaluate(paintController.BrushFalloff));
    }

    private void SetMappedSliderValue(Slider slider, TMP_Text valueText, float normalizedValue, BrushSliderRange range, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        SetSliderValue(slider, valueText, normalizedValue);
        onValueChanged(range.Evaluate(normalizedValue));
    }

    private void SetSliderValue(Slider slider, TMP_Text valueText, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        if (valueText != null)
        {
            valueText.text = Mathf.Clamp01(value).ToString("0.00");
        }
    }

    private void RefreshVisualState()
    {
        if (paintController == null)
        {
            return;
        }

        switch (paintController.CurrentTool)
        {
            case PaintController.ToolMode.Grow:
                SetActiveToolText("Grow Grass");
                break;
            case PaintController.ToolMode.Cut:
                SetActiveToolText("Cut Grass");
                break;
            case PaintController.ToolMode.Color:
                SetActiveToolText("Paint Grass");
                break;
        }

        SetButtonSelected(growButton, paintController.CurrentTool == PaintController.ToolMode.Grow);
        SetButtonSelected(cutButton, paintController.CurrentTool == PaintController.ToolMode.Cut);
        SetButtonSelected(paintButton, paintController.CurrentTool == PaintController.ToolMode.Color);
        SetButtonSelected(solidPaintButton, paintController.CurrentPaintBlendMode == PaintController.PaintBlendMode.Solid);
        SetButtonSelected(stripePaintButton, paintController.CurrentPaintBlendMode == PaintController.PaintBlendMode.Stripes);
        SetButtonSelected(ombrePaintButton, paintController.CurrentPaintBlendMode == PaintController.PaintBlendMode.Ombre);
    }

    private void SetActiveToolText(string value)
    {
        if (activeToolText != null)
        {
            activeToolText.text = value;
        }
    }

    private void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected ? selectedToolColor : idleToolColor;
        }
    }
}
