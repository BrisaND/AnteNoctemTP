using UnityEngine;

public class QuickEventManager : MonoBehaviour
{
    public static QuickEventManager Instance { get; private set; }

    [Header("UI")]
    public GameObject quickEventCanvas; // asignar el Canvas del quick event
    public SkillCheck skillCheck;       // referencia al SkillCheck dentro del canvas

    // Ciudadano que inició el evento
    private CitizenAI currentCitizen;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (quickEventCanvas != null) quickEventCanvas.SetActive(false);
        if (skillCheck != null)
        {
            // asegurar que no quede suscrito persistentemente
            skillCheck.OnSkillCheckResult.RemoveAllListeners();
        }
    }

    // Llamar desde CitizenAI cuando el jugador interactúa
    public void StartQuickEvent(CitizenAI citizen)
    {
        if (quickEventCanvas == null || skillCheck == null) return;

        currentCitizen = citizen;

        quickEventCanvas.SetActive(true);

        // pausar juego (SkillCheck usa unscaledDeltaTime)
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // subscribir al resultado
        skillCheck.OnSkillCheckResult.AddListener(HandleSkillResult);
        skillCheck.StartSkillCheck();
    }

    void HandleSkillResult(bool success)
    {
        // limpiar suscripción
        skillCheck.OnSkillCheckResult.RemoveListener(HandleSkillResult);

        // restaurar UI y tiempo
        quickEventCanvas.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // notificar al ciudadano
        if (currentCitizen != null)
        {
            currentCitizen.OnPlayerInteractionResult(success);
            currentCitizen = null;
        }
    }
}