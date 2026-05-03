using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TrashCan : MonoBehaviour
{
    [Header("Interacción")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public bool debugLogs = true;

    [Header("Puntuación")]
    [Tooltip("Segundos entre intentos de ganar 1 punto")]
    public float timePerPoint = 2f;
    [Tooltip("Evento que recibe '1' cada vez que se gana un punto")]
    public UnityEvent<int> OnScoreGained;
    int localScore = 0;

    [Header("Flor (objeto existente en escena)")]
    [Tooltip("Referencia al GameObject de la flor ya presente en la escena (debe estar en la posición que quieres). El script solo la activará/desactivará.")]
    public GameObject flowerObject;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad (0..1) de que aparezca la flor e interrumpa la obtención de puntos")]
    public float flowerChance = 0.2f;
    [Tooltip("Tiempo durante el cual el jugador queda retenido sin ganar puntos (si fallas)")]
    public float flowerRetainDuration = 3f;
    [Tooltip("Tiempo que la flor permanece activa (se ocultará automáticamente)")]
    public float flowerLifetime = 4f;
    [Tooltip("Si true, la flor se desactiva al Start si está asignada")]
    public bool deactivateFlowerAtStart = true;
    public UnityEvent OnFlowerSpawned;

    [Header("SkillCheck rápido")]
    [Tooltip("Referencia al componente quickEventFlower (opcional). Si se asigna, se usa para intentar huir.")]
    public EventFlower quickEventFlower;
    [Tooltip("Número de pulsaciones necesarias para huir (si se usa quickEventFlower)")]
    public int quickRequiredPresses = 10;
    [Tooltip("Si true, el skillcheck usará flowerRetainDuration como tiempo límite")]
    public bool useRetainDurationAsSkillTime = true;

    [Header("Control del jugador")]
    [Tooltip("Si true usará el componente PlayerController para pausar controles; si no existe intentará deshabilitar componentes heurísticos")]
    public bool pausePlayerMovement = true;

    bool playerInRange;
    bool interacting;
    Coroutine interactionCoroutine;

    GameObject currentPlayer;
    PlayerController currentPlayerController;
    List<Behaviour> disabledMovementComponents = new List<Behaviour>();

    void Start()
    {
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"TrashCan: el Collider en '{name}' no está marcado como IsTrigger. Recomiendo marcarlo para que OnTriggerEnter/Exit funcione correctamente.");
        }

        if (flowerObject != null && deactivateFlowerAtStart)
        {
            flowerObject.SetActive(false);
            if (debugLogs) Debug.Log($"TrashCan: flor '{flowerObject.name}' desactivada al inicio.");
        }

        if (quickEventFlower != null)
        {
            // aplicar parámetros por defecto
            quickEventFlower.requiredPresses = quickRequiredPresses;
            quickEventFlower.duration = useRetainDurationAsSkillTime ? flowerRetainDuration : quickEventFlower.duration;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (!playerInRange)
            {
                if (debugLogs) Debug.Log("TrashCan: pulsada la tecla de interacción pero el jugador NO está en rango (playerInRange=false). Comprueba Collider/Tag/Rigidbody.");
                return;
            }

            if (!interacting) StartInteraction();
            else StopInteraction();
        }
    }

    public void ForceInteract()
    {
        if (!playerInRange)
        {
            if (debugLogs) Debug.Log("TrashCan.ForceInteract: jugador no está en rango.");
            return;
        }

        if (!interacting) StartInteraction();
        else StopInteraction();
    }

    void StartInteraction()
    {
        interacting = true;
        interactionCoroutine = StartCoroutine(InteractionRoutine());
        if (debugLogs) Debug.Log("TrashCan: interacción iniciada.");
    }

    void StopInteraction()
    {
        interacting = false;
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }
        RestorePlayerMovement();
        // cancelar skillcheck si estaba en curso
        if (quickEventFlower != null && quickEventFlower.IsActive) quickEventFlower.Cancel();
        if (debugLogs) Debug.Log("TrashCan: interacción detenida.");
    }

    IEnumerator InteractionRoutine()
    {
        while (interacting)
        {
            yield return new WaitForSeconds(timePerPoint);

            if (!interacting) yield break;

            if (Random.value < flowerChance)
            {
                ShowFlower();
                OnFlowerSpawned?.Invoke();

                if (debugLogs) Debug.Log("TrashCan: apareció la flor — comenzando intento de huida.");

                // Preparar y lanzar skillcheck si está asignado
                bool skillResultReceived = false;
                bool skillSuccess = false;

                UnityAction<bool> onResult = (bool success) =>
                {
                    skillResultReceived = true;
                    skillSuccess = success;
                };

                // Pausar controles inmediatamente
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

                if (quickEventFlower != null)
                {
                    // configurar y suscribir
                    quickEventFlower.requiredPresses = quickRequiredPresses;
                    quickEventFlower.duration = useRetainDurationAsSkillTime ? flowerRetainDuration : quickEventFlower.duration;
                    quickEventFlower.OnSkillCheckResult.AddListener(onResult);
                    quickEventFlower.StartSkillCheck();
                }
                else
                {
                    // si no hay skillcheck asignado, esperar flowerRetainDuration y considerar fallo
                    StartCoroutine(DelayedSetFalseAfter(flowerRetainDuration, () => { skillResultReceived = true; skillSuccess = false; }));
                }

                // esperar hasta que haya resultado (éxito/fracaso)
                while (!skillResultReceived)
                    yield return null;

                // si existía quickEventFlower, limpiar listener
                if (quickEventFlower != null)
                    quickEventFlower.OnSkillCheckResult.RemoveListener(onResult);

                if (skillSuccess)
                {
                    // Escapó: ocultar flor, restaurar controles y terminar interacción (huye)
                    HideExistingFlower();
                    RestorePlayerMovement();

                    if (debugLogs) Debug.Log("TrashCan: skillcheck exitoso — el jugador huyó de la planta.");
                    // Opcional: detener la interacción para que no siga generando puntos ahora
                    interacting = false;
                    yield break;
                }
                else
                {
                    // Falló: mantener retenido por el tiempo restante (ya hemos usado el skill duration como límite).
                    if (debugLogs) Debug.Log("TrashCan: skillcheck fallido — el jugador sigue retenido hasta que termine la flor.");
                    // Si el flowerLifetime es mayor que flowerRetainDuration, la flor seguirá visible; esperamos flowerRetainDuration si no lo hicimos.
                    // Ya hemos usado flowerRetainDuration como límite del skillcheck; asegurémonos de que la flor se oculte cuando toque flowerLifetime
                    // Restauramos movimiento SOLO tras finalizar el tiempo de retención (si se desea)
                    yield return new WaitForSeconds(Mathf.Max(0f, flowerRetainDuration));
                    RestorePlayerMovement();
                    HideExistingFlower();
                    if (debugLogs) Debug.Log("TrashCan: fin de retención por flor tras fallo del skillcheck.");
                    // continuar bucle sin otorgar punto
                    continue;
                }
            }

            // Otorga 1 punto: incrementa contador local y dispara evento
            localScore += 1;
            OnScoreGained?.Invoke(1);
            if (debugLogs) Debug.Log($"TrashCan: +1 punto (total local {localScore})");
        }
    }

    IEnumerator DelayedSetFalseAfter(float seconds, System.Action set)
    {
        yield return new WaitForSeconds(seconds);
        set?.Invoke();
    }

    void ShowFlower()
    {
        if (flowerObject == null)
        {
            if (debugLogs) Debug.LogWarning("TrashCan: ShowFlower llamado pero 'flowerObject' no está asignado.");
            return;
        }

        flowerObject.SetActive(true);
        CancelInvoke(nameof(HideExistingFlower));
        Invoke(nameof(HideExistingFlower), flowerLifetime);

        if (debugLogs) Debug.Log($"TrashCan: flor '{flowerObject.name}' activada.");
    }

    void HideExistingFlower()
    {
        if (flowerObject != null)
        {
            flowerObject.SetActive(false);
            if (debugLogs) Debug.Log($"TrashCan: flor '{flowerObject.name}' desactivada.");
        }
    }

    // Heurístico para deshabilitar componentes de movimiento si no se tiene PlayerController
    void DisableMovementHeuristic(GameObject player)
    {
        if (player == null) return;

        disabledMovementComponents.Clear();

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        var rb = player.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

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
                if (debugLogs) Debug.Log($"TrashCan: deshabilitado componente '{m.GetType().Name}' en jugador (heurístico).");
            }
        }
    }

    void RestoreMovementHeuristic(GameObject player)
    {
        if (player == null) return;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null && !cc.enabled) cc.enabled = true;

        var rb = player.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        foreach (var b in disabledMovementComponents)
        {
            if (b != null) b.enabled = true;
        }
        disabledMovementComponents.Clear();
    }

    void RestorePlayerMovement()
    {
        if (currentPlayer != null && pausePlayerMovement)
        {
            if (currentPlayerController != null)
            {
                currentPlayerController.SetControlsEnabled(true);
            }
            else
            {
                RestoreMovementHeuristic(currentPlayer);
            }

            if (debugLogs) Debug.Log("TrashCan: movimiento del jugador restaurado.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = true;
            currentPlayer = other.transform.root.gameObject;
            currentPlayerController = currentPlayer.GetComponent<PlayerController>();
            if (debugLogs) Debug.Log($"TrashCan: jugador entró en rango (via {other.gameObject.name}). playerInRange=true");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = false;
            RestorePlayerMovement();
            StopInteraction();
            currentPlayer = null;
            currentPlayerController = null;
            if (debugLogs) Debug.Log($"TrashCan: jugador salió de rango (via {other.gameObject.name}). playerInRange=false");
        }
    }
}
