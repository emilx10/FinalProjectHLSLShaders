using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ControlsHelpPanel : MonoBehaviour
{
    private const string PanelName = "Controls Help Panel";

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(330f, 210f);
    [SerializeField] private Vector2 panelOffset = new Vector2(24f, 24f);

    [Header("Content")]
    [SerializeField] private string title = "Controls";
    [SerializeField, TextArea(5, 10)] private string controlsText =
        "WASD / Arrows - Pan camera\n" +
        "Right Mouse + Move - Rotate view\n" +
        "Middle Mouse + Drag - Pan view\n" +
        "Mouse Wheel - Zoom in / out\n" +
        "Left Mouse - Paint with active tool\n" +
        "1 / 2 / 3 - Grow / Cut / Paint\n" +
        "Tool Panel - Brush, color, style, reset";

    [Header("Style")]
    [SerializeField] private Color backgroundColor = new Color(0.06f, 0.07f, 0.065f, 0.88f);
    [SerializeField] private Color titleColor = new Color(0.93f, 0.96f, 0.91f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.78f, 0.84f, 0.78f, 1f);
    [SerializeField] private int titleFontSize = 22;
    [SerializeField] private int bodyFontSize = 15;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreatePanel();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreatePanel();
    }

    private static void TryCreatePanel()
    {
        if (FindObjectOfType<ControlsHelpPanel>() != null)
        {
            return;
        }

        if (FindObjectOfType<PaintController>() == null && FindObjectOfType<OrbitCameraController>() == null)
        {
            return;
        }

        Canvas canvas = FindTargetCanvas();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject panelObject = new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ControlsHelpPanel));
        panelObject.layer = 5;
        panelObject.transform.SetParent(canvas.transform, false);
    }

    private static Canvas FindTargetCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == "UI Canvas")
            {
                return canvases[i];
            }
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private void Awake()
    {
        BuildPanel();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        BuildPanel();
    }

    private void BuildPanel()
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = panelOffset;
        rect.sizeDelta = panelSize;

        Image image = GetComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        TMP_Text titleText = EnsureText("Title", title, titleFontSize, FontStyles.Bold, titleColor);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(16f, -44f);
        titleRect.offsetMax = new Vector2(-16f, -14f);
        titleText.font = font != null ? font : titleText.font;

        TMP_Text bodyText = EnsureText("Body", controlsText, bodyFontSize, FontStyles.Normal, bodyColor);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0f, 1f);
        bodyRect.offsetMin = new Vector2(16f, 14f);
        bodyRect.offsetMax = new Vector2(-16f, -52f);
        bodyText.font = font != null ? font : bodyText.font;
    }

    private TMP_Text EnsureText(string objectName, string value, int fontSize, FontStyles style, Color color)
    {
        Transform existing = transform.Find(objectName);
        TextMeshProUGUI text = existing != null
            ? existing.GetComponent<TextMeshProUGUI>()
            : null;

        if (text == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = 5;
            textObject.transform.SetParent(transform, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
