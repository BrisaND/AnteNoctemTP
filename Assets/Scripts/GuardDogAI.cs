using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardDogAI : MonoBehaviour
{
    public enum DogState { Patrolling, Tracking, Dragging }

    [Header("Patrullaje")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float waitAtPoint = 1.5f;
    public float patrolSpeed = 2.5f;

    [Header("Olfato")]
    [Tooltip("Distancia a la que detecta una marca")]
    public float scentRange = 8f;
    [Tooltip("Distancia a la que considera que llego a la marca")]
    public float scentReachDistance = 0.8f;
    public float trackingSpeed = 4f;

    [Header("Captura")]
    [Tooltip("Distancia a la que el perro te muerde")]
    public float biteDistance = 1.2f;
    [Tooltip("Velocidad de arrastre hacia el Warden")]
    public float dragSpeed = 1.5f;

    [Header("Minijuego")]
    [Tooltip("Cantidad de pulsaciones para zafarse")]
    public int requiredPresses = 15;
    [Tooltip("Tiempo limite para zafarse")]
    public float minigameDuration = 4f;
    [Tooltip("Referencia al EventFlower para el minijuego")]
    public EventFlower escapeMinigame;

    [Header("Escape")]
    [Tooltip("Segundos que el perro queda aturdido despues de que zafes")]
    public float stunAfterEscape = 3f;
    [Tooltip("Distancia que el perro retrocede al ser zafado")]
    public float knockbackDistance = 2f;

    [Header("Debug")]
    public bool showGizmo = true;

    public DogState currentState { get; private set; } = DogState.Patrolling;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerCtrl;
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private ScentMarker currentTarget;
    private bool minigameActive = false;
    private bool isStunned = false;

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

        switch (currentState)
        {
            case DogState.Patrolling:
                Patrol();
                if (!isStunned) CheckForScent(); // no rastrea si esta aturdido
                CheckBite();
                break;

            case DogState.Tracking:
                Track();
                CheckBite();
                break;

            case DogState.Dragging:
                // Movimiento manejado por la coroutine
                break;
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
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
    }

    // ===== OLFATO =====
    void CheckForScent()
    {
        var markers = FindObjectsByType<ScentMarker>(FindObjectsSortMode.None);
        ScentMarker best = null;
        float bestStrength = 0f;

        foreach (var m in markers)
        {
            if (m == null) continue;
            float d = Vector3.Distance(transform.position, m.transform.position);
            if (d > scentRange) continue;

            float strength = m.GetStrength();
            if (strength > bestStrength)
            {
                bestStrength = strength;
                best = m;
            }
        }

        if (best != null)
        {
            currentTarget = best;
            currentState = DogState.Tracking;
        }
    }

    // ===== TRACKING =====
    void Track()
    {
        agent.speed = trackingSpeed;

        if (currentTarget == null)
        {
            ReturnToPatrol();
            return;
        }

        var markers = FindObjectsByType<ScentMarker>(FindObjectsSortMode.None);
        ScentMarker freshest = null;
        float freshestStrength = 0f;

        foreach (var m in markers)
        {
            if (m == null) continue;
            float d = Vector3.Distance(transform.position, m.transform.position);
            if (d > scentRange) continue;

            float s = m.GetStrength();
            if (s > freshestStrength)
            {
                freshestStrength = s;
                freshest = m;
            }
        }

        if (freshest == null)
        {
            ReturnToPatrol();
            return;
        }

        currentTarget = freshest;
        agent.SetDestination(currentTarget.transform.position);
    }

    void ReturnToPatrol()
    {
        currentTarget = null;
        currentState = DogState.Patrolling;
        if (patrolPoints.Count > 0) GoToNextPatrolPoint();
    }

    // ===== MORDIDA =====
    void CheckBite()
    {
        if (player == null) return;
        if (isStunned) return; // no muerde si esta aturdido
        if (Vector3.Distance(transform.position, player.position) < biteDistance)
        {
            StartDragging();
        }
    }

    void StartDragging()
    {
        currentState = DogState.Dragging;
        StartCoroutine(DragPlayerCoroutine());
    }

    IEnumerator DragPlayerCoroutine()
    {
        if (playerCtrl != null) playerCtrl.SetControlsEnabled(false);

        Transform target = FindNearestWardenTransform();
        if (target == null) target = transform;

        var warden = target.GetComponent<WardenAI>();
        if (warden != null) warden.AlertToPosition(transform.position);

        StartEscapeMinigame();

        agent.speed = dragSpeed;

        while (minigameActive)
        {
            agent.SetDestination(target.position);

            if (player != null)
            {
                Vector3 offset = transform.forward * 1f;
                player.position = transform.position + offset;
            }

            if (Vector3.Distance(transform.position, target.position) < 1.5f)
            {
                EndMinigameForceFail();
                if (GameManager.Instance != null) GameManager.Instance.GameOver("El perro te llevo al Warden");
                yield break;
            }

            yield return null;
        }

        // El jugador zafo
        // El jugador zafo
        if (playerCtrl != null) playerCtrl.SetControlsEnabled(true);
        StartCoroutine(StunAndKnockback());
    }

    IEnumerator StunAndKnockback()
    {
        isStunned = true;

        // El perro retrocede unos pasos
        Vector3 knockbackDir = -transform.forward;
        Vector3 destination = transform.position + knockbackDir * knockbackDistance;

        // Si el destino esta en el NavMesh, lo usa, sino se queda donde esta
        if (UnityEngine.AI.NavMesh.SamplePosition(destination, out UnityEngine.AI.NavMeshHit hit, knockbackDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        // Cambia de estado a Patrolling pero seguira aturdido por X segundos
        currentState = DogState.Patrolling;

        // Aturdido: no muerde, no rastrea, solo retrocede
        float timer = 0f;
        while (timer < stunAfterEscape)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isStunned = false;

        // Despues del stun, vuelve a patrullar normal
        if (patrolPoints.Count > 0) GoToNextPatrolPoint();
    }

    Transform FindNearestWardenTransform()
    {
        var wardens = FindObjectsByType<WardenAI>(FindObjectsSortMode.None);
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var w in wardens)
        {
            if (w == null) continue;
            float d = Vector3.Distance(transform.position, w.transform.position);
            if (d < minDist) { minDist = d; closest = w.transform; }
        }
        return closest;
    }

    // ===== MINIJUEGO =====
    void StartEscapeMinigame()
    {
        if (escapeMinigame == null)
        {
            Debug.LogWarning("GuardDog: no hay EventFlower asignado para el minijuego");
            minigameActive = true;
            return;
        }

        escapeMinigame.requiredPresses = requiredPresses;
        escapeMinigame.duration = minigameDuration;
        escapeMinigame.OnSkillCheckResult.AddListener(OnMinigameResult);
        escapeMinigame.StartSkillCheck();
        minigameActive = true;
    }

    void OnMinigameResult(bool success)
    {
        escapeMinigame.OnSkillCheckResult.RemoveListener(OnMinigameResult);
        minigameActive = false;

        if (!success)
        {
            // Si fallo, lo arrastra hasta el Warden
            StartCoroutine(KeepDragging());
        }
    }

    IEnumerator KeepDragging()
    {
        Transform target = FindNearestWardenTransform();
        if (target == null) target = transform;

        while (true)
        {
            agent.SetDestination(target.position);
            if (player != null)
            {
                Vector3 offset = transform.forward * 1f;
                player.position = transform.position + offset;
            }

            if (Vector3.Distance(transform.position, target.position) < 1.5f)
            {
                if (GameManager.Instance != null) GameManager.Instance.GameOver("El perro te llevo al Warden");
                yield break;
            }
            yield return null;
        }
    }

    void EndMinigameForceFail()
    {
        if (escapeMinigame != null && escapeMinigame.IsActive)
        {
            escapeMinigame.Cancel();
        }
        minigameActive = false;
    }

    // ===== UTIL =====
    bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance < 0.5f;
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = currentState == DogState.Tracking ? Color.red :
                       currentState == DogState.Dragging ? Color.magenta : Color.green;
        Gizmos.DrawWireSphere(transform.position, scentRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, biteDistance);
    }
}