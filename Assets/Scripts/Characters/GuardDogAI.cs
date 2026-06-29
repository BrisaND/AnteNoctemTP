using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using AnteNoctem.Core;
using AnteNoctem.Enemies;

// ===== HERENCIA + CLASE ABSTRACTA =====
// Hereda de EnemyBase. Asi reutiliza el patrullaje, el NavMeshAgent, las referencias al jugador, etc.
public class GuardDogAI : EnemyBase
{
    // ===== ENUM =====
    public enum DogState { Patrolling, Tracking, Dragging }

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

    [Header("Modificadores de Noche")]
    public float nightScentRangeMultiplier = 1.8f;
    [Tooltip("Multiplicador para reducir el stun (menor = se recupera mas rapido)")]
    public float nightStunMultiplier = 0.5f;

    private float baseScentRange;
    private float baseTrackingSpeed;
    private float baseStunAfterEscape;

    [Header("Debug")]
    public bool showGizmo = true;

    public DogState currentState { get; private set; } = DogState.Patrolling;

    private ScentMarker currentTarget;
    private bool minigameActive = false;
    private bool isStunned = false;
    private bool wasPlayerHidden = false;

    protected override void Start()
    {
        base.Start();
        baseScentRange = scentRange;
        baseTrackingSpeed = trackingSpeed;
        baseStunAfterEscape = stunAfterEscape;
    }

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        if (playerCtrl != null)
        {
            // Si el jugador salio de un escondite, decidimos si lo perseguimos o volvemos a patrullar
            if (wasPlayerHidden && !playerCtrl.isHidden)
            {
                if (agent != null) agent.isStopped = false;
                if (Vector3.Distance(transform.position, player.position) <= scentRange)
                {
                    currentState = DogState.Tracking;
                }
                else
                {
                    ReturnToPatrol();
                }
            }
            wasPlayerHidden = playerCtrl.isHidden;

            if (playerCtrl.isHidden)
            {
                CheckHidingSpots();
                return;
            }
        }

        HandleBehavior();
    }

    // ===== METODO ABSTRACTO IMPLEMENTADO =====
    protected override void HandleBehavior()
    {
        switch (currentState)
        {
            case DogState.Patrolling:
                Patrol();
                if (!isStunned) CheckForScent();
                CheckBite();
                break;

            case DogState.Tracking:
                Track();
                CheckBite();
                break;

            case DogState.Dragging:
                break;
        }
    }

    // Busca la marca de olor mas fuerte cerca y se mueve hacia ella
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

    void CheckBite()
    {
        if (player == null) return;
        if (isStunned) return;

        // Si el jugador esta escondido, el perro no puede morderlo
        if (playerCtrl != null && playerCtrl.isHidden) return;

        if (Vector3.Distance(transform.position, player.position) < biteDistance)
        {
            StartDragging();
        }
    }

    void CheckHidingSpots()
    {
        if (playerCtrl != null && playerCtrl.isHidden)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= scentRange)
            {
                bool esTachoVulnerable = playerCtrl.currentHidingSpot != null && playerCtrl.currentHidingSpot.isVulnerableToDog;

                // Si el tacho es de los tirados y el perro viene persiguiendo, lo agarra sin minijuego
                if (esTachoVulnerable && currentState == DogState.Tracking)
                {
                    Debug.Log("El perro se mete al tacho tirado a sacarte.");
                    playerCtrl.ExitHide(playerCtrl.currentHidingSpot.puntoSalida);
                    StartDragging();
                    return;
                }

                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                // Rotamos al perro para que mire fijamente al tacho
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                dirToPlayer.y = 0;
                if (dirToPlayer != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 5f);
                }

                // ===== USO DEL HELPER ESTATICO DE EnemyBase =====
                Transform warden = EnemyBase.FindNearestWardenTransform(transform.position);
                if (warden != null)
                {
                    var wAI = warden.GetComponent<WardenAI>();
                    if (wAI != null)
                    {
                        wAI.AlertToPosition(player.position);
                        Debug.Log("El perro detecto el rastro en el tacho y llamo al Warden.");
                    }
                }
                else
                {
                    if (agent.isStopped) agent.isStopped = false;
                    ReturnToPatrol();
                }
            }
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

        Transform target = EnemyBase.FindNearestWardenTransform(transform.position);
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

        // El jugador zafo del minijuego
        if (playerCtrl != null) playerCtrl.SetControlsEnabled(true);
        StartCoroutine(StunAndKnockback());
    }

    IEnumerator StunAndKnockback()
    {
        isStunned = true;

        // El perro retrocede unos pasos
        Vector3 knockbackDir = -transform.forward;
        Vector3 destination = transform.position + knockbackDir * knockbackDistance;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        currentState = DogState.Patrolling;

        // Aturdido X segundos: no muerde, no rastrea
        float timer = 0f;
        while (timer < stunAfterEscape)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isStunned = false;

        if (patrolPoints.Count > 0) GoToNextPatrolPoint();
    }

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
            StartCoroutine(KeepDragging());
        }
    }

    IEnumerator KeepDragging()
    {
        Transform target = EnemyBase.FindNearestWardenTransform(transform.position);
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

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = currentState == DogState.Tracking ? Color.red :
                       currentState == DogState.Dragging ? Color.magenta : Color.green;
        Gizmos.DrawWireSphere(transform.position, scentRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, biteDistance);
    }

    protected override void ApplyDayNightModifiers()
    {
        base.ApplyDayNightModifiers();
        if (GameManager.Instance == null) return;
        float darkness = 1f - GameManager.Instance.GetDayProgress01();

        scentRange = Mathf.Lerp(baseScentRange, baseScentRange * nightScentRangeMultiplier, darkness);
        trackingSpeed = Mathf.Lerp(baseTrackingSpeed, baseTrackingSpeed * nightSpeedMultiplier, darkness);
        stunAfterEscape = Mathf.Lerp(baseStunAfterEscape, baseStunAfterEscape * nightStunMultiplier, darkness);
    }
}