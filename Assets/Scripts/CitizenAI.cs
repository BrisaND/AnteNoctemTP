using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CitizenAI : MonoBehaviour
{
    public enum CitizenState { Patrolling, Detaining }

    [Header("Patrullaje")]
    public List<Transform> patrolPoints = new List<Transform>();

    [Header("Debug")]
    public bool showGizmo = true;

    public CitizenState currentState { get; private set; } = CitizenState.Patrolling;

    private float _detentionDuration = 4f; //Detencion (si falla)
    private float _interactDistance = 2.8f; //Distancia de interacción con el player
    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerCtrl;
    private int currentPatrolIndex = 0;

    // Nueva referencia al WardenAI
    private WardenAI warden;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Buscar warden en la escena (puedes asignarlo por inspector si prefieres)
        if (warden == null) warden = FindFirstObjectByType<WardenAI>();
    }

    void Start()
    {
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
        // No hacer nada si el juego no está en Playing
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

        if (HasReachedDestination())
        {
            // No se detiene: simplemente fija el siguiente destino y sigue caminando
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
        // No iniciar si no hay QuickEventManager o ya hay un evento activo
        if (QuickEventManager.Instance == null) return;
        if (QuickEventManager.Instance.IsActive) return;

        // No iniciar si el juego no esta en Playing
        if (GameManager.Instance != null &&
            GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        QuickEventManager.Instance.StartQuickEvent(this);
    }

    // Invocado por QuickEventManager cuando termina (true = acierto)
    public void OnPlayerInteractionResult(bool success)
    {
        if (success)
        {
            if (RobberySystem.Instance != null)
            {
                RobberySystem.Instance.AwardStealPoints("Robo al ciudadano");
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

        // Avisar al warden para que se acerque y ejecute su lógica de kill
        if (warden != null && player != null)
        {
            warden.AlertToPosition(player.position);
        }

        // detener al agente mientras detiene al jugador para mantener la interacción coherente
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.velocity = Vector3.zero;

        if (playerCtrl != null) playerCtrl.SetControlsEnabled(false);

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