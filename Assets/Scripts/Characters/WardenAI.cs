using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using AnteNoctem.Core;
using AnteNoctem.Enemies;

// ===== HERENCIA + CLASE ABSTRACTA =====
// El Warden hereda de EnemyBase. Eso significa que automaticamente tiene patrolPoints,
// el NavMeshAgent, el sistema de modificadores de noche, etc.
// Acá solo agregamos lo unico del Warden: ver y oir al jugador, perseguirlo, buscarlo cuando lo pierde.
public class WardenAI : EnemyBase
{
    // ===== ENUM =====
    public enum WardenState { Patrolling, Chasing, Searching }

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
    public float chaseSpeed = 5f;

    [Header("Busqueda (cuando lo pierde)")]
    public float searchDuration = 5f;
    public float searchRadius = 5f;

    [Header("Modificadores de Noche")]
    [Tooltip("Multiplicador para distancia de vision cuando es de noche")]
    public float nightViewDistanceMultiplier = 1.35f;
    [Tooltip("Multiplicador para angulo de vision cuando es de noche")]
    public float nightViewAngleMultiplier = 1.3f;

    private float baseChaseSpeed;
    private float baseViewDistance;
    private float baseViewAngle;

    [Header("Debug")]
    public bool showVisionGizmo = true;

    // ===== GETTER/SETTER =====
    public WardenState currentState { get; private set; } = WardenState.Patrolling;
    private float searchTimer = 0f;
    private Vector3 lastKnownPosition;

    private Coroutine approachCoroutine;

    // ===== METODO VIRTUAL SOBRESCRITO =====
    // El Start original esta en EnemyBase. Lo sobrescribimos con 'override' para agregar nuestra logica.
    // 'base.Start()' llama al Start del padre asi no perdemos su comportamiento.
    protected override void Start()
    {
        base.Start();
        baseChaseSpeed = chaseSpeed;
        baseViewDistance = viewDistance;
        baseViewAngle = viewAngle;
    }

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        HandleBehavior();

        // Si el jugador esta a tiro y no esta escondido, lo atrapamos
        if (player != null && !playerCtrl.isHidden && Vector3.Distance(transform.position, player.position) < catchDistance)
        {
            TriggerGameOver();
        }
    }

    // ===== METODO ABSTRACTO IMPLEMENTADO =====
    // EnemyBase nos obliga a implementar HandleBehavior. Aca definimos el comportamiento propio del Warden.
    protected override void HandleBehavior()
    {
        // Si el jugador esta escondido en un tacho, el Warden no lo ve ni oye
        bool sees = (playerCtrl != null && playerCtrl.isHidden) ? false : CanSeePlayer();
        bool hears = (playerCtrl != null && playerCtrl.isHidden) ? false : (canHear && CanHearPlayer());

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
    }

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

    // Detecta si el jugador esta dentro del cono de vision y no hay obstaculos en el medio
    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eyePos;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle / 2f) return false;

        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, dist, obstacleMask | playerMask))
        {
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0) return true;
            return false;
        }
        return true;
    }

    // Detecta si el jugador esta haciendo ruido cerca
    bool CanHearPlayer()
    {
        if (player == null || playerCtrl == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);
        return dist <= playerCtrl.currentNoiseRadius;
    }

    void TriggerGameOver()
    {
        if (GameManager.Instance != null) GameManager.Instance.GameOver("Te atrapo un Warden");
    }

    // Permite que otras cosas (camara, ciudadano) le avisen al warden donde esta el jugador
    public void AlertToPosition(Vector3 pos)
    {
        lastKnownPosition = pos;
        StartChase();
    }

    public void ApproachAndExecuteKill(Transform target)
    {
        if (target == null) return;
        if (approachCoroutine != null) StopCoroutine(approachCoroutine);
        approachCoroutine = StartCoroutine(ApproachAndKillRoutine(target));
    }

    private IEnumerator ApproachAndKillRoutine(Transform target)
    {
        currentState = WardenState.Chasing;
        agent.isStopped = false;
        agent.speed = chaseSpeed;

        while (target != null)
        {
            agent.SetDestination(target.position);

            if (!agent.pathPending && agent.remainingDistance <= catchDistance)
            {
                TriggerGameOver();
                agent.isStopped = true;
                yield break;
            }
            yield return null;
        }

        agent.isStopped = true;
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

    // Sobrescribimos el de EnemyBase para agregar nuestras propias modificaciones de noche
    protected override void ApplyDayNightModifiers()
    {
        base.ApplyDayNightModifiers();

        if (GameManager.Instance == null) return;
        float darkness = 1f - GameManager.Instance.GetDayProgress01();

        chaseSpeed = Mathf.Lerp(baseChaseSpeed, baseChaseSpeed * nightSpeedMultiplier, darkness);
        viewDistance = Mathf.Lerp(baseViewDistance, baseViewDistance * nightViewDistanceMultiplier, darkness);
        viewAngle = Mathf.Lerp(baseViewAngle, baseViewAngle * nightViewAngleMultiplier, darkness);
    }
}