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

    [Header("Animación del Tacho")]
    public Animator trashCanAnimator;
    public string animatorBoolParam = "isOpen";

    [Header("Puntuación")]
    public UnityEvent<int> OnScoreGained;
    int localScore = 0;

    [Header("Flor (debe existir en escena)")]
    public GameObject flowerObject;
    public Animator flowerAnimator;
    public string flowerAttackTrigger = "AttackTrigger";
    [Range(0f, 1f)]
    public float flowerChance = 0.2f;

    [Tooltip("¿Cuántos segundos se queda la flor GIGANTE atrapando al jugador despues de fallar?")]
    public float catchDuration = 3f;

    public bool deactivateFlowerAtStart = true;
    public UnityEvent OnFlowerSpawned;

    [Header("SkillCheck")]
    [Tooltip("Referencia al componente EventFlower (skillcheck) en escena.")]
    public EventFlower quickEventFlower;
    public int quickRequiredPresses = 10;
    [Tooltip("Tiempo límite que tiene el jugador para resolver el SkillCheck")]
    public float skillCheckTimeLimit = 4f;

    [Header("Control del jugador")]
    public bool pausePlayerMovement = true;

    bool playerInRange;
    GameObject currentPlayer;
    PlayerController currentPlayerController;
    List<Behaviour> disabledMovementComponents = new List<Behaviour>();

    bool interactionInProgress = false;
    bool isPunishing = false; // evitar que OnTriggerExit cierre el tacho durante el ataque

    void Start()
    {
        if (deactivateFlowerAtStart) flowerObject.SetActive(false);
        if (trashCanAnimator == null) trashCanAnimator = GetComponentInChildren<Animator>();
        if (flowerAnimator == null && flowerObject != null) flowerAnimator = flowerObject.GetComponent<Animator>();

        quickEventFlower.requiredPresses = quickRequiredPresses;
        quickEventFlower.duration = skillCheckTimeLimit;

        var gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (gm != null) OnScoreGained.AddListener(gm.AddScore);
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (!playerInRange) return;
            InteractOnce();
        }
    }

    void InteractOnce()
    {
        if (interactionInProgress) return;
        StartCoroutine(HandleSingleInteraction());
    }

    IEnumerator HandleSingleInteraction()
    {
        interactionInProgress = true;
        isPunishing = false;

        if (Random.value < flowerChance)
        {
            // 1. Abrimos el tacho y mostramos la flor
            OpenTrashCanAndShowFlower();
            OnFlowerSpawned?.Invoke();

            // Espera obligatoria para que el Animator complete la transición de apertura
            yield return new WaitForSeconds(0.2f);

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

            quickEventFlower.OnSkillCheckResult.AddListener(onResult);
            quickEventFlower.requiredPresses = quickRequiredPresses;

            float tiempoSkillCheck = skillCheckTimeLimit;
            if (currentPlayerController != null) tiempoSkillCheck += currentPlayerController.escapeTimeBonus;

            quickEventFlower.duration = tiempoSkillCheck;
            quickEventFlower.StartSkillCheck();

            // Mantiene la tapa arriba y la flor activa mientras dure el minijuego
            while (!resultReceived)
            {
                // Forzamos el estado del Animator en cada frame por si otra función intenta apagarlo
                if (trashCanAnimator != null) trashCanAnimator.SetBool(animatorBoolParam, true);
                yield return null;
            }

            quickEventFlower.OnSkillCheckResult.RemoveListener(onResult);

            if (success)
            {
                // ÉXITO: Recién acá cerramos todo de golpe
                AwardStealPointsFromTrashCan();
                CloseTrashCanAndHideFlower();
                RestorePlayerMovement();
            }
            else
            {
                // 2. FASE ATAQUE (FALLÓ): Activamos el castigo manteniendo la tapa levantada
                isPunishing = true;

                if (flowerAnimator != null)
                {
                    flowerAnimator.SetTrigger(flowerAttackTrigger);
                }

                yield return new WaitForSeconds(catchDuration);

                // 3. FIN DEL CASTIGO: Liberamos y cerramos
                isPunishing = false;
                RestorePlayerMovement();
                CloseTrashCanAndHideFlower();
            }
        }
        else
        {
            AwardStealPointsFromTrashCan();
            StartCoroutine(QuickOpenCloseAnimation());
        }

        // Bloqueamos salidas accidentales hasta este frame exacto
        interactionInProgress = false;
    }

    void OpenTrashCanAndShowFlower()
    {
        if (flowerObject != null) flowerObject.SetActive(true);
        if (trashCanAnimator != null) trashCanAnimator.SetBool(animatorBoolParam, true);
    }

    void CloseTrashCanAndHideFlower()
    {
        if (flowerObject != null) flowerObject.SetActive(false);
        if (trashCanAnimator != null) trashCanAnimator.SetBool(animatorBoolParam, false);
    }

    IEnumerator QuickOpenCloseAnimation()
    {
        if (trashCanAnimator != null)
        {
            trashCanAnimator.SetBool(animatorBoolParam, true);
            yield return new WaitForSeconds(0.3f);
            trashCanAnimator.SetBool(animatorBoolParam, false);
        }
    }

    void DisableMovementHeuristic(GameObject player)
    {
        if (player == null) return;
        disabledMovementComponents.Clear();
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        var rb = player.GetComponentInChildren<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
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

    void RestorePlayerMovement()
    {
        if (currentPlayer == null) return;
        if (currentPlayerController != null) currentPlayerController.SetControlsEnabled(true);
    }

    void AwardStealPointsFromTrashCan()
    {
        if (RobberySystem.Instance != null) RobberySystem.Instance.AwardStealPoints("Robo en basurero");
        else if (GameManager.Instance != null) GameManager.Instance.AddScore(Random.Range(5, 16));
    }

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

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            // --- NUEVO FILTRO ---
            // Si el minijuego de la flor está en progreso o castigando,
            // bloqueamos por completo que este método cierre la tapa o limpie los datos.
            if (interactionInProgress || isPunishing)
            {
                return;
            }

            playerInRange = false;
            RestorePlayerMovement();
            CloseTrashCanAndHideFlower();
            currentPlayer = null;
            currentPlayerController = null;
        }
    }

    void OnDisable()
    {
        if (quickEventFlower != null && quickEventFlower.IsActive) quickEventFlower.Cancel();
    }

    public int GetLocalScore() => localScore;
}