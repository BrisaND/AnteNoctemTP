using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum DayState { Dia, Tarde, Noche }
    public enum GameState { Playing, GameOver, Victory }

    [Header("Timer")]
    [Tooltip("Duracion total del nivel en segundos")]
    public float levelDuration = 180f; // 3 min default
    public float currentTime { get; private set; }

    [Header("Score")]
    public int targetScore = 100;
    public int currentScore { get; private set; }

    [Header("Estados")]
    public DayState dayState { get; private set; } = DayState.Dia;
    public GameState gameState { get; private set; } = GameState.Playing;

    [Header("Umbrales del dia (porcentaje del tiempo restante)")]
    [Range(0f, 1f)] public float tardeThreshold = 0.5f;
    [Range(0f, 1f)] public float nocheThreshold = 0.2f;

    // Eventos para que otros scripts reaccionen
    public System.Action<DayState> OnDayStateChanged;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<int> OnScoreChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        currentTime = levelDuration;
        currentScore = 0;
    }

    void Update()
    {
        if (gameState != GameState.Playing) return;

        currentTime -= Time.deltaTime;
        UpdateDayState();

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            // si llego a la noche sin la cuota, perdiste
            if (currentScore >= targetScore) Victory();
            else GameOver("Se hizo de noche");
        }
    }

    void UpdateDayState()
    {
        float pct = currentTime / levelDuration;
        DayState newState;
        if (pct > tardeThreshold) newState = DayState.Dia;
        else if (pct > nocheThreshold) newState = DayState.Tarde;
        else newState = DayState.Noche;

        if (newState != dayState)
        {
            dayState = newState;
            OnDayStateChanged?.Invoke(dayState);
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
        if (currentScore >= targetScore && gameState == GameState.Playing)
        {
            // todavia no gana hasta que llegue al pueblo (lo hacemos despues)
            // por ahora solo lo marcamos
            Debug.Log("Cuota alcanzada! Vuelve al pueblo.");
        }
    }

    public void GameOver(string reason)
    {
        if (gameState != GameState.Playing) return;
        gameState = GameState.GameOver;
        Debug.Log("GAME OVER: " + reason);
        OnGameStateChanged?.Invoke(gameState);
        Time.timeScale = 0f;
        UnlockCursor();
    }

    public void Victory()
    {
        if (gameState != GameState.Playing) return;
        gameState = GameState.Victory;
        Debug.Log("VICTORIA!");
        OnGameStateChanged?.Invoke(gameState);
        Time.timeScale = 0f;
        UnlockCursor();
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    // Helper para UI
    public string GetTimeFormatted()
    {
        int min = Mathf.FloorToInt(currentTime / 60f);
        int sec = Mathf.FloorToInt(currentTime % 60f);
        return string.Format("{0:00}:{1:00}", min, sec);
    }
}
