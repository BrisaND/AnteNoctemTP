// Brisa Desouches

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using AnteNoctem.Core;
using AnteNoctem.Enemies;

// Hereda de EnemyBase.
public class GuardDogAI : EnemyBase
{
    public enum DogState { Patrolling, Tracking, Dragging }

    [Header("Animaciones (Opción A - IDs del Asset)")]
    [Tooltip("Referencia al componente Animator del perro")]
    public Animator dogAnimator;
    [SerializeField] private int idIdle = 0;
    [SerializeField] private int idMovimiento = 2;
    [SerializeField] private int idLadrar = 6;
    [SerializeField] private int idArrastrar = 5;
    [SerializeField] private int idAturdido = 1;

    // Nombre exacto del parámetro que controla el estado en el asset
    private readonly string PARAM_ANIMATION_ID = "AnimationID";

    [Header("Olfato")]
    [Tooltip("Distancia a la que detecta una marca")]
    public float scentRange = 8f;
    [Tooltip("Distancia a la que considera que llego a la marca")]
    public float scentReachDistance = 0.4f;
    public float trackingSpeed = 4f;

    [Header("Captura")]
    [Tooltip("Punto exacto (objeto vacío hijo del perro) donde se acoplará el jugador")]
    public Transform biteAnchor;
    [Tooltip("Distancia a la que el perro te muerde")]
    public float biteDistance = 1.2f;
    [Tooltip("Velocidad de arrastre hacia el Warden")]
    public float dragSpeed = 1.5f;

    [Header("Minijuego")]
    [Tooltip("Cantidad de pulsaciones para zafarse")]
    public int requiredPresses = 15;
    [Tooltip("Referencia al EventFlower para el minijuego")]
    public EventFlower escapeMinigame;

    [Header("Escape")]
    [Tooltip("Segundos que el perro queda aturdido despues de que zafes")]
    public float stunAfterEscape = 3f;
    [Tooltip("Distancia que el perro retrocede al ser zafado")]
    public float knockbackDistance = 2.5f;

    [Header("Audio del Perro - Configuración")]
    [Tooltip("El AudioSource que reproducirá los sonidos del perro.")]
    public AudioSource audioSource;

    [Space(5)]
    [Tooltip("Ladrido para persecución y alertas.")]
    public AudioClip barkTrackingSound;
    [Tooltip("Gruñido agresivo continuo para cuando te muerde y te arrastra.")]
    public AudioClip growlDraggingSound;

    [Header("Intervalos de Sonido (Tracking)")]
    [Tooltip("Tiempo mínimo de silencio entre ladridos/gruñidos durante el rastreo.")]
    public float minSoundDelay = 0.5f;
    [Tooltip("Tiempo máximo de silencio entre ladridos/gruñidos durante el rastreo.")]
    public float maxSoundDelay = 2.5f;
    [Tooltip("Probabilidad de que suene un Ladrido en lugar de un Gruñido (0.7 = 70% ladrido, 30% gruñido).")]
    [Range(0f, 1f)] public float barkProbability = 0.7f;

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

    private Coroutine _CoroutineDog;

    protected override void Start()
    {
        base.Start();
        baseScentRange = scentRange;
        baseTrackingSpeed = trackingSpeed;
        baseStunAfterEscape = stunAfterEscape;

        if (dogAnimator == null) dogAnimator = GetComponentInChildren<Animator>();

        // Configuración automática del AudioSource si falta asignarlo
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // Audio 3D posicional
            }
        }

        // Suscribirse al evento de la flor si está asignada
        if (escapeMinigame != null)
        {
            escapeMinigame.OnSkillCheckResult.AddListener(OnMinigameResult);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        // --- CONTROL AUTOMÁTICO DE LOCOMOCIÓN (IDLE / MOVIMIENTO) ---
        if (!isStunned && currentState != DogState.Dragging)
        {
            if (playerCtrl == null || !playerCtrl.isHidden)
            {
                if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
                {
                    CambiarAnimacion(idMovimiento);
                }
                else
                {
                    CambiarAnimacion(idIdle);
                }
            }
        }

        if (playerCtrl != null)
        {
            if (wasPlayerHidden && !playerCtrl.isHidden)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
                if (Vector3.Distance(transform.position, player.position) <= scentRange)
                {
                    CambiarEstado(DogState.Tracking);
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

    protected override void HandleBehavior()
    {
        switch (currentState)
        {
            case DogState.Patrolling:
                PatrollBehavior();
                break;

            case DogState.Tracking:
                Track();
                CheckBite();
                break;

            case DogState.Dragging:
                break;
        }
    }

    private void PatrollBehavior()
    {
        Patrol();
        if (!isStunned) CheckForScent();
        CheckBite();
    }

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
            CambiarEstado(DogState.Tracking);
        }
    }

    void Track()
    {
        if (agent != null && agent.isOnNavMesh) agent.speed = trackingSpeed;

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
        if (agent != null && agent.isOnNavMesh) agent.SetDestination(currentTarget.transform.position);
    }

    void ReturnToPatrol()
    {
        currentTarget = null;
        StopWaitCoroutine(); // Limpia el estado de espera anterior

        if (agent != null && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.isStopped = false;
        }

        CambiarEstado(DogState.Patrolling);
        if (patrolPoints != null && patrolPoints.Count > 0) GoToNextPatrolPoint();
    }

    void CheckBite()
    {
        if (player == null) return;
        if (isStunned) return;
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

                if (esTachoVulnerable && currentState == DogState.Tracking)
                {
                    Debug.Log("El perro se mete al tacho tirado a sacarte.");
                    playerCtrl.ExitHide(playerCtrl.currentHidingSpot.puntoSalida);
                    StartDragging();
                    return;
                }

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                CambiarAnimacion(idLadrar);

                // Si descubrimos al jugador escondido, ladramos frenéticamente
                if (audioSource != null && barkTrackingSound != null && audioSource.clip != barkTrackingSound)
                {
                    if (_CoroutineDog != null)
                        StopCoroutine(_CoroutineDog);

                    _CoroutineDog = StartCoroutine(CoroutineHidingSpotBarks());
                }

                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                dirToPlayer.y = 0;
                if (dirToPlayer != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 5f);
                }

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
                    if (agent != null && agent.isOnNavMesh && agent.isStopped) agent.isStopped = false;
                    ReturnToPatrol();
                }
            }
        }
    }

    // CORRUTINA A: Alterna ladridos y gruñidos aleatoriamente mientras persigue al jugador
    IEnumerator CoroutineDogSounds()
    {
        while (currentState == DogState.Tracking)
        {
            AudioClip clipToPlay = null;
            float roll = Random.value;

            if (roll <= barkProbability)
            {
                clipToPlay = barkTrackingSound;
            }
            else
            {
                clipToPlay = growlDraggingSound;
            }

            if (clipToPlay != null && audioSource != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.loop = false;
                audioSource.Play();

                yield return new WaitForSeconds(clipToPlay.length);
            }

            float randomDelay = Random.Range(minSoundDelay, maxSoundDelay);
            yield return new WaitForSeconds(randomDelay);
        }
    }

    // CORRUTINA B: Ladrido constante al encontrar al jugador en un escondite
    IEnumerator CoroutineHidingSpotBarks()
    {
        if (barkTrackingSound == null || audioSource == null) yield break;

        audioSource.clip = barkTrackingSound;
        audioSource.loop = false;

        while (true)
        {
            audioSource.Play();
            yield return new WaitForSeconds(barkTrackingSound.length + Random.Range(0.15f, 0.4f));
        }
    }

    void StartDragging()
    {
        if (isStunned) return;
        CambiarEstado(DogState.Dragging);

        Collider dogCollider = GetComponent<Collider>();
        if (player != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (dogCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(dogCollider, playerCollider, true);
            }

            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            if (biteAnchor == null)
            {
                GameObject backupAnchor = new GameObject("BiteAnchor_Backup");
                backupAnchor.transform.SetParent(transform);
                backupAnchor.transform.localPosition = new Vector3(0f, 0.2f, 1.1f);
                biteAnchor = backupAnchor.transform;
            }

            player.SetParent(biteAnchor);
            player.localPosition = Vector3.zero;
            player.localRotation = Quaternion.identity;
        }

        if (escapeMinigame != null)
        {
            escapeMinigame.useTimeLimit = false;
            escapeMinigame.requiredPresses = requiredPresses;
            escapeMinigame.StartSkillCheck();
            minigameActive = true;
        }
        else
        {
            Debug.LogWarning("GuardDog: no hay EventFlower asignado para el minijuego");
            minigameActive = true;
        }

        StartCoroutine(DragPlayerCoroutine());
    }

    IEnumerator DragPlayerCoroutine()
    {
        if (playerCtrl != null) playerCtrl.SetControlsEnabled(false);

        Transform target = EnemyBase.FindNearestWardenTransform(transform.position);
        if (target == null) target = transform;

        var warden = target.GetComponent<WardenAI>();
        if (warden != null) warden.AlertToPosition(transform.position);

        if (agent != null && agent.isOnNavMesh) agent.speed = dragSpeed;

        while (minigameActive)
        {
            if (agent != null && agent.isOnNavMesh) agent.SetDestination(target.position);
            CambiarAnimacion(idArrastrar);

            if (player != null && biteAnchor != null)
            {
                player.position = biteAnchor.position;
                player.rotation = biteAnchor.rotation;
            }

            if (Vector3.Distance(transform.position, target.position) < 1.5f)
            {
                minigameActive = false;
                if (escapeMinigame != null) escapeMinigame.Cancel();

                if (_CoroutineDog != null)
                    StopCoroutine(_CoroutineDog);

                _CoroutineDog = null;
                if (player != null) player.SetParent(null);

                if (GameManager.Instance != null) GameManager.Instance.GameOver("El perro te llevo al Warden");
                yield break;
            }

            yield return null;
        }
    }

    void OnMinigameResult(bool success)
    {
        if (success && minigameActive)
        {
            minigameActive = false;
            if (playerCtrl != null) playerCtrl.SetControlsEnabled(true);
            StartCoroutine(StunAndKnockback());
        }
    }

    IEnumerator StunAndKnockback()
    {
        isStunned = true;

        if (audioSource != null) audioSource.Stop();
        if (_CoroutineDog != null)
        {
            StopCoroutine(_CoroutineDog);
            _CoroutineDog = null;
        }

        if (player != null)
        {
            player.SetParent(null);
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = true;
        }

        Vector3 knockbackDir = -transform.forward;
        Vector3 destination = transform.position + knockbackDir * knockbackDistance;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
        {
            if (agent != null && agent.isOnNavMesh) agent.SetDestination(hit.position);
        }

        yield return new WaitForFixedUpdate();

        Collider dogCollider = GetComponent<Collider>();
        if (player != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (dogCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(dogCollider, playerCollider, false);
            }
        }

        CambiarEstado(DogState.Patrolling);

        float timer = 0f;
        while (timer < stunAfterEscape)
        {
            CambiarAnimacion(idAturdido);
            timer += Time.deltaTime;
            yield return null;
        }

        isStunned = false;
        if (patrolPoints != null && patrolPoints.Count > 0) GoToNextPatrolPoint();
    }

    private void CambiarEstado(DogState nuevoEstado)
    {
        if (currentState == nuevoEstado) return;
        currentState = nuevoEstado;

        // Limpia cualquier estado de espera previo
        StopWaitCoroutine();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (_CoroutineDog != null)
        {
            StopCoroutine(_CoroutineDog);
            _CoroutineDog = null;
        }

        if (audioSource != null) audioSource.Stop();

        switch (currentState)
        {
            case DogState.Patrolling:
                break;

            case DogState.Tracking:
                _CoroutineDog = StartCoroutine(CoroutineDogSounds());
                break;

            case DogState.Dragging:
                if (audioSource != null && growlDraggingSound != null)
                {
                    audioSource.clip = growlDraggingSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
                break;
        }
    }

    private void CambiarAnimacion(int id)
    {
        if (dogAnimator != null)
        {
            dogAnimator.SetInteger(PARAM_ANIMATION_ID, id);
        }
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