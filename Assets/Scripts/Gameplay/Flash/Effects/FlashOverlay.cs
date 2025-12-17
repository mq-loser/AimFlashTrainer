using UnityEngine;

public class FlashOverlay : MonoBehaviour
{
    [Header("Base Overlay")]
    public Color unseenTint = new Color(0.15f, 0.75f, 0.25f, 1f);
    public Color fullyFlashedTint = Color.white;
    [Range(0f, 1f)]
    public float unseenMaxAlpha = 0.35f;
    [Range(0f, 1f)]
    public float flashedMaxAlpha = 1f;

    [Header("Timing")]
    [Min(0.01f)]
    public float unseenDurationSeconds = 0.12f;
    [Min(0.01f)]
    public float minFlashedDurationSeconds = 1.5f;
    [Min(0.01f)]
    public float maxFlashedDurationSeconds = 2.5f;
    [Range(0f, 1f)]
    public float holdFraction = 0.15f;

    [Header("Burst (when flash is visible)")]
    public bool enableBurst = true;
    public Color burstColor = new Color(1f, 0.9f, 0.2f, 1f);
    [Range(0f, 1f)]
    public float burstMaxAlpha = 1f;
    [Min(0f)]
    public float burstMinSize = 140f;
    [Min(0f)]
    public float burstMaxSize = 520f;
    [Min(0)]
    public int burstRays = 18;
    [Min(0.5f)]
    public float burstRayThickness = 2f;
    [Min(0f)]
    public float burstRayLengthMultiplier = 1.15f;

    float durationSeconds;
    float remainingSeconds;
    float strength01;
    float maxAlpha;
    Color baseColor;

    bool showBurst;
    Vector2 burstViewportPos;
    float burstRotation;

    public bool isPlaying => remainingSeconds > 0f && strength01 > 0f;

    public void Play(float intensity01, FlashExposure exposure, Vector2 viewportPos, bool isOnScreen)
    {
        bool visible = isOnScreen && exposure != FlashExposure.Back;

        if (!visible)
        {
            strength01 = 1f;
            baseColor = unseenTint;
            maxAlpha = unseenMaxAlpha;
            durationSeconds = Mathf.Max(0.01f, unseenDurationSeconds);
            remainingSeconds = durationSeconds;
            showBurst = false;
            return;
        }

        strength01 = Mathf.Clamp01(intensity01);
        float t = Mathf.Pow(strength01, 0.6f);
        baseColor = Color.Lerp(unseenTint, fullyFlashedTint, t);
        maxAlpha = flashedMaxAlpha;
        float minD = Mathf.Min(minFlashedDurationSeconds, maxFlashedDurationSeconds);
        float maxD = Mathf.Max(minFlashedDurationSeconds, maxFlashedDurationSeconds);
        durationSeconds = Random.Range(minD, maxD);
        durationSeconds = Mathf.Max(0.01f, durationSeconds);
        remainingSeconds = durationSeconds;

        showBurst = enableBurst && isOnScreen && strength01 > 0.05f;
        burstViewportPos = viewportPos;
        burstRotation = Random.Range(0f, 360f);
    }

    void Update()
    {
        if (remainingSeconds <= 0f)
        {
            return;
        }

        remainingSeconds -= Time.deltaTime;
        if (remainingSeconds < 0f)
        {
            remainingSeconds = 0f;
        }
    }

    void OnGUI()
    {
        if (!isPlaying)
        {
            return;
        }

        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        float progress01 = 1f - (remainingSeconds / durationSeconds);
        float alpha01;
        if (progress01 <= holdFraction)
        {
            alpha01 = 1f;
        }
        else
        {
            float t = Mathf.InverseLerp(holdFraction, 1f, progress01);
            alpha01 = 1f - t;
        }

        Color old = GUI.color;
        Color baseC = baseColor;
        baseC.a = Mathf.Clamp01(alpha01 * strength01 * maxAlpha);
        GUI.color = baseC;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        if (showBurst)
        {
            DrawBurst(alpha01);
        }

        GUI.color = old;
    }

    void DrawBurst(float alpha01)
    {
        Vector2 center = new Vector2(
            burstViewportPos.x * Screen.width,
            (1f - burstViewportPos.y) * Screen.height
        );

        float size = Mathf.Lerp(burstMinSize, burstMaxSize, Mathf.Clamp01(strength01));
        float fadeScale = Mathf.Lerp(1f, 0.85f, 1f - alpha01);
        size *= fadeScale;

        float burstAlpha = Mathf.Clamp01(alpha01 * strength01 * burstMaxAlpha);
        if (burstAlpha <= 0f || size <= 0f)
        {
            return;
        }

        Texture2D radial = GetRadialTexture();
        if (radial != null)
        {
            Color old = GUI.color;
            Color c = burstColor;
            c.a = burstAlpha;
            GUI.color = c;
            GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), radial);
            GUI.color = old;
        }

        if (burstRays <= 0)
        {
            return;
        }

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;

        Color rayColor = burstColor;
        rayColor.a = burstAlpha * 0.7f;
        GUI.color = rayColor;

        float rayLength = size * burstRayLengthMultiplier;
        float step = 360f / burstRays;
        for (int i = 0; i < burstRays; i++)
        {
            GUI.matrix = oldMatrix;
            GUIUtility.RotateAroundPivot(burstRotation + (i * step), center);
            GUI.DrawTexture(
                new Rect(center.x - rayLength * 0.5f, center.y - burstRayThickness * 0.5f, rayLength, burstRayThickness),
                Texture2D.whiteTexture
            );
        }

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    static Texture2D radialTexture;

    static Texture2D GetRadialTexture()
    {
        if (radialTexture != null)
        {
            return radialTexture;
        }

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = Mathf.Pow(a, 3.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply(false, true);
        radialTexture = tex;
        return radialTexture;
    }
}
