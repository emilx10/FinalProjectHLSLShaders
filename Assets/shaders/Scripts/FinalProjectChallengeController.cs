using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalProjectChallengeController : MonoBehaviour
{
    private enum ChallengeDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    [Header("References")]
    [SerializeField] private PaintController paintController;

    [Header("Difficulty")]
    [SerializeField] private ChallengeDifficulty currentDifficulty = ChallengeDifficulty.Medium;

    [Header("Target Generation")]
    [SerializeField, Min(1)] private int randomStrokeCount = 64;
    [SerializeField] private Vector2 radiusRange = new Vector2(0.01f, 0.08f);
    [SerializeField] private Vector2 strengthRange = new Vector2(0.05f, 0.2f);
    [SerializeField, Range(0f, 1f)] private float growChance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float falloff = 0.5f;
    [SerializeField, Range(0f, 1f)] private float heightSmoothness = 0.5f;

    [Header("Scoring")]
    [SerializeField, Range(32, 512)] private int scoreSampleResolution = 96;
    [SerializeField, Range(0, 24)] private int nearbyMatchRadiusPixels = 10;
    [SerializeField, Range(0.001f, 1f)] private float lengthTolerance = 0.45f;
    [SerializeField, Range(0.001f, 1f)] private float colorTolerance = 0.55f;
    [SerializeField, Range(0f, 1f)] private float activeTargetThreshold = 0.02f;
    [SerializeField, Range(0f, 1f)] private float activePlayerThreshold = 0.02f;
    [SerializeField, Range(0f, 1f)] private float lengthWeight = 0.75f;
    [SerializeField, Range(0f, 1f)] private float colorWeight = 0.25f;

    [Header("Target Plane")]
    [SerializeField, Min(0f)] private float minimumEdgeGap = 0.75f;
    [SerializeField, Min(0f)] private float edgeGapSizeRatio = 0.08f;
    [SerializeField, Min(0f)] private float maximumEdgeGap = 1.5f;
    [SerializeField] private string targetPlaneName = "Challenge Target Plane";

    private readonly Color[] randomColors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(0.5f, 0f, 0.8f, 1f),
        Color.magenta
    };

    private RenderTexture targetLengthMap;
    private RenderTexture targetColorMap;
    private Material targetRenderMaterial;
    private Material targetPainterMaterial;
    private GameObject targetPlane;

    private Button newTargetButton;
    private Button scoreButton;
    private Button easyButton;
    private Button mediumButton;
    private Button hardButton;
    private TMP_Text scoreText;
    private TMP_Text rankText;

    private void Start()
    {
        if (paintController == null)
        {
            paintController = FindFirstObjectByType<PaintController>();
        }

        EnsureChallengeUI();
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture(targetLengthMap);
        ReleaseRenderTexture(targetColorMap);

        if (targetRenderMaterial != null)
        {
            Destroy(targetRenderMaterial);
        }

        if (targetPainterMaterial != null)
        {
            Destroy(targetPainterMaterial);
        }
    }

    public void SetPaintController(PaintController controller)
    {
        paintController = controller;
    }

    public void CreateNewTarget()
    {
        if (paintController == null || paintController.PaintMaterial == null || paintController.HairRenderMaterial == null)
        {
            SetScoreText("Missing paint setup", "-");
            Debug.LogError("FinalProjectChallengeController needs PaintController materials.");
            return;
        }

        EnsureTargetMaps();
        EnsurePainterMaterial();

        ClearTexture(targetLengthMap, Color.black);
        ClearTexture(targetColorMap, new Color(1f, 1f, 1f, 0f));

        System.Random rng = new System.Random(System.Environment.TickCount);
        GenerateLengthMap(rng);
        GenerateColorMap(rng);

        EnsureTargetPlane();
        BindTargetMapsToPlane();

        paintController.ClearLengthMapToBlack();
        paintController.ClearColorMapToWhite();
        SetScoreText("Score: --", "Rank: --");
    }

    public void ScoreCurrentAttempt()
    {
        if (targetLengthMap == null || targetColorMap == null)
        {
            SetScoreText("Create target first", "Rank: --");
            return;
        }

        Texture playerLengthMap = paintController != null ? paintController.LengthMap : null;
        Texture playerColorMap = paintController != null ? paintController.ColorMap : null;

        if (playerLengthMap == null || playerColorMap == null)
        {
            SetScoreText("Missing player maps", "Rank: --");
            return;
        }

        float score01 = CompareMaps(playerLengthMap, playerColorMap, targetLengthMap, targetColorMap, out bool changed);
        if (!changed)
        {
            SetScoreText("Score: None", "Rank: None");
            return;
        }

        SetScoreText("Score: " + (score01 * 100f).ToString("0.00") + "%", "Rank: " + EvaluateRank(score01));
    }

    private void EnsureChallengeUI()
    {
        if (newTargetButton != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Challenge Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform existing = canvas.transform.Find("Challenge Side Panel");
        GameObject panelObject = existing != null ? existing.gameObject : new GameObject("Challenge Side Panel");
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = EnsureRectTransform(panelObject);
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-24f, 0f);
        panelRect.sizeDelta = new Vector2(190f, 220f);
        panelRect.localScale = Vector3.one;

        Image panelImage = panelObject.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelObject.AddComponent<Image>();
        }
        panelImage.color = new Color(0.08f, 0.09f, 0.08f, 0.88f);

        easyButton = CreateDifficultyButton(panelRect, "Easy Button", "Easy", new Vector2(-52f, -18f), ChallengeDifficulty.Easy);
        mediumButton = CreateDifficultyButton(panelRect, "Medium Button", "Med", new Vector2(0f, -18f), ChallengeDifficulty.Medium);
        hardButton = CreateDifficultyButton(panelRect, "Hard Button", "Hard", new Vector2(52f, -18f), ChallengeDifficulty.Hard);
        newTargetButton = CreateButton(panelRect, "New Target Button", "New Target", new Vector2(0f, -64f), CreateNewTarget);
        scoreButton = CreateButton(panelRect, "Score Button", "Check Score", new Vector2(0f, -110f), ScoreCurrentAttempt);
        scoreText = CreateText(panelRect, "Score Text", "Score: --", new Vector2(0f, -156f), 16, FontStyles.Bold);
        rankText = CreateText(panelRect, "Rank Text", "Rank: --", new Vector2(0f, -182f), 16, FontStyles.Bold);
        RefreshDifficultyButtons();
    }

    private Button CreateButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = EnsureRectTransform(buttonObject);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(154f, 36f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.42f, 0.24f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        CreateButtonLabel(rect, label, 15);
        return button;
    }

    private Button CreateDifficultyButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, ChallengeDifficulty difficulty)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = EnsureRectTransform(buttonObject);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(48f, 30f);

        Image image = buttonObject.AddComponent<Image>();

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => SetDifficulty(difficulty));

        CreateButtonLabel(rect, label, 13);
        return button;
    }

    private TMP_Text CreateButtonLabel(RectTransform parent, string label, int size)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = EnsureRectTransform(textObject);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void SetDifficulty(ChallengeDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        RefreshDifficultyButtons();
        SetScoreText("Score: --", "Rank: --");
    }

    private void RefreshDifficultyButtons()
    {
        SetDifficultyButtonSelected(easyButton, currentDifficulty == ChallengeDifficulty.Easy);
        SetDifficultyButtonSelected(mediumButton, currentDifficulty == ChallengeDifficulty.Medium);
        SetDifficultyButtonSelected(hardButton, currentDifficulty == ChallengeDifficulty.Hard);
    }

    private void SetDifficultyButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected ? new Color(0.34f, 0.62f, 0.3f, 1f) : new Color(0.14f, 0.16f, 0.15f, 1f);
        }
    }

    private TMP_Text CreateText(RectTransform parent, string name, string value, Vector2 anchoredPosition, int size, FontStyles style)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = EnsureRectTransform(textObject);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(170f, 24f);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect != null)
        {
            return rect;
        }

        return target.AddComponent<RectTransform>();
    }

    private void SetScoreText(string score, string rank)
    {
        if (scoreText != null)
        {
            scoreText.text = score;
        }

        if (rankText != null)
        {
            rankText.text = rank;
        }
    }

    private void EnsureTargetMaps()
    {
        int width = paintController != null && paintController.LengthMap != null ? paintController.LengthMap.width : 1024;
        int height = paintController != null && paintController.LengthMap != null ? paintController.LengthMap.height : width;

        if (paintController != null && paintController.ColorMap != null &&
            (paintController.ColorMap.width != width || paintController.ColorMap.height != height))
        {
            Debug.LogWarning("Player LengthMap and ColorMap sizes do not match. Using LengthMap size for challenge target.");
        }

        if (targetLengthMap != null && targetLengthMap.width == width && targetLengthMap.height == height &&
            targetColorMap != null && targetColorMap.width == width && targetColorMap.height == height)
        {
            return;
        }

        ReleaseRenderTexture(targetLengthMap);
        ReleaseRenderTexture(targetColorMap);

        targetLengthMap = CreateRenderTexture(width, height, "RuntimeChallenge_LengthMap");
        targetColorMap = CreateRenderTexture(width, height, "RuntimeChallenge_ColorMap");
    }

    private RenderTexture CreateRenderTexture(int width, int height, string textureName)
    {
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0);
        descriptor.msaaSamples = 1;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;

        RenderTexture texture = new RenderTexture(descriptor);
        texture.name = textureName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Create();
        return texture;
    }

    private void EnsurePainterMaterial()
    {
        if (targetPainterMaterial != null)
        {
            return;
        }

        targetPainterMaterial = new Material(paintController.PaintMaterial);
        targetPainterMaterial.name = "Runtime Challenge Painter Material";
    }

    private void EnsureTargetPlane()
    {
        if (targetPlane != null)
        {
            return;
        }

        SubdividedPlaneGenerator sourcePlane = FindPlayerPlane();
        targetPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        targetPlane.name = targetPlaneName;
        targetPlane.layer = 2;

        Transform targetTransform = targetPlane.transform;
        if (sourcePlane != null)
        {
            targetTransform.rotation = sourcePlane.transform.rotation;
            targetTransform.localScale = sourcePlane.transform.localScale;
            targetTransform.position = GetTargetPlanePosition(sourcePlane);

            SubdividedPlaneGenerator generatedPlane = targetPlane.AddComponent<SubdividedPlaneGenerator>();
            generatedPlane.SetSize(sourcePlane.Width, sourcePlane.Length);
            generatedPlane.SetSubdivisions(sourcePlane.Subdivisions);
        }
        else
        {
            targetTransform.position = new Vector3(12f, 0f, 0f);
            targetTransform.localScale = Vector3.one * 1.5f;
        }

        Collider targetCollider = targetPlane.GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }
    }

    private Vector3 GetTargetPlanePosition(SubdividedPlaneGenerator sourcePlane)
    {
        float worldWidth = Mathf.Abs(sourcePlane.Width * sourcePlane.transform.lossyScale.x);
        float worldLength = Mathf.Abs(sourcePlane.Length * sourcePlane.transform.lossyScale.z);
        float largestSide = Mathf.Max(worldWidth, worldLength);
        float edgeGap = Mathf.Clamp(largestSide * edgeGapSizeRatio, minimumEdgeGap, maximumEdgeGap);

        Vector3 sideDirection = sourcePlane.transform.right.normalized;
        float centerDistance = worldWidth + edgeGap;
        return sourcePlane.transform.position + sideDirection * centerDistance;
    }

    private SubdividedPlaneGenerator FindPlayerPlane()
    {
        SubdividedPlaneGenerator[] planes = FindObjectsByType<SubdividedPlaneGenerator>(FindObjectsSortMode.None);
        for (int i = 0; i < planes.Length; i++)
        {
            if (planes[i] != null && planes[i].gameObject.name != targetPlaneName)
            {
                return planes[i];
            }
        }

        return null;
    }

    private void BindTargetMapsToPlane()
    {
        if (targetPlane == null || paintController == null || paintController.HairRenderMaterial == null)
        {
            return;
        }

        if (targetRenderMaterial == null)
        {
            targetRenderMaterial = new Material(paintController.HairRenderMaterial);
            targetRenderMaterial.name = "Runtime Challenge Target Grass Material";
        }

        targetRenderMaterial.SetTexture("_LengthMap", targetLengthMap);
        targetRenderMaterial.SetTexture("_ColorMap", targetColorMap);

        Renderer targetRenderer = targetPlane.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetRenderer.sharedMaterial = targetRenderMaterial;
        }
    }

    private void GenerateLengthMap(System.Random rng)
    {
        int strokeCount = Mathf.Max(1, GetDifficultyStrokeCount());

        for (int i = 0; i < strokeCount; i++)
        {
            Vector2 uv = RandomUV(rng);
            float radius = RandomRange(rng, GetDifficultyRadiusRange());
            float strength = RandomRange(rng, GetDifficultyStrengthRange());
            bool grow = rng.NextDouble() < growChance;

            PaintStroke(targetLengthMap, uv, radius, strength, grow ? 0f : 1f, Color.white, heightSmoothness);
        }
    }

    private void GenerateColorMap(System.Random rng)
    {
        int strokeCount = Mathf.Max(1, GetDifficultyStrokeCount() / 2);

        for (int i = 0; i < strokeCount; i++)
        {
            Vector2 uv = RandomUV(rng);
            float radius = RandomRange(rng, GetDifficultyRadiusRange());
            float strength = RandomRange(rng, GetDifficultyStrengthRange());
            Color brushColor = randomColors[rng.Next(0, randomColors.Length)];
            Color secondaryColor = randomColors[rng.Next(0, randomColors.Length)];

            PaintStroke(
                targetColorMap,
                uv,
                radius,
                strength,
                2f,
                brushColor,
                0f,
                secondaryColor,
                rng.Next(0, 3),
                RandomRange(rng, new Vector2(8f, 28f)),
                0.08f,
                RandomRange(rng, new Vector2(0f, 360f)));
        }
    }

    private Vector2 RandomUV(System.Random rng)
    {
        return new Vector2(
            Mathf.Lerp(0.15f, 0.85f, (float)rng.NextDouble()),
            Mathf.Lerp(0.20f, 0.95f, (float)rng.NextDouble()));
    }

    private float RandomRange(System.Random rng, Vector2 range)
    {
        return Mathf.Lerp(range.x, range.y, (float)rng.NextDouble());
    }

    private void PaintStroke(
        RenderTexture target,
        Vector2 uv,
        float radius,
        float strength,
        float mode,
        Color brushColor,
        float smoothness,
        Color? secondaryColor = null,
        float paintBlendMode = 0f,
        float patternScale = 16f,
        float patternSoftness = 0.08f,
        float ombreAngle = 0f)
    {
        targetPainterMaterial.SetVector("_BrushUV", new Vector4(uv.x, uv.y, 0f, 0f));
        targetPainterMaterial.SetFloat("_BrushRadius", radius);
        targetPainterMaterial.SetFloat("_BrushStrength", strength);
        targetPainterMaterial.SetFloat("_BrushFalloff", falloff);
        targetPainterMaterial.SetFloat("_HeightSmoothness", smoothness);
        targetPainterMaterial.SetColor("_BrushColor", brushColor);
        targetPainterMaterial.SetColor("_SecondaryBrushColor", secondaryColor ?? Color.white);
        targetPainterMaterial.SetFloat("_PaintBlendMode", paintBlendMode);
        targetPainterMaterial.SetFloat("_PatternScale", patternScale);
        targetPainterMaterial.SetFloat("_PatternSoftness", patternSoftness);
        targetPainterMaterial.SetFloat("_OmbreAngle", ombreAngle);
        targetPainterMaterial.SetFloat("_Mode", mode);

        RenderTexture temp = RenderTexture.GetTemporary(target.descriptor);
        Graphics.Blit(target, temp);
        Graphics.Blit(temp, target, targetPainterMaterial);
        RenderTexture.ReleaseTemporary(temp);
    }

    private float CompareMaps(Texture playerLength, Texture playerColor, Texture targetLength, Texture targetColor, out bool changed)
    {
        int size = Mathf.Clamp(GetDifficultyScoreResolution(), 32, 512);
        Texture2D playerLengthPixels = ReadTexture(playerLength, size, true);
        Texture2D playerColorPixels = ReadTexture(playerColor, size, false);
        Texture2D targetLengthPixels = ReadTexture(targetLength, size, true);
        Texture2D targetColorPixels = ReadTexture(targetColor, size, false);

        Color[] playerLengthColors = playerLengthPixels.GetPixels();
        Color[] playerColorColors = playerColorPixels.GetPixels();
        Color[] targetLengthColors = targetLengthPixels.GetPixels();
        Color[] targetColorColors = targetColorPixels.GetPixels();

        float totalScore = 0f;
        float totalWeight = 0f;
        changed = false;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                float targetLengthValue = targetLengthColors[i].r;
                if (targetLengthValue < GetDifficultyActiveTargetThreshold())
                {
                    continue;
                }

                Color targetColorValue = targetColorColors[i];
                float bestScore = FindBestNearbyMatch(
                    x,
                    y,
                    size,
                    targetLengthValue,
                    targetColorValue,
                    playerLengthColors,
                    playerColorColors);

                float activeWeight = Mathf.Clamp01(targetLengthValue);
                totalScore += bestScore * activeWeight;
                totalWeight += activeWeight;
            }
        }

        changed = HasPlayerChanged(playerLengthColors, playerColorColors);

        Destroy(playerLengthPixels);
        Destroy(playerColorPixels);
        Destroy(targetLengthPixels);
        Destroy(targetColorPixels);

        if (totalWeight <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(totalScore / totalWeight);
    }

    private float FindBestNearbyMatch(
        int targetX,
        int targetY,
        int textureSize,
        float targetLengthValue,
        Color targetColorValue,
        Color[] playerLengthColors,
        Color[] playerColorColors)
    {
        int radius = Mathf.Clamp(GetDifficultyNearbyMatchRadiusPixels(), 0, 24);
        float bestScore = 0f;

        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            int y = targetY + offsetY;
            if (y < 0 || y >= textureSize)
            {
                continue;
            }

            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                int x = targetX + offsetX;
                if (x < 0 || x >= textureSize)
                {
                    continue;
                }

                int i = y * textureSize + x;
                float playerLengthValue = playerLengthColors[i].r;
                if (playerLengthValue < GetDifficultyActivePlayerThreshold())
                {
                    continue;
                }

                Color playerColorValue = playerColorColors[i];

                float lengthDiff = Mathf.Abs(playerLengthValue - targetLengthValue);
                float colorDiff = (Mathf.Abs(playerColorValue.r - targetColorValue.r) +
                                   Mathf.Abs(playerColorValue.g - targetColorValue.g) +
                                   Mathf.Abs(playerColorValue.b - targetColorValue.b)) / 3f;

                float lengthScore = 1f - Mathf.Clamp01(lengthDiff / Mathf.Max(GetDifficultyLengthTolerance(), 0.0001f));
                float paintScore = 1f - Mathf.Clamp01(colorDiff / Mathf.Max(GetDifficultyColorTolerance(), 0.0001f));
                float presenceScore = Mathf.Clamp01(playerLengthValue / Mathf.Max(targetLengthValue, 0.0001f));
                float distance01 = radius > 0 ? new Vector2(offsetX, offsetY).magnitude / radius : 0f;
                float proximityScore = Mathf.Lerp(1f, GetDifficultyEdgeProximityScore(), Mathf.Clamp01(distance01));
                float currentLengthWeight = GetDifficultyLengthWeight();
                float currentColorWeight = GetDifficultyColorWeight();
                float totalMapWeight = Mathf.Max(currentLengthWeight + currentColorWeight, 0.0001f);
                float combinedScore = Mathf.Clamp01((lengthScore * currentLengthWeight + paintScore * currentColorWeight) / totalMapWeight);

                bestScore = Mathf.Max(bestScore, combinedScore * presenceScore * proximityScore);
            }
        }

        return bestScore;
    }

    private int GetDifficultyStrokeCount()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 12;
            case ChallengeDifficulty.Medium:
                return 28;
            default:
                return randomStrokeCount;
        }
    }

    private Vector2 GetDifficultyRadiusRange()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return new Vector2(0.04f, 0.14f);
            case ChallengeDifficulty.Medium:
                return new Vector2(0.025f, 0.11f);
            default:
                return radiusRange;
        }
    }

    private Vector2 GetDifficultyStrengthRange()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return new Vector2(0.12f, 0.35f);
            case ChallengeDifficulty.Medium:
                return new Vector2(0.08f, 0.28f);
            default:
                return strengthRange;
        }
    }

    private int GetDifficultyScoreResolution()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 64;
            case ChallengeDifficulty.Medium:
                return 80;
            default:
                return scoreSampleResolution;
        }
    }

    private int GetDifficultyNearbyMatchRadiusPixels()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 18;
            case ChallengeDifficulty.Medium:
                return 14;
            default:
                return nearbyMatchRadiusPixels;
        }
    }

    private float GetDifficultyLengthTolerance()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 0.75f;
            case ChallengeDifficulty.Medium:
                return 0.6f;
            default:
                return lengthTolerance;
        }
    }

    private float GetDifficultyColorTolerance()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 0.85f;
            case ChallengeDifficulty.Medium:
                return 0.7f;
            default:
                return colorTolerance;
        }
    }

    private float GetDifficultyActiveTargetThreshold()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 0.01f;
            case ChallengeDifficulty.Medium:
                return 0.015f;
            default:
                return activeTargetThreshold;
        }
    }

    private float GetDifficultyActivePlayerThreshold()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 0.005f;
            case ChallengeDifficulty.Medium:
                return 0.01f;
            default:
                return activePlayerThreshold;
        }
    }

    private float GetDifficultyLengthWeight()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 0.85f;
            case ChallengeDifficulty.Medium:
                return 0.8f;
            default:
                return lengthWeight;
        }
    }

    private float GetDifficultyColorWeight()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 0.15f;
            case ChallengeDifficulty.Medium:
                return 0.2f;
            default:
                return colorWeight;
        }
    }

    private float GetDifficultyEdgeProximityScore()
    {
        switch (currentDifficulty)
        {
            case ChallengeDifficulty.Easy:
                return 0.98f;
            case ChallengeDifficulty.Medium:
                return 0.95f;
            default:
                return 0.92f;
        }
    }

    private bool HasPlayerChanged(Color[] playerLengthColors, Color[] playerColorColors)
    {
        for (int i = 0; i < playerLengthColors.Length; i++)
        {
            Color playerColorValue = playerColorColors[i];
            if (Mathf.Abs(playerLengthColors[i].r) > 0.0001f ||
                Mathf.Abs(playerColorValue.r - 1f) > 0.0001f ||
                Mathf.Abs(playerColorValue.g - 1f) > 0.0001f ||
                Mathf.Abs(playerColorValue.b - 1f) > 0.0001f)
            {
                return true;
            }
        }

        return false;
    }

    private Texture2D ReadTexture(Texture source, int size, bool linear)
    {
        RenderTexture temp = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
        temp.wrapMode = TextureWrapMode.Clamp;
        temp.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, temp);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = temp;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
        texture.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
        texture.Apply(false, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temp);

        return texture;
    }

    private string EvaluateRank(float score01)
    {
        if (score01 >= 0.95f) return "S";
        if (score01 >= 0.85f) return "A";
        if (score01 >= 0.70f) return "B";
        if (score01 >= 0.50f) return "C";
        if (score01 >= 0.40f) return "D";
        if (score01 >= 0.20f) return "E";
        return "F";
    }

    private void ClearTexture(RenderTexture target, Color color)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        GL.Clear(true, true, color);
        RenderTexture.active = previous;
    }

    private void ReleaseRenderTexture(RenderTexture texture)
    {
        if (texture == null)
        {
            return;
        }

        texture.Release();
        Destroy(texture);
    }
}
