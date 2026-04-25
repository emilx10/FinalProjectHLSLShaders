using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrassToolPanel : MonoBehaviour
{
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

    [Header("Color and Utility")]
    [SerializeField] private ColorSwatch[] colorSwatches;
    [SerializeField] private Button resetGrassButton;
    [SerializeField] private Button clearPaintButton;

    [Header("Style")]
    [SerializeField] private Color selectedToolColor = new Color(0.25f, 0.63f, 0.36f, 1f);
    [SerializeField] private Color idleToolColor = new Color(0.18f, 0.2f, 0.19f, 1f);

    public PaintController PaintController => paintController;

    private void Awake()
    {
        if (paintController == null)
        {
            paintController = FindObjectOfType<PaintController>();
        }
    }

    private void OnEnable()
    {
        BindControls();
        SyncSlidersFromController();
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
        paintController = controller;
        SyncSlidersFromController();
        RefreshVisualState();
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

        BindSlider(radiusSlider, radiusValueText, paintController.SetBrushRadius);
        BindSlider(strengthSlider, strengthValueText, paintController.SetBrushStrength);
        BindSlider(falloffSlider, falloffValueText, paintController.SetBrushFalloff);

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

    private void BindSlider(Slider slider, TMP_Text valueText, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.AddListener(onValueChanged);
        slider.onValueChanged.AddListener(value =>
        {
            if (valueText != null)
            {
                valueText.text = value.ToString("0.00");
            }
        });
    }

    private void SyncSlidersFromController()
    {
        if (paintController == null)
        {
            return;
        }

        SetSliderValue(radiusSlider, radiusValueText, paintController.BrushRadius);
        SetSliderValue(strengthSlider, strengthValueText, paintController.BrushStrength);
        SetSliderValue(falloffSlider, falloffValueText, paintController.BrushFalloff);
    }

    private void SetSliderValue(Slider slider, TMP_Text valueText, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(Mathf.Clamp(value, slider.minValue, slider.maxValue));
        }

        if (valueText != null)
        {
            valueText.text = value.ToString("0.00");
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
