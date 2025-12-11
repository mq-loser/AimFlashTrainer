using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    public float size = 8f;
    public Color color = Color.white;

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;
        float half = size * 0.5f;

        Color oldColor = GUI.color;
        GUI.color = color;

        // 横线
        GUI.DrawTexture(new Rect(x - half, y, size, 1), Texture2D.whiteTexture);
        // 竖线
        GUI.DrawTexture(new Rect(x, y - half, 1, size), Texture2D.whiteTexture);

        GUI.color = oldColor;
    }
}

