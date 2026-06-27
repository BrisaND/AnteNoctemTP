using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum DayState { Dia, Tarde, Noche }
    public enum GameState { Playing, GameOver, Victory }

    [Header("Hub (Base)")]
    [Tooltip("Si está activo, no corre el temporizador ni el ciclo día/noche (escena campamento).")]
    public bool isHubScene;

    [Header("Timer")]
    [Tooltip("Duracion total del nivel en segundos")]
    public float levelDuration = 180f;
    public float currentTime { get; private set; }

    [Header("Score")]
    public int targetScore = 100;
    public int currentScore { get; private set; }

    [Tooltip("¿Ganar automáticamente al llegar al puntaje o permitir seguir farmeando?")]
    public bool autoWinOnTargetScore = false;

    [Header("UI Aviso de Cuota")]
    [Tooltip("El GameObject del Canvas o panel que avisa que ya se puede regresar.")]
    public GameObject quotaNotificationCanvas;
    [Tooltip("¿Cuántos segundos se queda el cartel en pantalla?")]
    public float notificationDuration = 4f;

    [Header("Estados")]
    public DayState dayState { get; private set; } = DayState.Dia;
    public GameState gameState { get; private set; } = GameState.Playing;

    [Header("Umbrales del dia (porcentaje del tiempo restante)")]
    [Range(0f, 1f)] public float tardeThreshold = 0.5f;
    [Range(0f, 1f)] public float nocheThreshold = 0.2f;

    public System.Action<DayState> OnDayStateChanged;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<int> OnScoreChanged;

    private bool hasNotifiedQuota = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Base")
            isHubScene = true;

        currentTime = levelDuration;
        currentScore = 0;

        if (quotaNotificationCanvas != null) quotaNotificationCanvas.SetActive(false);
    }

    void Update()
    {
        if (isHubScene) return;
        if (gameState != GameState.Playing) return;

        // Solo descontamos tiempo si no ha llegado a 0
        if (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            UpdateDayState();

            if (currentTime <= 0f)
            {
                currentTime = 0f;

                // --- NUEVA LÓGICA DE TIEMPO ---
                // Si se acaba el tiempo y NO llegó a los puntos obligatorios, pierde inmediatamente.
                // Si YA tiene los puntos, NO lo mandamos a Victoria. Se queda en el nivel para farmear.
                if (currentScore < targetScore)
                {
                    GameOver("Se hizo de noche y no juntaste la cuota mínima.");
                }
                else
                {
                    Debug.Log("Se acabó el tiempo, pero tienes la cuota. ¡Busca la puerta de salida para escapar!");
                }
            }
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

    public float GetDayProgress01()
    {
        if (levelDuration <= 0f) return 0f;
        return Mathf.Clamp01(currentTime / levelDuration);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);

        if (currentScore >= targetScore && gameState == GameState.Playing)
        {
            if (autoWinOnTargetScore)
            {
                Victory();
            }
            else
            {
                if (!hasNotifiedQuota)
                {
                    hasNotifiedQuota = true;
                    StartCoroutine(ShowQuotaNotificationRoutine());
                }
            }
        }
    }

    private IEnumerator ShowQuotaNotificationRoutine()
    {
        if (quotaNotificationCanvas != null)
        {
            // Forzamos la activación del GameObject completo
            quotaNotificationCanvas.SetActive(true);

            // Esperamos los segundos configurados en el inspector
            yield return new WaitForSeconds(notificationDuration);

            // Lo volvemos a apagar por completo
            quotaNotificationCanvas.SetActive(false);
        }
    }

    public void GameOver(string reason)
    {
        if (gameState != GameState.Playing) return;
        gameState = GameState.GameOver;
        OnGameStateChanged?.Invoke(gameState);

        if (QuickEventManager.Instance != null && QuickEventManager.Instance.IsActive)
        {
            if (QuickEventManager.Instance.quickEventCanvas != null)
                QuickEventManager.Instance.quickEventCanvas.SetActive(false);
        }
        Time.timeScale = 1f;
        UnlockCursor();
        SceneManager.LoadScene("GameOver");
    }

    public void Victory()
    {
        if (gameState != GameState.Playing) return;
        gameState = GameState.Victory;
        OnGameStateChanged?.Invoke(gameState);

        if (QuickEventManager.Instance != null && QuickEventManager.Instance.IsActive)
        {
            if (QuickEventManager.Instance.quickEventCanvas != null)
                QuickEventManager.Instance.quickEventCanvas.SetActive(false);
        }

    }

    public void CompleteLevelAndLoadVictory()
    {
        Time.timeScale = 1f;
        UnlockCursor();

        // RECIÉN ACÁ HACEMOS EL CAMBIO DE ESCENA
        Debug.Log("Cargando pantalla de victoria desde la puerta de escape...");
        SceneManager.LoadScene("Victory");
    }

    public void ReturnToBase()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Base");
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

    public string GetTimeFormatted()
    {
        int min = Mathf.FloorToInt(currentTime / 60f);
        int sec = Mathf.FloorToInt(currentTime % 60f);
        return string.Format("{0:00}:{1:00}", min, sec);
    }
}