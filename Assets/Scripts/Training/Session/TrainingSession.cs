using UnityEngine;

public class TrainingSession : MonoBehaviour
{
    public static TrainingSession Instance { get; private set; }

    [Header("Session")]
    [Min(1f)]
    public float durationSeconds = 60f;
    public bool autoStartOnPlay = false;

    [Header("Pause")]
    public bool pauseWhenCursorUnlocked = true;
    public bool freezeTimeScaleWhenPaused = true;

    public int shotsFired { get; private set; }
    public int shotsHit { get; private set; }
    public int revision { get; private set; }

    public float timeRemaining { get; private set; }
    public bool isRunning { get; private set; }
    public bool isFinished { get; private set; }
    public bool isPaused { get; private set; }

    public float accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired : 0f;

    float timeScaleBeforePause = 1f;
    bool isTimeScaleFrozen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        ResetSession();
        if (autoStartOnPlay)
        {
            StartSession();
        }
    }

    void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
    }

    void OnDestroy()
    {
        RestoreTimeScaleIfNeeded();
    }

    void Update()
    {
        if (!isRunning || isPaused || isFinished)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndSession();
        }
    }

    public void ResetSession()
    {
        RestoreTimeScaleIfNeeded();
        revision++;
        shotsFired = 0;
        shotsHit = 0;

        timeRemaining = durationSeconds;
        isRunning = false;
        isFinished = false;
        isPaused = false;
    }

    public void StartSession()
    {
        if (isFinished)
        {
            return;
        }

        RestoreTimeScaleIfNeeded();
        isRunning = true;
        isPaused = false;
    }

    public void EndSession()
    {
        RestoreTimeScaleIfNeeded();
        isRunning = false;
        isFinished = true;
        isPaused = false;
    }

    public void SetPaused(bool paused)
    {
        if (!pauseWhenCursorUnlocked)
        {
            return;
        }

        if (!isRunning || isFinished)
        {
            return;
        }

        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;

        if (!freezeTimeScaleWhenPaused)
        {
            return;
        }

        if (paused)
        {
            if (!isTimeScaleFrozen)
            {
                timeScaleBeforePause = Time.timeScale;
                isTimeScaleFrozen = true;
            }
            Time.timeScale = 0f;
        }
        else
        {
            RestoreTimeScaleIfNeeded();
        }
    }

    void RestoreTimeScaleIfNeeded()
    {
        if (!freezeTimeScaleWhenPaused || !isTimeScaleFrozen)
        {
            return;
        }

        Time.timeScale = timeScaleBeforePause;
        isTimeScaleFrozen = false;
    }

    public void RegisterShot()
    {
        if (!isRunning || isPaused || isFinished)
        {
            return;
        }

        shotsFired++;
    }

    public void RegisterHit()
    {
        if (!isRunning || isPaused || isFinished)
        {
            return;
        }

        shotsHit++;
    }
}
