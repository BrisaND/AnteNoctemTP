//TPFinal - Brisa Desouches

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using AnteNoctem.Core;
using AnteNoctem.Enemies;

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
    [Tooltip("El único AudioSource que usará el guardia.")]
    public AudioSource audioSource;
    [Tooltip("El sonido del silbato que sonará al detectar al jugador y de forma periódica durante la persecución.")]
    public AudioClip whistleSound;
    [Tooltip("Pasos pesados corriendo (con Loop activado) que suenan durante toda la persecución.")]
    public AudioClip chaseFootsteps;

    [Header("Intervalos del Silbato")]
    [Tooltip("Tiempo mínimo de silencio entre silbatazos mientras persigue.")]
    public float minWhistleDelay = 2f;
    [Tooltip("Tiempo máximo de silencio entre silbatazos mientras persigue.")]
    public float maxWhistleDelay = 6f;
    [Tooltip("Probabilidad de que sople el silbato en cada ciclo (0.7 = 70% de probabilidad).")]
    [Range(0f, 1f)] public float whistleProbability = 0.7f;

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
    private Coroutine _whistleCoroutine;

    private string currentPlayingAnim = "";

    protected override void Start()
    {
        base.Start();
        baseChaseSpeed = chaseSpeed;
        baseViewDistance = viewDistance;
        baseViewAngle = viewAngle;

        if (wardenAnimator == null) wardenAnimator = GetComponentInChildren<Animator>();

        // Configuración automática del AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // Audio 3D
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        HandleBehavior();
        ControlarAnimacionesPorEstado();

        if (player != null && !playerCtrl.isHidden && Vector3.Distance(transform.position, player.position) < catchDistance)
        {
            TriggerGameOver();
        }
    }

    private void ControlarAnimacionesPorEstado()
    {
        if (wardenAnimator == null) return;

        switch (currentState)
        {
            case WardenState.Patrolling:
                // Si se está moviendo reproduce Caminar; si está esperando en un punto (velocidad 0) reproduce Idle
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
        if (currentPlayingAnim == nombreAnimacion) return;

        if (wardenAnimator != null)
        {
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
                Patrol(); // Usa el sistema de patrullaje con espera de EnemyBase
                if (sees || hears) StartChase();
                break;

            case WardenState.Chasing:
                Chase();
                if (sees || hears)
                {
                    lastKnownPosition = player.position;

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
        if (currentState != WardenState.Chasing)
        {
            CambiarEstado(WardenState.Chasing);
        }

        agent.speed = chaseSpeed;
        lastKnownPosition = player.position;
    }

    void Chase()
    {
        agent.SetDestination(lastKnownPosition);
    }

    void StartSearch()
    {
        CambiarEstado(WardenState.Searching);
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
            CambiarEstado(WardenState.Patrolling);
            GoToNextPatrolPoint();
        }
    }

    private void CambiarEstado(WardenState nuevoEstado)
    {
        if (currentState == nuevoEstado) return;
        currentState = nuevoEstado;

        // Cancela cualquier espera previa de patrulla
        StopWaitCoroutine();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false; // Desbloqueo forzado del agente
        }

        if (_whistleCoroutine != null)
        {
            StopCoroutine(_whistleCoroutine);
            _whistleCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        switch (currentState)
        {
            case WardenState.Patrolling:
                // Al volver a patrullar, nos aseguramos de asignar destino inmediato
                GoToNextPatrolPoint();
                break;

            case WardenState.Chasing:
                if (audioSource != null && chaseFootsteps != null)
                {
                    audioSource.clip = chaseFootsteps;
                    audioSource.loop = true;
                    audioSource.Play();
                }
                _whistleCoroutine = StartCoroutine(CoroutineChaseWhistle());
                break;

            case WardenState.Searching:
                break;
        }
    }

    // Corrutina que gestiona el silbato usando intervalos y probabilidad
    IEnumerator CoroutineChaseWhistle()
    {
        if (whistleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(whistleSound);
            yield return new WaitForSeconds(whistleSound.length);
        }

        while (currentState == WardenState.Chasing)
        {
            float delay = Random.Range(minWhistleDelay, maxWhistleDelay);
            yield return new WaitForSeconds(delay);

            if (currentState == WardenState.Chasing && whistleSound != null && audioSource != null)
            {
                if (Random.value <= whistleProbability)
                {
                    audioSource.PlayOneShot(whistleSound);
                    yield return new WaitForSeconds(whistleSound.length);
                }
            }
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
        if (audioSource != null) audioSource.Stop();
        if (_whistleCoroutine != null) StopCoroutine(_whistleCoroutine);

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