using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class WardenAI : MonoBehaviour
{
    public enum WardenState { Patrolling, Chasing, Searching }

    [Header("Patrullaje")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float waitAtPoint = 2f;

    [Header("Captura")]
    public float catchDistance = 1.5f;

    [Header("Vision")]
    public float viewDistance = 12f;
    [Range(0f, 360f)] public float viewAngle = 90f;
    public float eyeHeight = 1.7f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Header("Audicion (escucha el ruido del jugador)")]
    public bool canHear = true;

    [Header("Velocidades")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Busqueda (cuando lo pierde)")]
    public float searchDuration = 5f;
    public float searchRadius = 5f;

    [Header("Debug")]
    public bool showVisionGizmo = true;

    public WardenState currentState { get; private set; } = WardenState.Patrolling;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerCtrl;
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private float searchTimer = 0f;
    private Vector3 lastKnownPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        bool sees = CanSeePlayer();
        bool hears = canHear && CanHearPlayer();

        switch (currentState)
        {
            case WardenState.Patrolling:
                Patrol();
                if (sees || hears) StartChase();
                break;

            case WardenState.Chasing:
                Chase();
                if (sees || hears)
                {
                    lastKnownPosition = player.position;
                }
                else if (HasReachedDestination())
                {
                    StartSearch();
                }
                break;

            case WardenState.Searching:
                Search();
                if (sees || hears) StartChase();
                break;
        }

        // chequeo de contacto fuera del switch, asi pasa en cualquier estado
        if (player != null && Vector3.Distance(transform.position, player.position) < catchDistance)
        {
            TriggerGameOver();
        }
    }

    // ===== PATRULLAJE =====
    void Patrol()
    {
        agent.speed = patrolSpeed;
        if (patrolPoints.Count == 0) return;

        if (HasReachedDestination())
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtPoint)
            {
                waitTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Count == 0) return;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
    }

    // ===== PERSECUCION =====
    void StartChase()
    {
        currentState = WardenState.Chasing;
        agent.speed = chaseSpeed;
        lastKnownPosition = player.position;
    }

    void Chase()
    {
        agent.SetDestination(lastKnownPosition);
    }

    // ===== BUSQUEDA =====
    void StartSearch()
    {
        currentState = WardenState.Searching;
        searchTimer = 0f;
        PickRandomSearchPoint();
    }

    void Search()
    {
        agent.speed = patrolSpeed;
        searchTimer += Time.deltaTime;
        if (HasReachedDestination()) PickRandomSearchPoint();
        if (searchTimer >= searchDuration)
        {
            currentState = WardenState.Patrolling;
            GoToNextPatrolPoint();
        }
    }

    void PickRandomSearchPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * searchRadius;
        randomDir += lastKnownPosition;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // ===== DETECCION =====
    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eyePos;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle / 2f) return false;

        // raycast para ver si hay obstaculo
        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, dist, obstacleMask | playerMask))
        {
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0) return true;
            return false;
        }
        return true;
    }

    bool CanHearPlayer()
    {
        if (player == null || playerCtrl == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);
        return dist <= playerCtrl.currentNoiseRadius;
    }

    bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance < 0.5f;
    }

    void TriggerGameOver()
    {
        if (GameManager.Instance != null) GameManager.Instance.GameOver("Te atrapo un Warden");
    }

    // Permite que cosas externas (camara, ciudadano) le avisen al warden
    public void AlertToPosition(Vector3 pos)
    {
        lastKnownPosition = pos;
        StartChase();
    }

    void OnDrawGizmos()
    {
        if (!showVisionGizmo) return;
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = currentState == WardenState.Chasing ? Color.red : Color.yellow;

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawRay(eyePos, leftDir * viewDistance);
        Gizmos.DrawRay(eyePos, rightDir * viewDistance);
        Gizmos.DrawWireSphere(eyePos, viewDistance);
    }
}
