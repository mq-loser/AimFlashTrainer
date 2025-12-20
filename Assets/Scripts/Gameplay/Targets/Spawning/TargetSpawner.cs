using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [Header("Refs")]
    public TrainingSession session;

    [Header("Spawn Rules")]
    public bool spawnOnlyWhileSessionRunning = true;
    public bool spawnWhilePaused = false;
    public bool clearTargetsOnSessionReset = true;
    public bool fillToMaxOnSessionStart = true;

    [Min(1)]
    public int maxAliveTargets = 3;

    [Min(0.01f)]
    public float spawnIntervalSeconds = 0.5f;

    [Tooltip("0 = never despawn automatically")]
    [Min(0f)]
    public float targetLifetimeSeconds = 0f;

    [Header("Spawn Area (World)")]
    public Vector3 areaSize = new Vector3(10f, 2f, 10f);

    [Header("Target")]
    public GameObject targetPrefab;
    public PrimitiveType fallbackPrimitive = PrimitiveType.Cube;
    [Min(0.01f)]
    public float targetScaleMin = 0.35f;
    [Min(0.01f)]
    public float targetScaleMax = 0.75f;

    readonly List<GameObject> activeTargets = new();
    float spawnTimer;
    bool lastRunning;
    int lastSessionRevision;
    bool attemptedResolveSession;

    void Awake()
    {
        TryResolveSession();
    }

    void Start()
    {
        TryResolveSession();
    }

    void Update()
    {
        TryResolveSession();
        CleanupDestroyedTargets();

        if (session != null)
        {
            if (clearTargetsOnSessionReset && session.revision != lastSessionRevision)
            {
                lastSessionRevision = session.revision;
                ClearAllTargets();
                spawnTimer = 0f;
            }

            bool startedThisFrame = session.isRunning && !lastRunning;
            lastRunning = session.isRunning;

            if (fillToMaxOnSessionStart && startedThisFrame)
            {
                FillToMax();
            }
        }

        if (!CanSpawn())
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        while (spawnTimer >= spawnIntervalSeconds && activeTargets.Count < maxAliveTargets)
        {
            spawnTimer -= spawnIntervalSeconds;
            SpawnOne();
        }
    }

    bool CanSpawn()
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

    void TryResolveSession()
    {
        if (session != null)
        {
            return;
        }

        // Avoid running FindObjectOfType every frame if there's no session in the scene.
        // We'll still retry once in case the session gets created later.
        if (attemptedResolveSession && TrainingSession.Instance == null)
        {
            return;
        }

        attemptedResolveSession = true;

        session = TrainingSession.Instance;
        if (session == null)
        {
            session = FindObjectOfType<TrainingSession>();
        }

        if (session != null)
        {
            lastRunning = session.isRunning;
            lastSessionRevision = session.revision;

            if (fillToMaxOnSessionStart && session.isRunning)
            {
                FillToMax();
            }
        }
    }

    void FillToMax()
    {
        CleanupDestroyedTargets();

        while (activeTargets.Count < maxAliveTargets)
        {
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        GameObject go = targetPrefab != null
            ? Instantiate(targetPrefab)
            : GameObject.CreatePrimitive(fallbackPrimitive);

        go.name = "Target";

        float min = Mathf.Min(targetScaleMin, targetScaleMax);
        float max = Mathf.Max(targetScaleMin, targetScaleMax);
        float scale = Random.Range(min, max);
        go.transform.localScale = Vector3.one * scale;

        // Collider.bounds can be stale for the current frame right after scaling/instantiating.
        // SyncTransforms ensures the physics representation matches the Transform so padding is correct.
        Physics.SyncTransforms();

        Vector3 padding = GetSpawnPadding(go);
        go.transform.position = GetRandomPointInArea(padding);

        if (go.GetComponent<Target>() == null)
        {
            go.AddComponent<Target>();
        }

        activeTargets.Add(go);

        if (targetLifetimeSeconds > 0f)
        {
            Destroy(go, targetLifetimeSeconds);
        }
    }

    Vector3 GetRandomPointInArea(Vector3 padding)
    {
        Vector3 half = areaSize * 0.5f;

        float x = SampleAxis(half.x, padding.x);
        float y = SampleAxis(half.y, padding.y);
        float z = SampleAxis(half.z, padding.z);

        return new Vector3(
            transform.position.x + x,
            transform.position.y + y,
            transform.position.z + z
        );
    }

    static float SampleAxis(float halfExtent, float padding)
    {
        float usable = Mathf.Max(0f, halfExtent - Mathf.Max(0f, padding));
        if (usable <= 0f)
        {
            return 0f;
        }

        return Random.Range(-usable, usable);
    }

    static Vector3 GetSpawnPadding(GameObject go)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>();
        bool hasBounds = false;
        Bounds bounds = default(Bounds);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (hasBounds)
        {
            return bounds.extents;
        }

        Renderer renderer = go.GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.bounds.extents : Vector3.zero;
    }

    void CleanupDestroyedTargets()
    {
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            if (activeTargets[i] == null)
            {
                activeTargets.RemoveAt(i);
            }
        }
    }

    public void ClearAllTargets()
    {
        for (int i = 0; i < activeTargets.Count; i++)
        {
            if (activeTargets[i] != null)
            {
                Destroy(activeTargets[i]);
            }
        }
        activeTargets.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
