//TPFinal - Malena Misson

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CitizenAI : MonoBehaviour
{
    public enum CitizenState { Patrolling, Detaining }

    [Header("Configuración de Animaciones")]
    public Animator citizenAnimator;
    public float transitionTime = 0.25f;
    [SerializeField] private string animIdle = "MIRARSE";
    [SerializeField] private string animCaminar = "WALKINGPONE";
    [SerializeField] private string animSujetar = "SURPRISE";

    [Header("UI de Interacción")]
    [Tooltip("Panel o Texto de UI (ej: 'Presiona E para Robar') que se muestra al estar cerca")]
    public GameObject interactionPromptUI;

    [Header("Configuración de Robo")]
    public bool randomizeDifficulty = true;
    public RobberySystem.DifficultyLevel difficulty = RobberySystem.DifficultyLevel.Easy;

    [Header("Economía y Puntos")]
    [Tooltip("Dinero mínimo que puede tener este ciudadano")]
    public float dineroMinimo = 10f;
    [Tooltip("Dinero máximo que puede tener este ciudadano")]
    public float dineroMaximo = 50f;

    // Variables de control de estado
    private float dineroActual;
    private bool haSidoRobado = false;
    private bool isPlayerInRange = false;

    [Header("Patrullaje")]
    public List<Transform> patrolPoints = new List<Transform>();

    [Header("Efectos Visuales")]
    public GameObject pesosParticlesPrefab;
    public Transform particleSpawnPoint;

    [Header("Audio del Ciudadano")]
    public AudioClip robSoundSuccess;
    public AudioClip detainSound;
    public AudioSource audioSource;

    [Header("Debug")]
    public bool showGizmo = true;

    public CitizenState currentState { get; private set; } = CitizenState.Patrolling;

    private float _detentionDuration = 4f;
    private float _interactDistance = 2.8f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerCtrl;
    private int currentPatrolIndex = 0;
    private WardenAI warden;
    private string currentPlayingAnim = "";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (warden == null) warden = FindFirstObjectByType<WardenAI>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }
        }

        // Aseguramos que la UI comience oculta
        SetPromptVisible(false);
    }

    void Start()
    {
        dineroActual = Random.Range(dineroMinimo, dineroMaximo);

        if (randomizeDifficulty)
        {
            float roll = Random.value;
            if (roll < 0.6f) difficulty = RobberySystem.DifficultyLevel.Easy;
            else if (roll < 0.9f) difficulty = RobberySystem.DifficultyLevel.Medium;
            else difficulty = RobberySystem.DifficultyLevel.Hard;
        }

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCtrl = p.GetComponent<PlayerController>();
        }

        if (citizenAnimator == null) citizenAnimator = GetComponentInChildren<Animator>();

        if (patrolPoints.Count > 0) GoToNextPatrolPoint();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing)
        {
            SetPromptVisible(false);
            return;
        }

        switch (currentState)
        {
            case CitizenState.Patrolling:
                Patrol();
                CheckPlayerInteractionWhileMoving();
                ControlarAnimacionMovimiento();
                break;
            case CitizenState.Detaining:
                SetPromptVisible(false); // Ocultar UI si el ciudadano está sujetando al jugador
                ReproducirAnimacion(animSujetar);
                break;
        }
    }

    private void ControlarAnimacionMovimiento()
    {
        if (citizenAnimator == null) return;

        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            ReproducirAnimacion(animCaminar);
        }
        else
        {
            ReproducirAnimacion(animIdle);
        }
    }

    private void ReproducirAnimacion(string nombreAnimacion)
    {
        if (currentPlayingAnim == nombreAnimacion) return;

        if (citizenAnimator != null)
        {
            citizenAnimator.CrossFadeInFixedTime(nombreAnimacion, transitionTime);
            currentPlayingAnim = nombreAnimacion;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Count == 0) return;
        agent.speed = 2f;

        if (HasReachedDestination())
        {
            GoToNextPatrolPoint();
        }
    }

    void CheckPlayerInteractionWhileMoving()
    {
        if (player == null) return;

        // Si ya fue robado, no se muestra nada y se ignora la interacción
        if (haSidoRobado)
        {
            if (isPlayerInRange) SetPromptVisible(false);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= _interactDistance)
        {
            if (!isPlayerInRange)
            {
                SetPromptVisible(true);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                SetPromptVisible(false);
                StartInteraction();
            }
        }
        else
        {
            if (isPlayerInRange)
            {
                SetPromptVisible(false);
            }
        }
    }

    private void SetPromptVisible(bool visible)
    {
        isPlayerInRange = visible;
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(visible);
        }
    }

    void StartInteraction()
    {
        if (QuickEventManager.Instance == null || QuickEventManager.Instance.IsActive) return;
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        if (haSidoRobado) return;

        QuickEventManager.Instance.StartQuickEvent(this);
    }

    public void OnPlayerInteractionResult(bool success)
    {
        SetPromptVisible(false);

        if (success)
        {
            haSidoRobado = true;

            if (RobberySystem.Instance != null)
            {
                RobberySystem.Instance.AwardStealPoints(difficulty, "Robo al ciudadano");
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(Random.Range(5, 16));
            }

            Debug.Log($"¡Éxito! Has robado {dineroActual:F2} pesos.");

            if (pesosParticlesPrefab != null)
            {
                Vector3 spawnPos = particleSpawnPoint != null ? particleSpawnPoint.position : transform.position;
                Instantiate(pesosParticlesPrefab, spawnPos, Quaternion.identity);
            }

            if (audioSource != null && robSoundSuccess != null)
            {
                audioSource.PlayOneShot(robSoundSuccess);
            }

            if (currentState != CitizenState.Patrolling) ResumePatrol();
        }
        else
        {
            StartCoroutine(DetainPlayerCoroutine());
        }
    }

    IEnumerator DetainPlayerCoroutine()
    {
        currentState = CitizenState.Detaining;

        if (audioSource != null && detainSound != null)
        {
            audioSource.PlayOneShot(detainSound);
        }

        if (warden != null && player != null)
        {
            warden.AlertToPosition(player.position);
        }

        agent.isStopped = true;
        agent.updatePosition = false;
        agent.velocity = Vector3.zero;

        if (playerCtrl != null) playerCtrl.SetControlsEnabled(false);

        float playerGroundY = player != null ? player.position.y : 0f;

        Rigidbody playerRb = player != null ? player.GetComponent<Rigidbody>() : null;
        bool wasKinematic = false;
        if (playerRb != null)
        {
            wasKinematic = playerRb.isKinematic;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }

        Vector3 holdOffsetXZ = transform.forward * 0.8f;
        float timer = 0f;
        while (timer < _detentionDuration)
        {
            timer += Time.deltaTime;
            if (player != null)
            {
                Vector3 targetPos = transform.position + holdOffsetXZ;
                targetPos.y = playerGroundY;
                player.position = targetPos;
            }
            yield return null;
        }

        if (playerRb != null)
        {
            playerRb.isKinematic = wasKinematic;
        }

        if (playerCtrl != null) playerCtrl.SetControlsEnabled(true);

        ResumePatrol();
    }

    void ResumePatrol()
    {
        agent.updatePosition = true;
        agent.isStopped = false;
        currentState = CitizenState.Patrolling;
        GoToNextPatrolPoint();
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Count == 0) return;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
        agent.isStopped = false;
    }

    bool HasReachedDestination()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath) return false;
        return agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance);
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _interactDistance);
    }
}