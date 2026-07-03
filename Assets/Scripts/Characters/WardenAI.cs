// Brisa Decouches

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using AnteNoctem.Core;
using AnteNoctem.Enemies;

// Acá solo agregamos lo unico del Warden: ver y oir al jugador, perseguirlo, buscarlo cuando lo pierde.
public class WardenAI : EnemyBase
{
    public enum WardenState { Patrolling, Chasing, Searching }

    [Header("Configuración de Animaciones (Nombres del Asset)")]
    [Tooltip("Referencia al componente Animator del Warden")]
    public Animator wardenAnimator;
    [Tooltip("Tiempo de transición entre estados de animación")]
    public float transitionTime = 0.25f;
    [SerializeField] private string animIdle = "IDLE";
    [SerializeField] private string animCaminar = "WALKING";
    [SerializeField] private string animCorrer = "RUN";
    [SerializeField] private string animBuscar = "MIRAR";

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
    [Tooltip("Radio de búsqueda aleatoria al perder al jugador.")]
    public float searchRadius = 5f;

    [Header("Audio del Warden")]
    [Tooltip("El AudioSource que reproducirá los sonidos del policía.")]
    public AudioSource audioSource;
    [Tooltip("Silbato o grito que suena una sola vez EXACTAMENTE cuando detecta al jugador.")]
    public AudioClip alertSound;
    [Tooltip("Pasos pesados corriendo (con Loop activado) que suenan durante toda la persecución.")]
    public AudioClip chaseFootsteps;

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

    public WardenState currentState { get; private set; } = WardenState.Patrolling;
    private float searchTimer = 0f;
    private Vector3 lastKnownPosition;

    private Coroutine approachCoroutine;

    // Almacena la última animación reproducida para evitar reiniciar clips idénticos en bucle
    private string currentPlayingAnim = "";

    protected override void Start()
    {
        base.Start();
        baseChaseSpeed = chaseSpeed;
        baseViewDistance = viewDistance;
        baseViewAngle = viewAngle;

        if (wardenAnimator == null) wardenAnimator = GetComponentInChildren<Animator>();

        // Configuración automática del AudioSource si no se asignó en el Inspector
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
    }

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        HandleBehavior();
        ControlarAnimacionesPorEstado();

        // Si el jugador esta a tiro y no esta escondido, lo atrapamos
        if (player != null && !playerCtrl.isHidden && Vector3.Distance(transform.position, player.position) < catchDistance)
        {
            TriggerGameOver();
        }
    }

    // --- CONTROL DE ANIMACIONES SEGÚN EL ESTADO ACTUAL Y VELOCIDAD ---
    private void ControlarAnimacionesPorEstado()
    {
        if (wardenAnimator == null) return;

        switch (currentState)
        {
            case WardenState.Patrolling:
                // Evaluamos si el NavMeshAgent se está desplazando realmente
                if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
                {
                    ReproducirAnimacion(animCaminar);
                }
                else
                {
                    ReproducirAnimacion(animIdle);
                }
                break;

            case WardenState.Chasing:
                ReproducirAnimacion(animCorrer);
                break;

            case WardenState.Searching:
                ReproducirAnimacion(animBuscar);
                break;
        }
    }

    private void ReproducirAnimacion(string nombreAnimacion)
    {
        // Evitamos recalcular la transición si ya está corriendo ese clip
        if (currentPlayingAnim == nombreAnimacion) return;

        if (wardenAnimator != null)
        {
            // Forzamos el fundido suavizado usando el sistema directo que lee los clips del asset
            wardenAnimator.CrossFadeInFixedTime(nombreAnimacion, transitionTime);
            currentPlayingAnim = nombreAnimacion;
        }
    }

    protected override void HandleBehavior()
    {
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

                    // Si por algún motivo se pausaron los pasos de carrera pero sigue persiguiendo, los reactivamos
                    if (audioSource != null && chaseFootsteps != null && !audioSource.isPlaying)
                    {
                        audioSource.clip = chaseFootsteps;
                        audioSource.loop = true;
                        audioSource.Play();
                    }
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
        // Solo disparamos el audio de alerta si veníamos de un estado que NO era persecución
        if (currentState != WardenState.Chasing)
        {
            if (audioSource != null)
            {
                // 1. Silbato o grito (No interrumpe el bucle posterior)
                if (alertSound != null)
                {
                    audioSource.PlayOneShot(alertSound);
                }

                // 2. Transición inmediata a pasos corriendo en loop
                if (chaseFootsteps != null)
                {
                    audioSource.clip = chaseFootsteps;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }
        }

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

        // Al perderlo, apagamos el loop de pasos de carrera pesados
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

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

    bool CanHearPlayer()
    {
        if (player == null || playerCtrl == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);
        return dist <= playerCtrl.currentNoiseRadius;
    }

    void TriggerGameOver()
    {
        // Apagamos los bucles de sonido antes del game over
        if (audioSource != null) audioSource.Stop();
        if (GameManager.Instance != null) GameManager.Instance.GameOver("Te atrapo un Warden");
    }

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
        // Al activarse el script cinemático de muerte, también aseguramos que suene la persecución
        StartChase();
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