using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PaintController : MonoBehaviour
{
    public enum ToolMode
    {
        Grow = 0,
        Cut = 1,
        Color = 2
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask paintLayerMask = ~0;
    [SerializeField] private Material paintMaterial;
    [SerializeField] private Material hairRenderMaterial;

    [Header("Render Textures")]
    [SerializeField] private RenderTexture lengthMap;
    [SerializeField] private RenderTexture colorMap;

    [Header("Brush")]
    [SerializeField] private ToolMode currentTool = ToolMode.Cut;
    [SerializeField,] private float brushRadius = 0.05f;
    [SerializeField,] private float brushStrength = 0.08f;
    [SerializeField, Range(0f, 1f)] private float brushFalloff = 0.5f;
    [SerializeField, Range(0f, 1f)] private float heightSmoothness = 0.75f;
    [SerializeField, Min(0f)] private float continuousHeightPaintRate = 3f;
    [SerializeField, Range(0.001f, 1f)] private float heightStepLimit = 0.08f;
    [SerializeField] private Color brushColor = Color.red;

    [Header("Input")]
    [SerializeField] private bool paintContinuouslyWhileHolding = true;

    [Header("Shader Property Names")]
    [SerializeField] private string brushUVProperty = "_BrushUV";
    [SerializeField] private string brushRadiusProperty = "_BrushRadius";
    [SerializeField] private string brushStrengthProperty = "_BrushStrength";
    [SerializeField] private string brushFalloffProperty = "_BrushFalloff";
    [SerializeField] private string heightSmoothnessProperty = "_HeightSmoothness";
    [SerializeField] private string heightStepLimitProperty = "_HeightStepLimit";
    [SerializeField] private string brushColorProperty = "_BrushColor";
    [SerializeField] private string modeProperty = "_Mode";
    [SerializeField] private string lengthMapProperty = "_LengthMap";
    [SerializeField] private string colorMapProperty = "_ColorMap";
    [SerializeField] private string lengthMultiplierProperty = "_LengthMulty";

    [Header("Startup")]
    [SerializeField] private bool clearMapsOnAwake = true;
    [SerializeField] private Color initialLengthColor = Color.black; // black = shaved start
    [SerializeField] private Color initialColorColor = Color.white;

    [Header("Picking")]
    [SerializeField] private bool useHeightAwarePicking = true;
    [SerializeField, Range(8, 256)] private int heightRaymarchSteps = 64;
    [SerializeField, Range(2, 16)] private int heightRaymarchRefinementSteps = 6;

    [Header("VFX")]
    [SerializeField] private GameObject cutVFXPrefab;
    [SerializeField] private GameObject growVFXPrefab;
    [SerializeField] private GameObject colorVFXPrefab;

    private Texture2D lengthMapReadback;
    private bool lengthMapReadbackDirty = true;

    public ToolMode CurrentTool => currentTool;
    public float BrushRadius => brushRadius;
    public float BrushStrength => brushStrength;
    public float BrushFalloff => brushFalloff;
    public float HeightSmoothness => heightSmoothness;
    public float ContinuousHeightPaintRate => continuousHeightPaintRate;
    public float HeightStepLimit => heightStepLimit;
    public Color BrushColor => brushColor;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        EnsureRenderTextureCreated(lengthMap);
        EnsureRenderTextureCreated(colorMap);

        if (clearMapsOnAwake)
        {
            if (lengthMap != null)
            {
                ClearRenderTexture(lengthMap, initialLengthColor);
                lengthMapReadbackDirty = true;
            }

            if (colorMap != null)
                ClearRenderTexture(colorMap, initialColorColor);
        }

        BindMapsToRenderMaterial();
    }

    private void Update()
    {
        HandleToolHotkeys();

        if (paintContinuouslyWhileHolding)
        {
            if (IsPaintHeld())
                TryPaintAtMouse(Time.deltaTime);
        }
        else
        {
            if (WasPaintPressedThisFrame())
                TryPaintAtMouse(1f);
        }
    }

    private void HandleToolHotkeys()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            currentTool = ToolMode.Grow;

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            currentTool = ToolMode.Cut;

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            currentTool = ToolMode.Color;
    }

    private bool IsPaintHeld()
    {
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool WasPaintPressedThisFrame()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void TryPaintAtMouse(float paintScale)
    {
        if (targetCamera == null || Mouse.current == null || paintMaterial == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, paintLayerMask))
            return;

        Vector2 uv = ResolvePaintUV(hit, ray);

        PaintAtUV(uv, paintScale);

        Vector3 worldPos = hit.point;

        SpawnVFX(worldPos);
    }

    private void SpawnVFX(Vector3 position)
    {
        GameObject prefab = null;

        switch (currentTool)
        {
            case ToolMode.Cut:
                prefab = cutVFXPrefab;
                break;

            case ToolMode.Grow:
                prefab = growVFXPrefab;
                break;

            case ToolMode.Color:
                prefab = colorVFXPrefab;
                break;
        }

        if (prefab != null)
        {
            GameObject vfx = Instantiate(prefab, position, Quaternion.identity);

            // If it's the color tool, set the particle color
            if (currentTool == ToolMode.Color)
            {
                ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = brushColor;  // <-- Set color to the current brush color
                }
            }

            Destroy(vfx, 2f);
        }
    }

    public void PaintAtUV(Vector2 uv)
    {
        PaintAtUV(uv, 1f);
    }

    public void PaintAtUV(Vector2 uv, float paintScale)
    {
        if (paintMaterial == null)
            return;

        float appliedStrength = brushStrength;
        bool isHeightTool = currentTool == ToolMode.Grow || currentTool == ToolMode.Cut;

        if (paintContinuouslyWhileHolding && isHeightTool)
            appliedStrength *= continuousHeightPaintRate * Mathf.Max(0f, paintScale);

        paintMaterial.SetVector(brushUVProperty, new Vector4(uv.x, uv.y, 0f, 0f));
        paintMaterial.SetFloat(brushRadiusProperty, brushRadius);
        paintMaterial.SetFloat(brushStrengthProperty, appliedStrength);
        paintMaterial.SetFloat(brushFalloffProperty, brushFalloff);
        paintMaterial.SetFloat(heightSmoothnessProperty, heightSmoothness);
        paintMaterial.SetFloat(heightStepLimitProperty, heightStepLimit);
        paintMaterial.SetColor(brushColorProperty, brushColor);
        paintMaterial.SetFloat(modeProperty, (float)currentTool);

        switch (currentTool)
        {
            case ToolMode.Grow:
            case ToolMode.Cut:
                PaintIntoTarget(lengthMap);
                lengthMapReadbackDirty = true;
                break;

            case ToolMode.Color:
                PaintIntoTarget(colorMap);
                break;
        }

        BindMapsToRenderMaterial();
    }

    private void PaintIntoTarget(RenderTexture target)
    {
        if (target == null || paintMaterial == null)
            return;

        EnsureRenderTextureCreated(target);

        RenderTexture temp = RenderTexture.GetTemporary(target.descriptor);
        temp.wrapMode = TextureWrapMode.Clamp;
        temp.filterMode = FilterMode.Bilinear;

        Graphics.Blit(target, temp);
        Graphics.Blit(temp, target, paintMaterial);

        RenderTexture.ReleaseTemporary(temp);
    }

    private void BindMapsToRenderMaterial()
    {
        if (hairRenderMaterial == null)
            return;

        if (lengthMap != null)
            hairRenderMaterial.SetTexture(lengthMapProperty, lengthMap);

        if (colorMap != null)
            hairRenderMaterial.SetTexture(colorMapProperty, colorMap);
    }

    private void EnsureRenderTextureCreated(RenderTexture rt)
    {
        if (rt != null && !rt.IsCreated())
            rt.Create();
    }

    private void ClearRenderTexture(RenderTexture rt, Color clearColor)
    {
        if (rt == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, clearColor);
        RenderTexture.active = previous;

        if (rt == lengthMap)
            lengthMapReadbackDirty = true;
    }

    private Vector2 ResolvePaintUV(RaycastHit hit, Ray ray)
    {
        if (!useHeightAwarePicking || lengthMap == null || hairRenderMaterial == null)
            return hit.textureCoord;

        SubdividedPlaneGenerator plane = hit.collider != null ? hit.collider.GetComponent<SubdividedPlaneGenerator>() : null;
        if (plane == null)
            return hit.textureCoord;

        if (!TryRefreshLengthMapReadback())
            return hit.textureCoord;

        if (TryGetDisplacedSurfaceUV(ray, hit.collider.transform, plane, out Vector2 refinedUV))
            return refinedUV;

        return hit.textureCoord;
    }

    private bool TryRefreshLengthMapReadback()
    {
        if (lengthMap == null)
            return false;

        EnsureRenderTextureCreated(lengthMap);

        if (lengthMapReadback != null &&
            !lengthMapReadbackDirty &&
            lengthMapReadback.width == lengthMap.width &&
            lengthMapReadback.height == lengthMap.height)
        {
            return true;
        }

        if (lengthMapReadback == null ||
            lengthMapReadback.width != lengthMap.width ||
            lengthMapReadback.height != lengthMap.height)
        {
            lengthMapReadback = new Texture2D(lengthMap.width, lengthMap.height, TextureFormat.RGBA32, false, true);
            lengthMapReadback.wrapMode = TextureWrapMode.Clamp;
            lengthMapReadback.filterMode = FilterMode.Bilinear;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = lengthMap;
        lengthMapReadback.ReadPixels(new Rect(0, 0, lengthMap.width, lengthMap.height), 0, 0, false);
        lengthMapReadback.Apply(false, false);
        RenderTexture.active = previous;

        lengthMapReadbackDirty = false;
        return true;
    }

    private bool TryGetDisplacedSurfaceUV(Ray worldRay, Transform planeTransform, SubdividedPlaneGenerator plane, out Vector2 uv)
    {
        uv = default;

        Vector3 originLS = planeTransform.InverseTransformPoint(worldRay.origin);
        Vector3 directionLS = planeTransform.InverseTransformDirection(worldRay.direction).normalized;

        if (Mathf.Abs(directionLS.y) < 0.00001f)
            return false;

        float halfWidth = plane.Width * 0.5f;
        float halfLength = plane.Length * 0.5f;
        float maxHeight = Mathf.Max(0f, hairRenderMaterial.GetFloat(lengthMultiplierProperty));

        float tMin = 0f;
        float tMax = 5000f;

        if (!ClipRayToAxisBounds(originLS.x, directionLS.x, -halfWidth, halfWidth, ref tMin, ref tMax))
            return false;

        if (!ClipRayToAxisBounds(originLS.z, directionLS.z, -halfLength, halfLength, ref tMin, ref tMax))
            return false;

        if (!ClipRayToAxisBounds(originLS.y, directionLS.y, -0.01f, maxHeight + 0.01f, ref tMin, ref tMax))
            return false;

        float previousT = tMin;
        float previousDiff = float.MaxValue;

        for (int step = 0; step <= heightRaymarchSteps; step++)
        {
            float stepT = Mathf.Lerp(tMin, tMax, step / (float)heightRaymarchSteps);
            Vector3 pointLS = originLS + directionLS * stepT;
            Vector2 sampleUV = LocalPointToUV(pointLS, halfWidth, halfLength);
            float surfaceHeight = SampleLengthHeight(sampleUV);
            float diff = pointLS.y - surfaceHeight;

            if (step > 0 && previousDiff > 0f && diff <= 0f)
            {
                float lowT = previousT;
                float highT = stepT;

                for (int refine = 0; refine < heightRaymarchRefinementSteps; refine++)
                {
                    float midT = (lowT + highT) * 0.5f;
                    Vector3 midPointLS = originLS + directionLS * midT;
                    Vector2 midUV = LocalPointToUV(midPointLS, halfWidth, halfLength);
                    float midDiff = midPointLS.y - SampleLengthHeight(midUV);

                    if (midDiff > 0f)
                        lowT = midT;
                    else
                        highT = midT;
                }

                Vector3 refinedPointLS = originLS + directionLS * ((lowT + highT) * 0.5f);
                uv = LocalPointToUV(refinedPointLS, halfWidth, halfLength);
                return true;
            }

            previousT = stepT;
            previousDiff = diff;
        }

        return false;
    }

    private bool ClipRayToAxisBounds(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
    {
        if (Mathf.Abs(direction) < 0.00001f)
            return origin >= min && origin <= max;

        float invDirection = 1f / direction;
        float axisT0 = (min - origin) * invDirection;
        float axisT1 = (max - origin) * invDirection;

        if (axisT0 > axisT1)
        {
            float temp = axisT0;
            axisT0 = axisT1;
            axisT1 = temp;
        }

        tMin = Mathf.Max(tMin, axisT0);
        tMax = Mathf.Min(tMax, axisT1);
        return tMax >= tMin;
    }

    private Vector2 LocalPointToUV(Vector3 pointLS, float halfWidth, float halfLength)
    {
        float u = Mathf.InverseLerp(-halfWidth, halfWidth, pointLS.x);
        float v = Mathf.InverseLerp(-halfLength, halfLength, pointLS.z);
        return new Vector2(u, v);
    }

    private float SampleLengthHeight(Vector2 uv)
    {
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);

        if (lengthMapReadback == null)
            return 0f;

        return lengthMapReadback.GetPixelBilinear(uv.x, uv.y).r * Mathf.Max(0f, hairRenderMaterial.GetFloat(lengthMultiplierProperty));
    }

    public void SetToolGrow() => currentTool = ToolMode.Grow;
    public void SetToolCut() => currentTool = ToolMode.Cut;
    public void SetToolColor() => currentTool = ToolMode.Color;
    public void SetToolMode(ToolMode value) => currentTool = value;

    public void SetBrushRadius(float value) => brushRadius = Mathf.Max(0.0001f, value);
    public void SetBrushStrength(float value) => brushStrength = Mathf.Max(0f, value);
    public void SetBrushFalloff(float value) => brushFalloff = Mathf.Clamp01(value);
    public void SetHeightSmoothness(float value) => heightSmoothness = Mathf.Clamp01(value);
    public void SetContinuousHeightPaintRate(float value) => continuousHeightPaintRate = Mathf.Max(0f, value);
    public void SetHeightStepLimit(float value) => heightStepLimit = Mathf.Clamp(value, 0.001f, 1f);
    public void SetBrushColor(Color value) => brushColor = value;

    public void ClearLengthMapToBlack()
    {
        if (lengthMap != null)
            ClearRenderTexture(lengthMap, Color.black);
    }

    public void ClearLengthMapToWhite()
    {
        if (lengthMap != null)
            ClearRenderTexture(lengthMap, Color.white);
    }

    public void ClearColorMapToWhite()
    {
        if (colorMap != null)
            ClearRenderTexture(colorMap, Color.white);
    }

    public void ClearColorMapToColor(Color color)
    {
        if (colorMap != null)
            ClearRenderTexture(colorMap, color);
    }

    private Vector3 UVToWorldPosition(Vector2 uv, RaycastHit hit)
    {
        Renderer renderer = hit.collider.GetComponent<Renderer>();
        if (renderer == null)
            return hit.point;

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return hit.point;

        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        int triangleIndex = hit.triangleIndex * 3;

        Vector3 v0 = vertices[triangles[triangleIndex]];
        Vector3 v1 = vertices[triangles[triangleIndex + 1]];
        Vector3 v2 = vertices[triangles[triangleIndex + 2]];

        Vector2 uv0 = uvs[triangles[triangleIndex]];
        Vector2 uv1 = uvs[triangles[triangleIndex + 1]];
        Vector2 uv2 = uvs[triangles[triangleIndex + 2]];

        Vector3 bary = hit.barycentricCoordinate;

        Vector3 worldPos =
            hit.collider.transform.TransformPoint(
                v0 * bary.x + v1 * bary.y + v2 * bary.z
            );

        return worldPos;
    }
}
