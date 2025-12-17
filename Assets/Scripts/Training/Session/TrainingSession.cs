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

    public int flashesTotal { get; private set; }
    public int flashesFront { get; private set; }
    public int flashesSide { get; private set; }
    public int flashesBack { get; private set; }
    public float lastFlashIntensity01 { get; private set; }
    public float lastFlashAngleDegrees { get; private set; }

    public float timeRemaining { get; private set; }
    public bool isRunning { get; private set; }
    public bool isFinished { get; private set; }
    public bool isPaused { get; private set; }

    public float accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired : 0f;
    public float backFlashRate => flashesTotal > 0 ? (float)flashesBack / flashesTotal : 0f;

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
        flashesTotal = 0;
        flashesFront = 0;
        flashesSide = 0;
        flashesBack = 0;
        lastFlashIntensity01 = 0f;
        lastFlashAngleDegrees = 0f;

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

    public void RegisterFlash(FlashExposure exposure, float intensity01, float angleDegrees)
    {
        if (!isRunning || isPaused || isFinished)
        {
            return;
        }

        flashesTotal++;
        switch (exposure)
        {
            case FlashExposure.Front:
                flashesFront++;
                break;
            case FlashExposure.Side:
                flashesSide++;
                break;
            case FlashExposure.Back:
                flashesBack++;
                break;
        }

        lastFlashIntensity01 = Mathf.Clamp01(intensity01);
        lastFlashAngleDegrees = Mathf.Clamp(angleDegrees, 0f, 180f);
    }
}
