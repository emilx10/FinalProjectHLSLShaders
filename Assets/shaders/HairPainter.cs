using UnityEngine;
using UnityEngine.InputSystem;

public class HairPainter : MonoBehaviour
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
    [SerializeField] private float brushRadius = 0.05f;
    [SerializeField] private float brushStrength = 0.15f;
    [SerializeField] private Color brushColor = Color.red;

    [Header("Input")]
    [SerializeField] private bool paintContinuouslyWhileHolding = true;

    [Header("Shader Property Names")]
    [SerializeField] private string brushUVProperty = "_BrushUV";
    [SerializeField] private string brushRadiusProperty = "_BrushRadius";
    [SerializeField] private string brushStrengthProperty = "_BrushStrength";
    [SerializeField] private string brushColorProperty = "_BrushColor";
    [SerializeField] private string modeProperty = "_Mode";
    [SerializeField] private string lengthMapProperty = "_LengthMap";
    [SerializeField] private string colorMapProperty = "_ColorMap";

    [Header("Startup")]
    [SerializeField] private bool clearMapsOnAwake = true;
    [SerializeField] private Color initialLengthColor = Color.black; // black = shaved start
    [SerializeField] private Color initialColorColor = Color.white;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        EnsureRenderTextureCreated(lengthMap);
        EnsureRenderTextureCreated(colorMap);

        if (clearMapsOnAwake)
        {
            if (lengthMap != null)
                ClearRenderTexture(lengthMap, initialLengthColor);

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
                TryPaintAtMouse();
        }
        else
        {
            if (WasPaintPressedThisFrame())
                TryPaintAtMouse();
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

    private void TryPaintAtMouse()
    {
        if (targetCamera == null || Mouse.current == null || paintMaterial == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, paintLayerMask))
            return;

        Vector2 uv = hit.textureCoord;
        PaintAtUV(uv);
        Debug.Log("UV: " + uv);
    }

    public void PaintAtUV(Vector2 uv)
    {
        if (paintMaterial == null)
            return;

        paintMaterial.SetVector(brushUVProperty, new Vector4(uv.x, uv.y, 0f, 0f));
        paintMaterial.SetFloat(brushRadiusProperty, brushRadius);
        paintMaterial.SetFloat(brushStrengthProperty, brushStrength);
        paintMaterial.SetColor(brushColorProperty, brushColor);
        paintMaterial.SetFloat(modeProperty, (float)currentTool);

        switch (currentTool)
        {
            case ToolMode.Grow:
            case ToolMode.Cut:
                PaintIntoTarget(lengthMap);
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
    }

    public void SetToolGrow() => currentTool = ToolMode.Grow;
    public void SetToolCut() => currentTool = ToolMode.Cut;
    public void SetToolColor() => currentTool = ToolMode.Color;

    public void SetBrushRadius(float value) => brushRadius = Mathf.Max(0.0001f, value);
    public void SetBrushStrength(float value) => brushStrength = Mathf.Max(0f, value);
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
}