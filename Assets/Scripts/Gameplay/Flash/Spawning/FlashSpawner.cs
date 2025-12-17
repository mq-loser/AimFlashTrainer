using UnityEngine;

public class FlashSpawner : MonoBehaviour
{
    [Header("Refs")]
    public TrainingSession session;
    public Camera playerCamera;
    public FlashOverlay overlay;

    [Header("Rules")]
    public bool spawnOnlyWhileSessionRunning = true;
    public bool spawnWhilePaused = false;
    public bool clearOnSessionReset = true;
    public bool showIndicator = true;

    [Min(0.1f)]
    public float minIntervalSeconds = 2.5f;
    [Min(0.1f)]
    public float maxIntervalSeconds = 5f;
    [Min(0.01f)]
    public float fuseSeconds = 0.6f;

    [Header("Spawn Space (relative to camera)")]
    [Min(0.1f)]
    public float minDistance = 6f;
    [Min(0.1f)]
    public float maxDistance = 12f;
    [Range(0f, 180f)]
    public float horizontalAngleRangeDegrees = 75f;
    public float heightOffset = 0f;
    [Min(0f)]
    public float heightVariance = 0.6f;

    [Header("Exposure (angle from view)")]
    [Range(0f, 180f)]
    public float fullFlashAngleDegrees = 25f;
    [Range(0f, 180f)]
    public float backFlashAngleDegrees = 80f;
    public bool applyDistanceFalloff = true;

    [Header("Indicator (optional)")]
    public GameObject indicatorPrefab;
    public Color indicatorColor = new Color(1f, 0.9f, 0.2f, 1f);
    [Min(0.01f)]
    public float indicatorScale = 0.2f;

    float timeUntilNext;
    float fuseRemaining;
    bool hasPendingFlash;
    Vector3 pendingPosition;
    GameObject indicatorInstance;

    int lastSessionRevision;
    bool attemptedResolveSession;

    void Awake()
    {
        TryResolveRefs();
        ScheduleNext();
    }

    void Start()
    {
        TryResolveRefs();
    }

    void Update()
    {
        TryResolveRefs();

        if (session != null && clearOnSessionReset && session.revision != lastSessionRevision)
        {
            lastSessionRevision = session.revision;
            ResetSpawnerState();
        }

        if (!CanTick())
        {
            return;
        }

        if (!hasPendingFlash)
        {
            timeUntilNext -= Time.deltaTime;
            if (timeUntilNext <= 0f)
            {
                SpawnFlash();
            }
            return;
        }

        fuseRemaining -= Time.deltaTime;
        if (fuseRemaining <= 0f)
        {
            Detonate();
        }
    }

    bool CanTick()
    {
        if (!spawnOnlyWhileSessionRunning)
        {
            return true;
        }

        if (session == null)
        {
            return false;
        }

        if (!session.isRunning || session.isFinished)
        {
            return false;
        }

        if (session.isPaused && !spawnWhilePaused)
        {
            return false;
        }

        return true;
    }

    void TryResolveRefs()
    {
        if (session == null)
        {
            if (!attemptedResolveSession || TrainingSession.Instance != null)
            {
                attemptedResolveSession = true;
                session = TrainingSession.Instance != null ? TrainingSession.Instance : FindObjectOfType<TrainingSession>();
                if (session != null)
                {
                    lastSessionRevision = session.revision;
                }
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (overlay == null)
        {
            overlay = FindObjectOfType<FlashOverlay>();
        }
    }

    void ResetSpawnerState()
    {
        hasPendingFlash = false;
        fuseRemaining = 0f;
        pendingPosition = default(Vector3);
        DestroyIndicator();
        ScheduleNext();
    }

    void ScheduleNext()
    {
        float min = Mathf.Min(minIntervalSeconds, maxIntervalSeconds);
        float max = Mathf.Max(minIntervalSeconds, maxIntervalSeconds);
        timeUntilNext = Random.Range(min, max);
    }

    void SpawnFlash()
    {
        if (playerCamera == null)
        {
            ScheduleNext();
            return;
        }

        pendingPosition = GetRandomPosition();
        hasPendingFlash = true;
        fuseRemaining = Mathf.Max(0.01f, fuseSeconds);

        if (!showIndicator)
        {
            return;
        }

        indicatorInstance = indicatorPrefab != null
            ? Instantiate(indicatorPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        indicatorInstance.name = "FlashIndicator";
        indicatorInstance.transform.position = pendingPosition;
        indicatorInstance.transform.localScale = Vector3.one * indicatorScale;

        foreach (var col in indicatorInstance.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }

        var renderer = indicatorInstance.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = indicatorColor;
        }
    }

    Vector3 GetRandomPosition()
    {
        Vector3 origin = playerCamera.transform.position;

        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        float yaw = Random.Range(-horizontalAngleRangeDegrees, horizontalAngleRangeDegrees);
        Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * forward;

        float distMin = Mathf.Min(minDistance, maxDistance);
        float distMax = Mathf.Max(minDistance, maxDistance);
        float distance = Random.Range(distMin, distMax);

        Vector3 pos = origin + dir * distance;
        pos.y = origin.y + heightOffset + Random.Range(-heightVariance, heightVariance);
        return pos;
    }

    void Detonate()
    {
        if (playerCamera == null)
        {
            DestroyIndicator();
            hasPendingFlash = false;
            ScheduleNext();
            return;
        }

        Vector3 vp = playerCamera.WorldToViewportPoint(pendingPosition);
        bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

        EvaluateFlash(pendingPosition, onScreen, out float intensity01, out FlashExposure exposure, out float angleDegrees);

        if (overlay == null)
        {
            overlay = FindObjectOfType<FlashOverlay>();
            if (overlay == null)
            {
                overlay = gameObject.AddComponent<FlashOverlay>();
            }
        }

        overlay.Play(intensity01, exposure, new Vector2(vp.x, vp.y), onScreen);
        session?.RegisterFlash(exposure, intensity01, angleDegrees);

        DestroyIndicator();
        hasPendingFlash = false;
        ScheduleNext();
    }

    void EvaluateFlash(Vector3 flashPosition, bool isOnScreen, out float intensity01, out FlashExposure exposure, out float angleDegrees)
    {
        Vector3 camPos = playerCamera.transform.position;
        Vector3 toFlash = flashPosition - camPos;

        float distance = toFlash.magnitude;
        Vector3 toDir = distance > 0.0001f ? (toFlash / distance) : playerCamera.transform.forward;

        angleDegrees = Vector3.Angle(playerCamera.transform.forward, toDir);

        // Valorant-like simplification: if the flash is visible on-screen, it's a full flash regardless of
        // where it appears in the viewport. If it's off-screen, treat it as a "back flash" success.
        if (isOnScreen)
        {
            exposure = FlashExposure.Front;
            intensity01 = 1f;
        }
        else
        {
            exposure = FlashExposure.Back;
            intensity01 = 0f;
        }
    }

    void DestroyIndicator()
    {
        if (indicatorInstance == null)
        {
            return;
        }

        Destroy(indicatorInstance);
        indicatorInstance = null;
    }
}
