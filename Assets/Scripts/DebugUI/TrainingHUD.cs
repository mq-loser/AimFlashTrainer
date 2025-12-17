using UnityEngine;

public class TrainingHUD : MonoBehaviour
{
    public TrainingSession session;

    [Header("HUD")]
    public bool showWhileCursorUnlocked = true;
    public bool showControlsHint = true;

    void Awake()
    {
        if (session == null)
        {
            session = TrainingSession.Instance;
        }
    }

    void OnGUI()
    {
        if (session == null)
        {
            session = TrainingSession.Instance;
            if (session == null) return;
        }

        if (!showWhileCursorUnlocked && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        DrawTopLeftStats();
        DrawCenterOverlays();
    }

    void DrawTopLeftStats()
    {
        string status = session.isFinished
            ? "FINISHED"
            : session.isPaused
                ? "PAUSED"
                : session.isRunning
                    ? "RUNNING"
                    : "IDLE";

        string text =
            $"Time: {session.timeRemaining:0.0}s\n" +
            $"Hits: {session.shotsHit}\n" +
            $"Shots: {session.shotsFired}\n" +
            $"Acc: {session.accuracy * 100f:0.0}%\n" +
            $"Backflash: {session.flashesBack} / {session.flashesTotal} ({session.backFlashRate * 100f:0.0}%)\n" +
            $"Status: {status}";

        var rect = new Rect(12, 12, 280, 140);
        GUI.Box(rect, "");
        GUI.Label(new Rect(rect.x + 10, rect.y + 8, rect.width - 20, rect.height - 16), text);
    }

    void DrawCenterOverlays()
    {
        if (!showControlsHint) return;

        if (session.isFinished)
        {
            DrawCenterLabel($"Session finished\nHits: {session.shotsHit} / {session.shotsFired}\nPress R to reset");
            return;
        }

        if (!session.isRunning)
        {
            DrawCenterLabel("Click LMB/RMB to lock cursor and start\nEsc to unlock");
            return;
        }

        if (session.isPaused)
        {
            DrawCenterLabel("Paused (cursor unlocked)\nClick to resume");
        }
    }

    static void DrawCenterLabel(string text)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            wordWrap = true,
            normal = { textColor = Color.white },
        };

        float width = 420;
        float height = 90;
        var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(rect, "");
        GUI.Label(rect, text, style);
    }
}
