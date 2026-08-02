//TPFinal - Juan Cruz Villarreo

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AnteNoctem.Core;

//Solo puede haber un GameManager en toda la escena
public class GameManager : Singleton<GameManager>
{
    // Listamos los posibles estados del dia y del juego con nombres claros en vez de numeros sueltos
    public enum DayState { Dia, Tarde, Noche }
    public enum GameState { Playing, GameOver, Victory }

    [Header("Hub (Base)")]
    [Tooltip("Si está activo, no corre el temporizador ni el ciclo día/noche (escena campamento).")]
    public bool isHubScene;

    [Header("Timer")]
    [Tooltip("Duracion total del nivel en segundos")]
    public float levelDuration = 180f;

    // Cualquiera puede leer currentTime, pero solo el GameManager puede modificarlo
    public float currentTime { get; private set; }

    [Header("Timer Audio Alerta")]
    [Tooltip("El AudioSource que reproducirá el sonido de cuenta regresiva.")]
    public AudioSource timerAudioSource;
    [Tooltip("Clip de audio del timer (ej: tic-tac o alarma rápida).")]
    public AudioClip timerAlertClip;
    [Tooltip("¿A cuántos segundos del final debe empezar a sonar el audio?")]
    public float alertTimeThreshold = 30f;

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

    // Cuando algo importante pasa en el juego, los "invocamos" y todos los scripts que se suscribieron se enteran.
    public GameDelegates.DayStateChangedHandler OnDayStateChanged;
    public GameDelegates.ScoreChangedHandler OnScoreChanged;

    // Lo usamos para avisar cuando cambia el estado del juego.
    public System.Action<GameState> OnGameStateChanged;

    private bool hasNotifiedQuota = false;
    private bool isAlertAudioPlaying = false; // Flag para controlar que no se ejecute Play() en cada frame

    void Start()
    {
        // Si estamos en la escena Base (pueblo), desactivamos el timer
        if (SceneManager.GetActiveScene().name == "Base")
            isHubScene = true;

        currentTime = levelDuration;
        currentScore = 0;

        if (quotaNotificationCanvas != null) quotaNotificationCanvas.SetActive(false);

        // Auto-configuración del AudioSource si te olvidás de asignarlo en el inspector
        if (timerAudioSource == null)
        {
            timerAudioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (isHubScene) return;
        if (gameState != GameState.Playing) return;

        if (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            UpdateDayState();
            CheckTimerAudioAlert(); // <-- Verificamos el estado del sonido del timer en cada frame

            // Si se acaba el tiempo y no llego a la cuota, pierde.
            // Si ya tiene la cuota, no lo mandamos a victoria: tiene que llegar a la puerta de salida.
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                StopTimerAudio(); // Detener sonido al expirar el tiempo

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

    // Controla cuándo debe empezar a reproducirse el audio de alerta
    void CheckTimerAudioAlert()
    {
        if (timerAudioSource == null || timerAlertClip == null) return;

        // Si entramos en la zona crítica de tiempo y todavía no está sonando, lo activamos
        if (currentTime <= alertTimeThreshold && !isAlertAudioPlaying)
        {
            isAlertAudioPlaying = true;
            timerAudioSource.clip = timerAlertClip;
            timerAudioSource.loop = true; // Forzamos a que sea un bucle continuo
            timerAudioSource.Play();
        }
    }

    // Apaga el audio de forma segura
    void StopTimerAudio()
    {
        if (timerAudioSource != null && timerAudioSource.isPlaying)
        {
            timerAudioSource.Stop();
        }
        isAlertAudioPlaying = false;
    }

    // Calculamos en que parte del dia estamos segun el tiempo que queda
    void UpdateDayState()
    {
        float pct = currentTime / levelDuration;
        DayState newState;
        if (pct > tardeThreshold) newState = DayState.Dia;
        else if (pct > nocheThreshold) newState = DayState.Tarde;
        else newState = DayState.Noche;

        // Si cambio de estado, avisamos a todos los que esten suscriptos al delegate
        if (newState != dayState)
        {
            dayState = newState;
            OnDayStateChanged?.Invoke(dayState);
        }
    }


    /// Devuelve el progreso del dia entre 0 y 1. 1 = inicio del nivel (dia), 0 = se acabo el tiempo (noche).
    /// Los enemigos y el DayNightCycle lo usan para volverse mas peligrosos a medida que oscurece.

    public float GetDayProgress01()
    {
        if (levelDuration <= 0f) return 0f;
        return Mathf.Clamp01(currentTime / levelDuration);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        // avisa a la UI y a quien este suscripto que cambio el puntaje
        OnScoreChanged?.Invoke(currentScore, targetScore);

        // Si llego a la cuota minima, le avisamos al jugador
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

    // Muestra un cartel "Ya tienes la cuota, busca la salida!" por unos segundos
    private IEnumerator ShowQuotaNotificationRoutine()
    {
        if (quotaNotificationCanvas != null)
        {
            quotaNotificationCanvas.SetActive(true);
            yield return new WaitForSeconds(notificationDuration);
            quotaNotificationCanvas.SetActive(false);
        }
    }

    public void GameOver(string reason)
    {
        if (gameState != GameState.Playing) return;
        gameState = GameState.GameOver;
        StopTimerAudio(); // Matamos el audio si perdés
        OnGameStateChanged?.Invoke(gameState);

        // Si habia un QuickEvent activo, lo cerramos
        if (QuickEventManager.Instance != null && QuickEventManager.Instance.IsActive)
        {
            if (QuickEventManager.Instance.quickEventCanvas != null)
                QuickEventManager.Instance.quickEventCanvas.SetActive(false);
        }
        Time.timeScale = 1f;
        UnlockCursor();
        // Reseteamos los materiales al perder
        if (MaterialInventory.Instance != null)
        {
            MaterialInventory.Instance.ResetInventory();
        }

        // Reseteamos los power-ups al perder
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var powerUps = player.GetComponent<PlayerPowerUps>();
            if (powerUps != null) powerUps.ResetPowerUps();
        }
        SceneManager.LoadScene("GameOver");
    }

    public void Victory()
    {
        if (gameState != GameState.Playing) return;
        gameState = GameState.Victory;
        StopTimerAudio(); // Matamos el audio si ganás
        OnGameStateChanged?.Invoke(gameState);

        if (QuickEventManager.Instance != null && QuickEventManager.Instance.IsActive)
        {
            if (QuickEventManager.Instance.quickEventCanvas != null)
                QuickEventManager.Instance.quickEventCanvas.SetActive(false);
        }
    }

    // Lo llama ExitZone cuando el jugador llega a la puerta de salida con la cuota completa
    public void CompleteLevelAndLoadVictory()
    {
        Time.timeScale = 1f;
        UnlockCursor();
        StopTimerAudio();
        Debug.Log("Cargando pantalla de victoria desde la puerta de escape...");
        SceneManager.LoadScene("Victory");
    }

    public void ReturnToBase()
    {
        Time.timeScale = 1f;
        StopTimerAudio();
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
        StopTimerAudio();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    // Helper para mostrar el tiempo en formato MM:SS en la UI
    public string GetTimeFormatted()
    {
        int min = Mathf.FloorToInt(currentTime / 60f);
        int sec = Mathf.FloorToInt(currentTime % 60f);
        return string.Format("{0:00}:{1:00}", min, sec);
    }
}