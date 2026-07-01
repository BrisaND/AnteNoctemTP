// Malena Farias

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CitizenAI : MonoBehaviour
{
    // En vez de usar numeros sueltos (0 = patrullando, 1 = deteniendo), usamos nombres.
    public enum CitizenState { Patrolling, Detaining }

    [Header("Configuración de Animaciones (Nombres del Asset)")]
    [Tooltip("Referencia al componente Animator del Ciudadano")]
    public Animator citizenAnimator;
    [Tooltip("Tiempo de transición entre estados de animación")]
    public float transitionTime = 0.25f;
    [SerializeField] private string animIdle = "MIRARSE";
    [SerializeField] private string animCaminar = "WALKINGPONE";
    [SerializeField] private string animSujetar = "SURPRISE";

    [Header("Configuración")]
    public bool randomizeDifficulty = true;
    public RobberySystem.DifficultyLevel difficulty = RobberySystem.DifficultyLevel.Easy;

    [Header("Patrullaje")]
    public List<Transform> patrolPoints = new List<Transform>();

    [Header("Efectos Visuales")]
    [Tooltip("Arrastrá acá el Prefab FX_RoboPesos que configuraste")]
    public GameObject pesosParticlesPrefab;
    [Tooltip("Punto de origen opcional (si se deja vacío, saldrán del centro del ciudadano)")]
    public Transform particleSpawnPoint;

    [Header("Debug")]
    public bool showGizmo = true;

    // Cualquiera puede leer el estado actual, pero solo este script puede cambiarlo
    public CitizenState currentState { get; private set; } = CitizenState.Patrolling;

    // Variables privadas que nadie de afuera puede modificar
    private float _detentionDuration = 4f;
    private float _interactDistance = 2.8f;

    // El ciudadano TIENE estos componentes adentro
    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerCtrl;
    private int currentPatrolIndex = 0;

    private WardenAI warden;

    // Almacena la última animación reproducida para evitar reiniciar clips idénticos en cada frame
    private string currentPlayingAnim = "";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (warden == null) warden = FindFirstObjectByType<WardenAI>();
    }

    void Start()
    {
        // Decidimos la dificultad del SkillCheck con una tirada random
        if (randomizeDifficulty)
        {
            float roll = Random.value;
            if (roll < 0.6f) difficulty = RobberySystem.DifficultyLevel.Easy;
            else if (roll < 0.9f) difficulty = RobberySystem.DifficultyLevel.Medium;
            else difficulty = RobberySystem.DifficultyLevel.Hard;

            Debug.Log($"Citizen {gameObject.name} | roll={roll} | dificultad={difficulty}");
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
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        switch (currentState)
        {
            case CitizenState.Patrolling:
                Patrol();
                CheckPlayerInteractionWhileMoving();
                ControlarAnimacionMovimiento();
                break;
            case CitizenState.Detaining:
                // Forzamos la animación de forcejeo o sujeción mientras dura la corrutina
                ReproducirAnimacion(animSujetar);
                break;
        }
    }

    // --- CONTROL DE ANIMACIÓN EN PATRULLA ---
    private void ControlarAnimacionMovimiento()
    {
        if (citizenAnimator == null) return;

        // Evaluamos la velocidad real en el NavMeshAgent para decidir si camina o se queda quieto
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

        // Al llegar, va al siguiente punto sin detenerse
        if (HasReachedDestination())
        {
            GoToNextPatrolPoint();
        }
    }

    void CheckPlayerInteractionWhileMoving()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= _interactDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartInteraction();
            }
        }
    }

    void StartInteraction()
    {
        if (QuickEventManager.Instance == null) return;
        if (QuickEventManager.Instance.IsActive) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        QuickEventManager.Instance.StartQuickEvent(this);
    }

    // El QuickEventManager nos avisa si el jugador zafo del skillcheck
    public void OnPlayerInteractionResult(bool success)
    {
        if (success)
        {
            if (RobberySystem.Instance != null)
            {
                RobberySystem.Instance.AwardStealPoints(difficulty, "Robo al ciudadano");
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(Random.Range(5, 16));
            }

            //Ráfaga de partículas de dinero
            if (pesosParticlesPrefab != null)
            {
                Vector3 spawnPos = particleSpawnPoint != null ? particleSpawnPoint.position : transform.position;
                Instantiate(pesosParticlesPrefab, spawnPos, Quaternion.identity);
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

        if (warden != null && player != null)
        {
            warden.AlertToPosition(player.position);
        }

        agent.isStopped = true;
        agent.updatePosition = false;
        agent.velocity = Vector3.zero;

        if (playerCtrl != null) playerCtrl.SetControlsEnabled(false);

        // Guardamos la altura Y original del jugador (la del piso)
        float playerGroundY = player != null ? player.position.y : 0f;

        // Congelamos temporalmente el Rigidbody del jugador para que no forcejee con nuestro movimiento manual
        Rigidbody playerRb = player != null ? player.GetComponent<Rigidbody>() : null;
        bool wasKinematic = false;
        if (playerRb != null)
        {
            wasKinematic = playerRb.isKinematic;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }

        // Mantener al jugador agarrado por X segundos, siempre a la altura del piso
        Vector3 holdOffsetXZ = transform.forward * 0.8f;
        float timer = 0f;
        while (timer < _detentionDuration)
        {
            timer += Time.deltaTime;
            if (player != null)
            {
                Vector3 targetPos = transform.position + holdOffsetXZ;
                targetPos.y = playerGroundY; // Forzamos que se mantenga a nivel del piso
                player.position = targetPos;
            }
            yield return null;
        }

        // Restauramos el Rigidbody para que vuelva a caer con gravedad
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