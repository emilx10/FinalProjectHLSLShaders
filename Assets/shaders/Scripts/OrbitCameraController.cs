using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class OrbitCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Distance")]
    [SerializeField, Min(0.5f)] private float minDistance = 6f;
    [SerializeField, Min(1f)] private float maxDistance = 24f;
    [SerializeField, Min(0.01f)] private float zoomSpeed = 2.5f;

    [Header("Rotation")]
    [SerializeField, Min(0.01f)] private float rotationSensitivity = 0.2f;
    [SerializeField] private float minPitch = 15f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Pan")]
    [SerializeField, Min(0.01f)] private float panSpeed = 0.75f;
    [SerializeField, Min(0.001f)] private float dragPanScale = 0.02f;
    [SerializeField] private bool allowKeyboardPan = true;
    [SerializeField] private bool allowMiddleMousePan = true;

    [Header("Behavior")]
    [SerializeField] private bool blockPointerInputOverUI = true;
    [SerializeField] private bool initializeFromCurrentTransform = true;

    private float yaw;
    private float pitch;
    private float distance;
    private bool initialized;

    public static bool HasPointerCapture { get; private set; }

    private void Awake()
    {
        if (target == null)
        {
            SubdividedPlaneGenerator plane = FindObjectOfType<SubdividedPlaneGenerator>();
            if (plane != null)
                target = plane.transform;
        }

        InitializeState();
    }

    private void OnEnable()
    {
        InitializeState();
    }

    private void OnDisable()
    {
        HasPointerCapture = false;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        InitializeState();

        bool pointerOverUI = blockPointerInputOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        bool isRotating = mouse != null && mouse.rightButton.isPressed && !pointerOverUI;
        bool isPointerPanning = allowMiddleMousePan && mouse != null && mouse.middleButton.isPressed && !pointerOverUI;
        HasPointerCapture = isRotating || isPointerPanning;

        if (isRotating)
        {
            Vector2 lookDelta = mouse.delta.ReadValue();
            yaw += lookDelta.x * rotationSensitivity;
            pitch = Mathf.Clamp(pitch - lookDelta.y * rotationSensitivity, minPitch, maxPitch);
        }

        if (mouse != null && !pointerOverUI)
        {
            float scrollDelta = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) > 0.001f)
            {
                distance -= scrollDelta * zoomSpeed * 0.01f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }

        Vector2 panInput = Vector2.zero;

        if (allowKeyboardPan && keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                panInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                panInput.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                panInput.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                panInput.y += 1f;
        }

        if (isPointerPanning && mouse != null)
        {
            Vector2 dragDelta = mouse.delta.ReadValue();
            panInput += new Vector2(-dragDelta.x, -dragDelta.y) * dragPanScale;
        }

        if (panInput.sqrMagnitude > 1f)
            panInput.Normalize();

        if (panInput.sqrMagnitude > 0.0001f)
            PanTarget(panInput);

        ApplyCameraTransform();
    }

    private void InitializeState()
    {
        if (initialized || !initializeFromCurrentTransform || target == null)
            return;

        Vector3 offset = transform.position - target.position;
        float offsetMagnitude = offset.magnitude;

        if (offsetMagnitude <= 0.001f)
        {
            distance = Mathf.Clamp((minDistance + maxDistance) * 0.5f, minDistance, maxDistance);
            yaw = transform.eulerAngles.y;
            pitch = Mathf.Clamp(NormalizeAngle(transform.eulerAngles.x), minPitch, maxPitch);
            initialized = true;
            ApplyCameraTransform();
            return;
        }

        distance = Mathf.Clamp(offsetMagnitude, minDistance, maxDistance);
        Quaternion lookRotation = Quaternion.LookRotation((target.position - transform.position).normalized, Vector3.up);
        Vector3 eulerAngles = lookRotation.eulerAngles;
        yaw = eulerAngles.y;
        pitch = Mathf.Clamp(NormalizeAngle(eulerAngles.x), minPitch, maxPitch);
        initialized = true;
        ApplyCameraTransform();
    }

    private void PanTarget(Vector2 panInput)
    {
        Vector3 flattenedForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 flattenedRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        if (flattenedForward.sqrMagnitude < 0.0001f)
            flattenedForward = Vector3.forward;

        if (flattenedRight.sqrMagnitude < 0.0001f)
            flattenedRight = Vector3.right;

        float distanceFactor = Mathf.Max(1f, distance * 0.1f);
        Vector3 panOffset = (flattenedRight * panInput.x + flattenedForward * panInput.y) *
            (panSpeed * distanceFactor * Time.unscaledDeltaTime * 60f);

        target.position += panOffset;
    }

    private void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        transform.SetPositionAndRotation(target.position + offset, rotation);
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
