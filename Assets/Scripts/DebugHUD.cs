using UnityEngine;
using System.Collections.Generic;

public class DebugHUD : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerController player;
    public List<WardenAI> wardens = new List<WardenAI>();

    [Header("Estilo")]
    public int fontSize = 16;
    public Color textColor = Color.white;
    public Color bgColor = new Color(0, 0, 0, 0.6f);

    [Header("Margenes (pixeles desde el borde)")]
    public int marginX = 20;
    public int marginY = 20;
    public int panelWidth = 260;
    public int panelPadding = 12;

    private GUIStyle labelStyle;
    private GUIStyle bgStyle;
    private Texture2D bgTex;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerController>();
        }

        if (wardens.Count == 0)
        {
            wardens.AddRange(FindObjectsByType<WardenAI>(FindObjectsSortMode.None));
        }
    }

    void InitStyles()
    {
        if (labelStyle != null) return;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.normal.textColor = textColor;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.wordWrap = false;

        bgTex = MakeTex(2, 2, bgColor);
        bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.normal.background = bgTex;
        bgStyle.border = new RectOffset(0, 0, 0, 0);
    }

    void OnGUI()
    {
        InitStyles();

        int lineH = fontSize + 6;

        // ===== Panel arriba izq: GameManager =====
        if (GameManager.Instance != null)
        {
            int lines = 4;
            int h = (panelPadding * 2) + (lines * lineH);
            Rect panel = new Rect(marginX, marginY, panelWidth, h);
            GUI.Box(panel, "", bgStyle);

            var gm = GameManager.Instance;
            float x = panel.x + panelPadding;
            float y = panel.y + panelPadding;
            DrawLine(x, ref y, lineH, "TIEMPO: " + gm.GetTimeFormatted());
            DrawLine(x, ref y, lineH, "PUNTOS: " + gm.currentScore + " / " + gm.targetScore);
            DrawLine(x, ref y, lineH, "MOMENTO: " + gm.dayState);
            DrawLine(x, ref y, lineH, "ESTADO: " + gm.gameState);
        }

        // ===== Panel arriba der: Player =====
        if (player != null)
        {
            int lines = 3;
            int h = (panelPadding * 2) + (lines * lineH);
            Rect panel = new Rect(Screen.width - panelWidth - marginX, marginY, panelWidth, h);
            GUI.Box(panel, "", bgStyle);

            float x = panel.x + panelPadding;
            float y = panel.y + panelPadding;
            DrawLine(x, ref y, lineH, "JUGADOR: " + player.currentState);
            DrawLine(x, ref y, lineH, "Ruido: " + player.currentNoiseRadius.ToString("F1") + "m");
            DrawLine(x, ref y, lineH, "Crouch: " + (player.isCrouching ? "SI" : "NO"));
        }

        // ===== Panel abajo izq: Wardens =====
        if (wardens.Count > 0)
        {
            int lines = wardens.Count + 1; // +1 por el header
            int h = (panelPadding * 2) + (lines * lineH);
            Rect panel = new Rect(marginX, Screen.height - h - marginY, panelWidth, h);
            GUI.Box(panel, "", bgStyle);

            float x = panel.x + panelPadding;
            float y = panel.y + panelPadding;
            DrawLine(x, ref y, lineH, "WARDENS:");
            for (int i = 0; i < wardens.Count; i++)
            {
                if (wardens[i] == null) continue;
                DrawLine(x, ref y, lineH, "  #" + (i + 1) + ": " + wardens[i].currentState);
            }
        }

        // ===== Pantalla Game Over / Victory =====
        if (GameManager.Instance != null)
        {
            var gs = GameManager.Instance.gameState;
            if (gs == GameManager.GameState.Victory)
            {
                DrawEndScreen(gs);
            }
        }
    }

    void DrawLine(float x, ref float y, int lineH, string text)
    {
        GUI.Label(new Rect(x, y, panelWidth - (panelPadding * 2), lineH), text, labelStyle);
        y += lineH;
    }

    void DrawEndScreen(GameManager.GameState state)
    {
        // fondo negro semi-transparente sobre toda la pantalla
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string title = state == GameManager.GameState.Victory ? "VICTORIA" : "GAME OVER";
        Color color = state == GameManager.GameState.Victory ? Color.green : Color.red;

        // titulo grande centrado
        GUIStyle bigStyle = new GUIStyle(GUI.skin.label);
        bigStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.08f); // escalado al alto
        bigStyle.fontStyle = FontStyle.Bold;
        bigStyle.alignment = TextAnchor.MiddleCenter;
        bigStyle.normal.textColor = color;

        Rect titleRect = new Rect(0, Screen.height * 0.3f, Screen.width, Screen.height * 0.15f);
        GUI.Label(titleRect, title, bigStyle);

        // boton reiniciar centrado
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.025f);

        float btnW = 220;
        float btnH = 60;
        Rect btnRect = new Rect(
            (Screen.width - btnW) / 2f,
            Screen.height * 0.55f,
            btnW,
            btnH
        );

        if (GUI.Button(btnRect, "Reiniciar", btnStyle))
        {
            GameManager.Instance.RestartLevel();
        }
    }

    Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(w, h);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
