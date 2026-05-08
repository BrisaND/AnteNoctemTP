using UnityEngine;

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

    [Header("Debug")]
    public bool showVisionGizmo = true;

    private Transform player;
    private PlayerController playerCtrl;
    private float baseRotationY;
    private float detectionTimer = 0f;
    private float lastAlertTime = -999f;

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
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

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

    void TryAlertWardens()
    {
        if (Time.time - lastAlertTime < alertCooldown) return;
        lastAlertTime = Time.time;

        var wardens = FindObjectsByType<WardenAI>(FindObjectsSortMode.None);
        WardenAI closest = null;
        float minDist = float.MaxValue;
        foreach (var w in wardens)
        {
            if (w == null) continue;
            float d = Vector3.Distance(w.transform.position, player.position);
            if (d < minDist) { minDist = d; closest = w; }
        }

        if (closest != null)
        {
            closest.AlertToPosition(player.position);
            Debug.Log("Camara alerto al Warden");
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
}
