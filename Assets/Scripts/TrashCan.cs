using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class TrashCan : MonoBehaviour
{
    [Header("Interacción")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public bool debugLogs = true;

    [Header("Puntuación")]
    public UnityEvent<int> OnScoreGained;
    int localScore = 0;

    [Header("Flor (debe existir en escena)")]
    [Tooltip("Referencia al GameObject de la flor ya presente en la escena (debe estar en la posición deseada).")]
    public GameObject flowerObject;
    [Range(0f, 1f)]
    public float flowerChance = 0.2f;
    [Tooltip("Tiempo durante el cual el jugador queda retenido si falla")]
    public float flowerRetainDuration = 3f;
    [Tooltip("Tiempo que la flor permanece activa")]
    public float flowerLifetime = 4f;
    public bool deactivateFlowerAtStart = true;
    public UnityEvent OnFlowerSpawned;

    [Header("SkillCheck")]
    [Tooltip("Referencia al componente EventFlower (skillcheck) que ya está en la escena.")]
    public EventFlower quickEventFlower;
    [Tooltip("Número de pulsaciones necesarias para huir")]
    public int quickRequiredPresses = 10;
    [Tooltip("Si true, el skillcheck usará flowerRetainDuration como tiempo límite")]
    public bool useRetainDurationAsSkillTime = true;

    [Header("Control del jugador")]
    [Tooltip("Usar PlayerController para pausar controles si existe")]
    public bool pausePlayerMovement = true;

    bool playerInRange;
    GameObject currentPlayer;
    PlayerController currentPlayerController;
    List<Behaviour> disabledMovementComponents = new List<Behaviour>();

    // Evita reentradas
    bool interactionInProgress = false;

    void Start()
    {
        if (deactivateFlowerAtStart) flowerObject.SetActive(false);

        quickEventFlower.requiredPresses = quickRequiredPresses;
        quickEventFlower.duration = useRetainDurationAsSkillTime ? flowerRetainDuration : quickEventFlower.duration;

        var col = GetComponent<Collider>();

        var gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            OnScoreGained.AddListener(gm.AddScore);
            if (debugLogs) Debug.Log("TrashCan: OnScoreGained conectado automáticamente a GameManager.AddScore.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            // Solo permitir interacción si el jugador está en rango
            if (!playerInRange)
            {
                return;
            }

            InteractOnce();
        }
    }

    // Maneja una interacción completa (aparecer flor, skillcheck, retención, puntuación) sin permitir reentradas
    void InteractOnce()
    {
        if (interactionInProgress)
        {
            return;
        }

        StartCoroutine(HandleSingleInteraction());
    }

    // Cmaneja toda la lógica de una interacción completa, asegurando que no se pueden solapar múltiples interacciones
    IEnumerator HandleSingleInteraction()
    {
        interactionInProgress = true;

        if (Random.value < flowerChance)
        {
            // Aparece la flor
            ShowFlower();
            OnFlowerSpawned?.Invoke();

            // Pausar controles del jugador
            if (pausePlayerMovement && currentPlayer != null)
            {
                if (currentPlayerController != null)
                {
                    currentPlayerController.SetControlsEnabled(false);
                    currentPlayerController.ResetMovementState();
                }
                else
                {
                    DisableMovementHeuristic(currentPlayer);
                }
            }

            bool resultReceived = false;
            bool success = false;

            UnityAction<bool> onResult = (bool r) =>
            {
                resultReceived = true;
                success = r;
            };

            // Suscribir y arrancar el skillcheck existente
            quickEventFlower.OnSkillCheckResult.AddListener(onResult);
            quickEventFlower.requiredPresses = quickRequiredPresses;
            quickEventFlower.duration = useRetainDurationAsSkillTime ? flowerRetainDuration : quickEventFlower.duration;
            quickEventFlower.StartSkillCheck();

            // Esperar resultado
            while (!resultReceived)
                yield return null;

            // Limpiar listener
            quickEventFlower.OnSkillCheckResult.RemoveListener(onResult);

            if (success)
            {
                // Éxito en el skillcheck -> sumar punto y restaurar movimiento
                AwardStealPointsFromTrashCan();
                HideExistingFlower();
                RestorePlayerMovement();
            }
            else
            {
                // Falló el skillcheck -> esperar un tiempo antes de permitir moverse
                yield return new WaitForSeconds(Mathf.Max(0f, flowerRetainDuration));
                RestorePlayerMovement();
                HideExistingFlower();
            }
        }
        else
        {
            // No aparece flor -> robo inmediato
            AwardStealPointsFromTrashCan();
        }

        interactionInProgress = false;
    }

    // Muestra la flor y programa su ocultación tras x segundos, cancelando cualquier ocultación previa pendiente
    void ShowFlower()
    {
        flowerObject.SetActive(true);
        CancelInvoke(nameof(HideExistingFlower));
        Invoke(nameof(HideExistingFlower), flowerLifetime);
    }

    // Oculta la flor inmediatamente
    void HideExistingFlower()
    {
        flowerObject.SetActive(false);
    }

    // deshabilita movimiento si no hay PlayerController, buscando componentes relacionados con movimiento y desactivándolos (y restaurándolos luego)
    void DisableMovementHeuristic(GameObject player)
    {
        if (player == null) return;

        disabledMovementComponents.Clear();

        // Deshabilitar CharacterController si existe
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Deshabilitar Rigidbody para evitar que el jugador se mueva por fuerzas externas mientras está retenido
        var rb = player.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // intentari cubrir otros tipos de movimiento: buscar componentes con nombres relacionados con movimiento y desactivarlos (y restaurarlos luego)
        var monos = player.GetComponents<MonoBehaviour>();
        foreach (var m in monos)
        {
            if (m == null) continue;
            var typeName = m.GetType().Name.ToLowerInvariant();
            if (typeName.Contains("move") || typeName.Contains("walk") || typeName.Contains("controller") || typeName.Contains("player"))
            {
                if (m == this) continue;
                m.enabled = false;
                disabledMovementComponents.Add(m);
            }
        }
    }

    // Restaura el movimiento del jugador, usando PlayerController
    void RestorePlayerMovement()
    {
        if (currentPlayer == null) return;

        if (currentPlayerController != null)
            currentPlayerController.SetControlsEnabled(true);
    }

    void AwardStealPointsFromTrashCan()
    {
        if (RobberySystem.Instance != null)
        {
            RobberySystem.Instance.AwardStealPoints("Robo en basurero");
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(Random.Range(5, 16));
        }
    }

    // Detecta al jugador entrando en rango, guardando referencias para la interacción
    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            playerInRange = true;
            currentPlayer = pc.gameObject;
            currentPlayerController = pc;
        }
    }

    // Asegura limpiar estado si el jugador sale del rango durante una interacción
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = false;
            RestorePlayerMovement();
            interactionInProgress = false;
            currentPlayer = null;
            currentPlayerController = null;
        }
    }

    // Asegura limpiar estado si el objeto se desactiva durante una interacción
    void OnDisable()
    {
        // Asegurar que si hay un skillcheck en curso se limpia
        if (quickEventFlower != null && quickEventFlower.IsActive)
        {
            quickEventFlower.Cancel();
        }
    }

    // obtiene la puntuación local del cubo de basura
    public int GetLocalScore() => localScore;
}
