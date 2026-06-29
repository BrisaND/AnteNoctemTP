using UnityEngine;
using AnteNoctem.Enemies;

public class SurveillanceCamera : MonoBehaviour
{
    [Header("Vision")]
    public float viewDistance = 10f;
    [Range(0f, 360f)] public float viewAngle = 60f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Header("Rotacion")]
    [Tooltip("Grados que oscila a cada lado del centro")]
    public float swingAngle = 60f;
    [Tooltip("Velocidad de oscilacion en grados por segundo")]
    public float swingSpeed = 30f;

    [Header("Deteccion")]
    [Tooltip("Tiempo que tarda en alertar despues de verte")]
    public float detectionDelay = 1f;
    [Tooltip("Si el crouch reduce el rango de vision")]
    public bool crouchReducesDetection = true;
    [Range(0f, 1f)] public float crouchDetectionMultiplier = 0.5f;

    [Header("Alerta")]
    [Tooltip("Cuantos segundos antes de poder volver a alertar")]
    public float alertCooldown = 5f;

    [Header("Modificadores de Noche")]
    public float nightViewDistanceMultiplier = 1.6f;
    public float nightSwingSpeedMultiplier = 1.7f;
    [Tooltip("Multiplicador para reducir el delay de deteccion (menor = detecta mas rapido)")]
    public float nightDetectionDelayMultiplier = 0.4f;

    private float baseViewDistance;
    private float baseSwingSpeed;
    private float baseDetectionDelay;

    [Header("Debug")]
    public bool showVisionGizmo = true;

    private Transform player;
    private PlayerController playerCtrl;
    private float baseRotationY;
    private float detectionTimer = 0f;
    private float lastAlertTime = -999f;

    // ===== GETTER/SETTER =====
    public bool IsDetecting { get; private set; } = false;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCtrl = p.GetComponent<PlayerController>();
        }
        baseRotationY = transform.eulerAngles.y;

        baseViewDistance = viewDistance;
        baseSwingSpeed = swingSpeed;
        baseDetectionDelay = detectionDelay;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        ApplyDayNightModifiers();
        Swing();

        bool sees = CanSeePlayer();
        IsDetecting = sees;

        if (sees)
        {
            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionDelay)
            {
                TryAlertWardens();
                detectionTimer = 0f;
            }
        }
        else
        {
            detectionTimer = 0f;
        }
    }

    // Hace oscilar la camara como un barrido de seguridad
    void Swing()
    {
        float t = Mathf.PingPong(Time.time * swingSpeed / (swingAngle * 2f), 1f);
        float angle = Mathf.Lerp(-swingAngle, swingAngle, t);
        transform.rotation = Quaternion.Euler(0, baseRotationY + angle, 0);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 toPlayer = (player.position + Vector3.up * 1f) - transform.position;
        float dist = toPlayer.magnitude;

        // Si esta agachado, la camara lo ve a menor distancia
        float effectiveDistance = viewDistance;
        if (crouchReducesDetection && playerCtrl != null && playerCtrl.isCrouching)
        {
            effectiveDistance *= crouchDetectionMultiplier;
        }

        if (dist > effectiveDistance) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle / 2f) return false;

        if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hit, dist, obstacleMask | playerMask))
        {
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0) return true;
            return false;
        }
        return true;
    }

    // Avisa al Warden mas cercano usando el helper estatico de EnemyBase
    void TryAlertWardens()
    {
        if (Time.time - lastAlertTime < alertCooldown) return;
        lastAlertTime = Time.time;

        // ===== USO DEL HELPER ESTATICO DE EnemyBase =====
        Transform wardenTransform = EnemyBase.FindNearestWardenTransform(player.position);
        if (wardenTransform != null)
        {
            var warden = wardenTransform.GetComponent<WardenAI>();
            if (warden != null)
            {
                warden.AlertToPosition(player.position);
                Debug.Log("Camara alerto al Warden");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showVisionGizmo) return;
        Gizmos.color = IsDetecting ? Color.red : Color.cyan;

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir * viewDistance);
        Gizmos.DrawRay(transform.position, rightDir * viewDistance);
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }

    void ApplyDayNightModifiers()
    {
        if (GameManager.Instance == null) return;

        float dayProgress = GameManager.Instance.GetDayProgress01();
        float darkness = 1f - dayProgress;

        viewDistance = Mathf.Lerp(baseViewDistance, baseViewDistance * nightViewDistanceMultiplier, darkness);
        swingSpeed = Mathf.Lerp(baseSwingSpeed, baseSwingSpeed * nightSwingSpeedMultiplier, darkness);
        detectionDelay = Mathf.Lerp(baseDetectionDelay, baseDetectionDelay * nightDetectionDelayMultiplier, darkness);
    }
}