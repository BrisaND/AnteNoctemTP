using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using AnteNoctem.Interactions;

[RequireComponent(typeof(Collider))]
public class TrashCan : MonoBehaviour, IInteractable
{
    [Header("Interacción")]
    public string playerTag = "Player";
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
    public float catchDuration = 3f;
    public bool deactivateFlowerAtStart = true;
    public UnityEvent OnFlowerSpawned;

    [Header("SkillCheck")]
    [Tooltip("Referencia al componente EventFlower asignado al Tacho")]
    public EventFlower quickEventFlower;
    public int quickRequiredPresses = 10;
    public float skillCheckTimeLimit = 4f;

    [Header("Control del jugador")]
    public bool pausePlayerMovement = true;

    // Guardado de posición original de la flor para evitar desfases
    private Vector3 initialFlowerLocalPos;
    private Quaternion initialFlowerLocalRot;

    GameObject currentPlayer;
    PlayerController currentPlayerController;
    List<Behaviour> disabledMovementComponents = new List<Behaviour>();

    bool interactionInProgress = false;

    void Start()
    {
        // Guardamos la transformación local original de la flor antes de hacer nada
        if (flowerObject != null)
        {
            initialFlowerLocalPos = flowerObject.transform.localPosition;
            initialFlowerLocalRot = flowerObject.transform.localRotation;
        }

        if (deactivateFlowerAtStart && flowerObject != null) flowerObject.SetActive(false);
        if (trashCanAnimator == null) trashCanAnimator = GetComponentInChildren<Animator>();
        if (flowerAnimator == null && flowerObject != null) flowerAnimator = flowerObject.GetComponent<Animator>();

        if (quickEventFlower != null)
        {
            quickEventFlower.useTimeLimit = true;
            quickEventFlower.requiredPresses = quickRequiredPresses;
            quickEventFlower.duration = skillCheckTimeLimit;
        }

        var gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (gm != null) OnScoreGained.AddListener(gm.AddScore);
    }

    public string GetPromptText() => "Apretá E para revolver la basura";
    public bool CanInteract() => !interactionInProgress;

    public void Interact(PlayerController player)
    {
        currentPlayer = player.gameObject;
        currentPlayerController = player;
        if (interactionInProgress) return;
        StartCoroutine(HandleSingleInteraction());
    }

    IEnumerator HandleSingleInteraction()
    {
        interactionInProgress = true;

        if (Random.value < flowerChance)
        {
            OpenTrashCanAndShowFlower();
            OnFlowerSpawned?.Invoke();

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

            quickEventFlower.useTimeLimit = true;
            quickEventFlower.duration = tiempoSkillCheck;
            quickEventFlower.StartSkillCheck();

            while (!resultReceived)
            {
                if (trashCanAnimator != null) trashCanAnimator.SetBool(animatorBoolParam, true);
                yield return null;
            }

            quickEventFlower.OnSkillCheckResult.RemoveListener(onResult);

            if (success)
            {
                AwardStealPointsFromTrashCan();
                CloseTrashCanAndHideFlower();
                RestorePlayerMovement();

                currentPlayer = null;
                currentPlayerController = null;
            }
            else
            {
                // ATAQUE DE LA FLOR
                if (flowerAnimator != null) flowerAnimator.SetTrigger(flowerAttackTrigger);

                float timer = 0f;
                while (timer < catchDuration)
                {
                    timer += Time.deltaTime;
                    // Forzamos que la tapa siga abierta durante el ataque
                    if (trashCanAnimator != null) trashCanAnimator.SetBool(animatorBoolParam, true);
                    yield return null;
                }

                RestorePlayerMovement();
                CloseTrashCanAndHideFlower();

                currentPlayer = null;
                currentPlayerController = null;
            }
        }
        else
        {
            AwardStealPointsFromTrashCan();
            yield return StartCoroutine(QuickOpenCloseAnimation());

            currentPlayer = null;
            currentPlayerController = null;
        }

        interactionInProgress = false;
    }

    void OpenTrashCanAndShowFlower()
    {
        if (flowerObject != null)
        {
            // Forzamos que vuelva a su posición de diseño antes de activarse
            flowerObject.transform.localPosition = initialFlowerLocalPos;
            flowerObject.transform.localRotation = initialFlowerLocalRot;
            flowerObject.SetActive(true);
        }
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
            yield return new WaitForSeconds(0.4f);
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

    void OnDisable()
    {
        if (quickEventFlower != null && quickEventFlower.IsActive) quickEventFlower.Cancel();
    }

    public int GetLocalScore() => localScore;
}