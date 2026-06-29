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

    [Header("Configuración")]
    public bool randomizeDifficulty = true;
    public RobberySystem.DifficultyLevel difficulty = RobberySystem.DifficultyLevel.Easy;

    [Header("Patrullaje")]
    public List<Transform> patrolPoints = new List<Transform>();

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
        }

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCtrl = p.GetComponent<PlayerController>();
        }
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
                break;
            case CitizenState.Detaining:
                break;
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

        // Avisar al warden para que se acerque
        if (warden != null && player != null)
        {
            warden.AlertToPosition(player.position);
        }

        agent.isStopped = true;
        agent.updatePosition = false;
        agent.velocity = Vector3.zero;

        if (playerCtrl != null) playerCtrl.SetControlsEnabled(false);

        // Mantener al jugador agarrado por X segundos
        Vector3 holdOffset = transform.forward * 0.8f;
        float timer = 0f;
        while (timer < _detentionDuration)
        {
            timer += Time.deltaTime;
            if (player != null)
            {
                player.position = transform.position + holdOffset;
            }
            yield return null;
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