using UnityEngine;

/// <summary>
/// GDD 4.1 Dynamic Camera: centers on the midpoint of human + ghost, zooms out as they separate,
/// clamps at a maximum zoom-out, and keeps a configurable buffer (X) between both players and the screen edge.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DynamicCamera : MonoBehaviour
{
    public static DynamicCamera Instance { get; private set; }
    [Header("Screen Shake")]
    public float hitShakeStrength = 0.25f;   // world units at closest zoom
    public float hitShakeDuration = 0.18f;   // seconds (unscaled)

    Vector3 basePos;                          // smoothed position, without shake
    float shakeTimer, shakeDuration, shakeStrength;

    [Header("Zoom (orthographic size = half the visible height, in world units / meters)")]
    public float minOrthographicSize = 6f;
    public float maxOrthographicSize = 14f;

    [Tooltip("X: minimum distance (world units / meters) between either player and the visible screen edge.")]
    public float screenEdgeBuffer = 2.5f;

    [Header("Smoothing (seconds)")]
    public float positionSmoothTime = 0.12f;
    public float zoomOutSmoothTime = 0.15f;
    public float zoomInSmoothTime = 0.45f;  

    Camera cam;
    Transform human, ghost;
    Vector3 posVelocity;
    float zoomVelocity;

    // Lazy accessor: GameManager.Awake can run BEFORE this component's Awake, so never rely on Awake having run.
    Camera C
    {
        get
        {
            if (cam == null) cam = GetComponent<Camera>();
            Instance = this;
            return cam;
        }
    }

    public Camera Cam => C;
    public float HalfHeight => C.orthographicSize;
    public float HalfWidth => C.orthographicSize * C.aspect;
    /// <summary>1 at the closest zoom; grows as the camera zooms out.</summary>
    public float ZoomScale => C.orthographicSize / Mathf.Max(0.01f, minOrthographicSize);

    /// <summary>The currently visible world rectangle (after zoom). Enemy spawning uses this.</summary>
    public Rect VisibleRect
    {
        get
        {
            Vector2 c = transform.position;
            return new Rect(c.x - HalfWidth, c.y - HalfHeight, HalfWidth * 2f, HalfHeight * 2f);
        }
    }

    /// <summary>Largest per-axis human-ghost separation that still fits in the max zoom with the edge buffer.</summary>
    public Vector2 MaxSeparation
    {
        get
        {
            float w = Mathf.Max(0f, 2f * (maxOrthographicSize * C.aspect - screenEdgeBuffer)) * 0.98f;
            float h = Mathf.Max(0f, 2f * (maxOrthographicSize - screenEdgeBuffer)) * 0.98f;
            return new Vector2(w, h);
        }
    }

    void Awake() { var _ = C; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Init(Transform humanTransform, Transform ghostTransform)
    {
        human = humanTransform;
        ghost = ghostTransform;
        C.orthographic = true;
        posVelocity = Vector3.zero;
        zoomVelocity = 0f;
       ComputeTarget(out Vector2 mid, out float size);
        C.orthographicSize = size;
        basePos = new Vector3(mid.x, mid.y, -10f);
        transform.position = basePos;
        shakeTimer = 0f;
    }

    void ComputeTarget(out Vector2 mid, out float size)
    {
        Vector2 h = human.position, g = ghost.position;
        mid = (h + g) * 0.5f;
        float needHalfW = Mathf.Abs(h.x - g.x) * 0.5f + screenEdgeBuffer;
        float needHalfH = Mathf.Abs(h.y - g.y) * 0.5f + screenEdgeBuffer;
        float required = Mathf.Max(needHalfH, needHalfW / C.aspect);
        size = Mathf.Clamp(required, minOrthographicSize, maxOrthographicSize);
    }

    void LateUpdate()
    {
        if (human == null || ghost == null) return;
        ComputeTarget(out Vector2 mid, out float targetSize);

        float smooth = targetSize > C.orthographicSize ? zoomOutSmoothTime : zoomInSmoothTime;
        C.orthographicSize = Mathf.SmoothDamp(C.orthographicSize, targetSize, ref zoomVelocity, smooth);
        basePos = Vector3.SmoothDamp(basePos, new Vector3(mid.x, mid.y, -10f), ref posVelocity, positionSmoothTime);

        transform.position = basePos + ShakeOffset();
    }

    /// <summary>Triggers a short screen shake. Uses Inspector defaults if no values are passed.</summary>
    public void Shake(float strength = -1f, float duration = -1f)
    {
        if (strength < 0f) strength = hitShakeStrength;
        if (duration < 0f) duration = hitShakeDuration;

        // Don't let a weak shake cut off a stronger one that's still playing
        float remaining = shakeTimer > 0f ? shakeStrength * (shakeTimer / shakeDuration) : 0f;
        shakeStrength = Mathf.Max(remaining, strength);
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;
    }

    Vector3 ShakeOffset()
    {
        if (shakeTimer <= 0f) return Vector3.zero;
        shakeTimer -= Time.unscaledDeltaTime;   // unaffected by the 0.35 death slow-mo
        float k = Mathf.Clamp01(shakeTimer / shakeDuration);
        float amp = shakeStrength * k * k * ZoomScale;   // eases out; scales with zoom so it reads the same zoomed out
        return (Vector3)(Random.insideUnitCircle * amp);
    }

    /// <summary>Mouse position -> world position, clamped inside the safe (buffered) part of the screen.</summary>
    public Vector2 ScreenToWorldClamped(Vector2 screenPos)
    {
        Vector3 w = C.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -transform.position.z));
        Vector2 c = transform.position;
        float hw = Mathf.Max(0f, HalfWidth - screenEdgeBuffer);
        float hh = Mathf.Max(0f, HalfHeight - screenEdgeBuffer);
        return new Vector2(Mathf.Clamp(w.x, c.x - hw, c.x + hw), Mathf.Clamp(w.y, c.y - hh, c.y + hh));
    }

    /// <summary>Keeps the ghost within what the max zoom-out can show alongside the human.</summary>
    public Vector2 ClampToTether(Vector2 humanPos, Vector2 ghostPos)
    {
        Vector2 s = MaxSeparation;
        Vector2 d = ghostPos - humanPos;
        d.x = Mathf.Clamp(d.x, -s.x, s.x);
        d.y = Mathf.Clamp(d.y, -s.y, s.y);
        return humanPos + d;
    }
}