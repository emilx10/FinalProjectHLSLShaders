using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GrassToolPanelSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/FinalProjectScene.unity";
    private const string PrefabFolder = "Assets/shaders/UI";
    private const string PrefabPath = PrefabFolder + "/GrassToolPanel.prefab";
    private const string AppliedSessionKey = "GrassToolPanelSceneBuilder_Applied_RemoveHiddenSliders1";

    static GrassToolPanelSceneBuilder()
    {
        EditorApplication.delayCall += ApplyOnceAfterCompile;
    }

    [MenuItem("Tools/Grass Tools/Rebuild Scene Tool Panel")]
    public static void ApplyNow()
    {
        ApplySceneAuthoredPanel(force: true);
    }

    private static void ApplyOnceAfterCompile()
    {
        if (SessionState.GetBool(AppliedSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AppliedSessionKey, true);
        ApplySceneAuthoredPanel(force: false);
    }

    private static void ApplySceneAuthoredPanel(bool force)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        bool sceneWasOpen = activeScene.path == ScenePath;
        Scene scene = sceneWasOpen
            ? activeScene
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PaintController paintController = Object.FindObjectOfType<PaintController>();
        Canvas targetCanvas = FindCanvas("UI Canvas");

        if (paintController == null || targetCanvas == null)
        {
            Debug.LogWarning("GrassToolPanelSceneBuilder could not find the FinalProjectScene PaintController or UI Canvas.");
            return;
        }

        GameObject prefab = CreateOrUpdatePrefab();
        RemoveOldToolButtons();
        RemoveCanvasPanelComponent(targetCanvas);
        RemoveExistingPanelChildren(targetCanvas);
        AddPanelInstance(targetCanvas, paintController, prefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Grass tool panel prefab created and FinalProjectScene updated.");
    }

    private static Canvas FindCanvas(string canvasName)
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == canvasName)
            {
                return canvases[i];
            }
        }

        return null;
    }

    private static GameObject CreateOrUpdatePrefab()
    {
        EnsureFolder("Assets/shaders");
        EnsureFolder(PrefabFolder);

        GameObject root = BuildPanelHierarchy();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject BuildPanelHierarchy()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        GameObject root = CreateUIObject("Grass Tool Panel", null, new Vector2(330f, 450f));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(24f, -24f);

        Image panelImage = root.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.065f, 0.88f);

        GrassToolPanel panel = root.AddComponent<GrassToolPanel>();

        TMP_Text activeTool = CreateText("Active Tool", root.transform, "Grow Grass", 26, FontStyles.Bold, new Vector2(18f, -18f), new Vector2(294f, 34f), font);

        Button growButton = CreateButton("Grow Button", root.transform, "Grow", new Vector2(18f, -64f), new Vector2(92f, 42f), font);
        Button cutButton = CreateButton("Cut Button", root.transform, "Cut", new Vector2(119f, -64f), new Vector2(92f, 42f), font);
        Button paintButton = CreateButton("Paint Button", root.transform, "Paint", new Vector2(220f, -64f), new Vector2(92f, 42f), font);

        Slider radiusSlider = CreateSlider("Brush Size Slider", root.transform, 0.005f, 0.18f, 0.05f, new Vector2(18f, -122f), font, out TMP_Text radiusValue);
        Slider strengthSlider = CreateSlider("Power Slider", root.transform, 0.01f, 0.25f, 0.08f, new Vector2(18f, -190f), font, out TMP_Text strengthValue);
        Slider falloffSlider = CreateSlider("Soft Edge Slider", root.transform, 0f, 1f, 0.5f, new Vector2(18f, -258f), font, out TMP_Text falloffValue);

        CreateText("Paint Color Label", root.transform, "Paint Color", 16, FontStyles.Bold, new Vector2(18f, -336f), new Vector2(120f, 24f), font);
        GrassToolPanel.ColorSwatch[] swatches = new GrassToolPanel.ColorSwatch[]
        {
            CreateSwatch(root.transform, new Color(0.95f, 0.16f, 0.14f, 1f), new Vector2(128f, -334f), "Red"),
            CreateSwatch(root.transform, new Color(0.16f, 0.44f, 0.95f, 1f), new Vector2(158f, -334f), "Blue"),
            CreateSwatch(root.transform, new Color(0.16f, 0.74f, 0.28f, 1f), new Vector2(188f, -334f), "Green"),
            CreateSwatch(root.transform, new Color(0.93f, 0.82f, 0.22f, 1f), new Vector2(218f, -334f), "Yellow"),
            CreateSwatch(root.transform, new Color(0.6f, 0.28f, 0.95f, 1f), new Vector2(248f, -334f), "Purple"),
            CreateSwatch(root.transform, Color.white, new Vector2(278f, -334f), "White")
        };

        Button resetButton = CreateButton("Reset Grass Button", root.transform, "Reset Grass", new Vector2(18f, -382f), new Vector2(142f, 36f), font);
        Button clearButton = CreateButton("Clear Paint Button", root.transform, "Clear Paint", new Vector2(170f, -382f), new Vector2(142f, 36f), font);

        SerializedObject serializedPanel = new SerializedObject(panel);
        SetObject(serializedPanel, "growButton", growButton);
        SetObject(serializedPanel, "cutButton", cutButton);
        SetObject(serializedPanel, "paintButton", paintButton);
        SetObject(serializedPanel, "activeToolText", activeTool);
        SetObject(serializedPanel, "radiusSlider", radiusSlider);
        SetObject(serializedPanel, "radiusValueText", radiusValue);
        SetObject(serializedPanel, "strengthSlider", strengthSlider);
        SetObject(serializedPanel, "strengthValueText", strengthValue);
        SetObject(serializedPanel, "falloffSlider", falloffSlider);
        SetObject(serializedPanel, "falloffValueText", falloffValue);
        SetObject(serializedPanel, "resetGrassButton", resetButton);
        SetObject(serializedPanel, "clearPaintButton", clearButton);
        SetSwatches(serializedPanel, swatches);
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static Slider CreateSlider(string name, Transform parent, float min, float max, float value, Vector2 position, TMP_FontAsset font, out TMP_Text valueText)
    {
        GameObject row = CreateUIObject(name, parent, new Vector2(294f, 62f));
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = position;
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(0.12f, 0.14f, 0.13f, 0.9f);

        string label = name.Replace(" Slider", string.Empty);
        CreateText(label + " Label", row.transform, label, 14, FontStyles.Bold, new Vector2(10f, -6f), new Vector2(180f, 20f), font);
        valueText = CreateText(label + " Value", row.transform, value.ToString("0.00"), 14, FontStyles.Normal, new Vector2(214f, -6f), new Vector2(68f, 20f), font);
        valueText.alignment = TextAlignmentOptions.Right;

        GameObject sliderObject = CreateUIObject("Slider", row.transform, new Vector2(274f, 22f));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(10f, -32f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);

        Image backgroundImage = CreateImage("Background", sliderObject.transform, new Vector2(0f, 0f), new Vector2(274f, 22f), new Color(0.04f, 0.05f, 0.045f, 1f));
        Image fillImage = CreateImage("Fill", sliderObject.transform, new Vector2(0f, -6f), new Vector2(0f, 10f), new Color(0.25f, 0.63f, 0.36f, 1f));
        Image handleImage = CreateImage("Handle", sliderObject.transform, new Vector2(0f, -1f), new Vector2(18f, 24f), new Color(0.93f, 0.96f, 0.91f, 1f));

        slider.targetGraphic = handleImage;
        slider.fillRect = fillImage.rectTransform;
        slider.handleRect = handleImage.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;

        backgroundImage.raycastTarget = false;
        fillImage.raycastTarget = false;
        return slider;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, TMP_FontAsset font)
    {
        GameObject buttonObject = CreateUIObject(name, parent, size);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.2f, 0.19f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText("Label", buttonObject.transform, label, 15, FontStyles.Bold, Vector2.zero, size, font);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }

    private static GrassToolPanel.ColorSwatch CreateSwatch(Transform parent, Color color, Vector2 position, string name)
    {
        GameObject swatchObject = CreateUIObject(name + " Swatch", parent, new Vector2(24f, 24f));
        RectTransform rect = swatchObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;

        Image image = swatchObject.AddComponent<Image>();
        image.color = color;

        Button button = swatchObject.AddComponent<Button>();
        button.targetGraphic = image;

        return new GrassToolPanel.ColorSwatch
        {
            button = button,
            image = image,
            color = color
        };
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int fontSize, FontStyles style, Vector2 position, Vector2 size, TMP_FontAsset font)
    {
        GameObject textObject = CreateUIObject(name, parent, size);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.93f, 0.96f, 0.91f, 1f);
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        if (font != null)
        {
            text.font = font;
        }

        return text;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject imageObject = CreateUIObject(name, parent, size);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = 5;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        return go;
    }

    private static void AddPanelInstance(Canvas canvas, PaintController paintController, GameObject prefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        instance.name = "Grass Tool Panel";

        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(330f, 450f);
        rect.localScale = Vector3.one;

        GrassToolPanel panel = instance.GetComponent<GrassToolPanel>();
        if (panel != null)
        {
            panel.SetPaintController(paintController);
            SerializedObject serializedPanel = new SerializedObject(panel);
            SetObject(serializedPanel, "paintController", paintController);
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void RemoveOldToolButtons()
    {
        ToolButton[] oldButtons = Object.FindObjectsOfType<ToolButton>(true);
        for (int i = 0; i < oldButtons.Length; i++)
        {
            if (oldButtons[i] != null)
            {
                Object.DestroyImmediate(oldButtons[i].gameObject);
            }
        }
    }

    private static void RemoveCanvasPanelComponent(Canvas canvas)
    {
        GrassToolPanel panel = canvas.GetComponent<GrassToolPanel>();
        if (panel != null)
        {
            Object.DestroyImmediate(panel);
        }
    }

    private static void RemoveExistingPanelChildren(Canvas canvas)
    {
        GrassToolPanel[] panels = canvas.GetComponentsInChildren<GrassToolPanel>(true);
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null && panels[i].gameObject != canvas.gameObject)
            {
                Object.DestroyImmediate(panels[i].gameObject);
            }
        }
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetSwatches(SerializedObject serializedObject, GrassToolPanel.ColorSwatch[] swatches)
    {
        SerializedProperty property = serializedObject.FindProperty("colorSwatches");
        if (property == null)
        {
            return;
        }

        property.arraySize = swatches.Length;
        for (int i = 0; i < swatches.Length; i++)
        {
            SerializedProperty item = property.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("button").objectReferenceValue = swatches[i].button;
            item.FindPropertyRelative("image").objectReferenceValue = swatches[i].image;
            item.FindPropertyRelative("color").colorValue = swatches[i].color;
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(folder);
        AssetDatabase.CreateFolder(parent, name);
    }
}
