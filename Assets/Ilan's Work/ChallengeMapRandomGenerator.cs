using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChallengeMapRandomGenerator : MonoBehaviour
{
    [Header("Main Map Size Reference")]
    [SerializeField] private Texture mainLengthMapReference;
    [SerializeField] private Texture mainColorMapReference;

    [Header("Random Generation")]
    [SerializeField] private Material painterMaterial;
    [SerializeField, Min(1)] private int randomStrokeCount = 64;
    [SerializeField, Min(0)] private int randomSeed = 1;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private bool saveGeneratedChallenge = true;

    [Header("Challenge Naming")]
    [SerializeField] private string challengeFolder = "Assets/ChallengeMaps";
    [SerializeField, Min(1)] private int challengeNumber = 1;

    [Header("Brush Ranges")]
    [SerializeField] private Vector2 radiusRange = new Vector2(0.01f, 0.08f);
    [SerializeField] private Vector2 strengthRange = new Vector2(0.05f, 0.2f);
    [SerializeField, Range(0f, 1f)] private float falloff = 0.5f;
    [SerializeField, Range(0f, 1f)] private float heightSmoothness = 0.5f;

    [Header("Length Map")]
    [SerializeField] private float growChance = 0.6f;
    [SerializeField] private Color startingLengthColor = Color.black;

    [Header("Color Map")]
    [SerializeField] private Color startingColor = Color.white;
    [SerializeField]
    private Color[] randomColors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(0.5f, 0f, 0.8f, 1f),
        Color.magenta
    };

    public RenderTexture GeneratedLengthMap { get; private set; }
    public RenderTexture GeneratedColorMap { get; private set; }
    public ChallengeMapDefinition GeneratedDefinition { get; private set; }

    [ContextMenu("Generate Random Challenge")]
    public void GenerateRandomChallenge()
    {
        if (painterMaterial == null)
        {
            Debug.LogError("Random challenge generation needs a painter material.");
            return;
        }

        int width;
        int height;
        if (!TryGetReferenceSize(out width, out height))
        {
            width = 8192;
            height = 8192;
        }

        EnsureTargetMaps(width, height);

        ClearTexture(GeneratedLengthMap, startingLengthColor);
        ClearTexture(GeneratedColorMap, startingColor);

        System.Random rng = useRandomSeed ? new System.Random(Environment.TickCount) : new System.Random(randomSeed);

        GenerateLengthMap(rng);
        GenerateColorMap(rng);

        if (saveGeneratedChallenge)
        {
            SaveGeneratedChallenge(width, height);
        }
    }

    private void GenerateLengthMap(System.Random rng)
    {
        int strokeCount = Mathf.Max(1, randomStrokeCount);

        for (int i = 0; i < strokeCount; i++)
        {
            Vector2 uv = new Vector2(
                Mathf.Lerp(0.15f, 0.85f, (float)rng.NextDouble()),
                Mathf.Lerp(0.20f, 0.95f, (float)rng.NextDouble()));

            float radius = Mathf.Lerp(radiusRange.x, radiusRange.y, (float)rng.NextDouble());
            float strength = Mathf.Lerp(strengthRange.x, strengthRange.y, (float)rng.NextDouble());
            bool grow = rng.NextDouble() < growChance;

            PaintStroke(
                GeneratedLengthMap,
                uv,
                radius,
                strength,
                grow ? 0f : 1f,
                Color.white,
                heightSmoothness);
        }
    }

    private void GenerateColorMap(System.Random rng)
    {
        int strokeCount = Mathf.Max(1, randomStrokeCount / 2);

        for (int i = 0; i < strokeCount; i++)
        {
            Vector2 uv = new Vector2(
                Mathf.Lerp(0.15f, 0.85f, (float)rng.NextDouble()),
                Mathf.Lerp(0.20f, 0.95f, (float)rng.NextDouble()));

            float radius = Mathf.Lerp(radiusRange.x, radiusRange.y, (float)rng.NextDouble());
            float strength = Mathf.Lerp(strengthRange.x, strengthRange.y, (float)rng.NextDouble());
            Color color = randomColors[rng.Next(0, randomColors.Length)];

            PaintStroke(
                GeneratedColorMap,
                uv,
                radius,
                strength,
                2f,
                color,
                0f);
        }
    }

    private void PaintStroke(RenderTexture target, Vector2 uv, float radius, float strength, float mode, Color brushColor, float smoothness)
    {
        painterMaterial.SetVector("_BrushUV", new Vector4(uv.x, uv.y, 0f, 0f));
        painterMaterial.SetFloat("_BrushRadius", radius);
        painterMaterial.SetFloat("_BrushStrength", strength);
        painterMaterial.SetFloat("_BrushFalloff", falloff);
        painterMaterial.SetFloat("_HeightSmoothness", smoothness);
        painterMaterial.SetColor("_BrushColor", brushColor);
        painterMaterial.SetFloat("_Mode", mode);

        RenderTexture temp = RenderTexture.GetTemporary(target.descriptor);
        Graphics.Blit(target, temp);
        Graphics.Blit(temp, target, painterMaterial);
        RenderTexture.ReleaseTemporary(temp);
    }

    private void EnsureTargetMaps(int width, int height)
    {
        ReleaseGeneratedMaps();
        GeneratedLengthMap = CreateTargetRenderTexture(width, height, "RandomChallengeLengthMap_Runtime");
        GeneratedColorMap = CreateTargetRenderTexture(width, height, "RandomChallengeColorMap_Runtime");
    }

    private void ReleaseGeneratedMaps()
    {
        if (GeneratedLengthMap != null)
        {
            GeneratedLengthMap.Release();
            Destroy(GeneratedLengthMap);
            GeneratedLengthMap = null;
        }

        if (GeneratedColorMap != null)
        {
            GeneratedColorMap.Release();
            Destroy(GeneratedColorMap);
            GeneratedColorMap = null;
        }
    }

    private RenderTexture CreateTargetRenderTexture(int width, int height, string name)
    {
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0);
        descriptor.msaaSamples = 1;
        descriptor.enableRandomWrite = false;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;

        RenderTexture rt = new RenderTexture(descriptor);
        rt.name = name;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();
        return rt;
    }

    private void ClearTexture(RenderTexture target, Color color)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        GL.Clear(true, true, color);
        RenderTexture.active = previous;
    }

    private bool TryGetReferenceSize(out int width, out int height)
    {
        width = 0;
        height = 0;

        Texture reference = mainLengthMapReference != null ? mainLengthMapReference : mainColorMapReference;
        if (reference == null)
        {
            return false;
        }

        width = reference.width;
        height = reference.height;

        if (mainLengthMapReference != null &&
            (mainLengthMapReference.width != width || mainLengthMapReference.height != height))
        {
            Debug.LogWarning("Random generator: main LengthMap reference size does not match.");
        }

        if (mainColorMapReference != null &&
            (mainColorMapReference.width != width || mainColorMapReference.height != height))
        {
            Debug.LogWarning("Random generator: main ColorMap reference size does not match.");
        }

        return true;
    }

    private void SaveGeneratedChallenge(int width, int height)
    {
#if UNITY_EDITOR
        string challengeFolderName = "RandomChallenge_" + challengeNumber.ToString();
        string fullFolder = ChallengeMapSaveUtility.BuildChallengeFolder(challengeFolder, challengeFolderName);

        ChallengeMapSaveUtility.EnsureProjectFolder(fullFolder);

        Texture2D lengthAsset = ChallengeMapSaveUtility.SaveTextureAsPng(
            GeneratedLengthMap,
            fullFolder + "/RandomChallengeLengthMap_" + challengeNumber.ToString() + ".png",
            true);

        Texture2D colorAsset = ChallengeMapSaveUtility.SaveTextureAsPng(
            GeneratedColorMap,
            fullFolder + "/RandomChallengeColorMap_" + challengeNumber.ToString() + ".png",
            false);

        GeneratedDefinition = ChallengeMapSaveUtility.CreateOrUpdateDefinitionAsset(
            fullFolder,
            challengeFolderName,
            ChallengeMapDefinition.ChallengeSource.Random,
            lengthAsset,
            colorAsset,
            challengeNumber);

        Debug.Log("Saved random challenge in folder: " + fullFolder + " (" + width + "x" + height + ")");
#else
        Debug.LogWarning("Challenge saving is editor-only.");
#endif
    }

    private void OnDestroy()
    {
        ReleaseGeneratedMaps();
    }
}