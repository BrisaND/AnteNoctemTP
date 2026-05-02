using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float rotationSpeed = 10f;

    [Header("Crouch")]
    public float standHeight = 1.8f;
    public float crouchHeight = 1f;
    public CapsuleCollider playerCollider;

    [Header("Ruido (lo escuchan los enemigos)")]
    [Tooltip("Radio de ruido en metros segun el estado")]
    public float walkNoiseRadius = 4f;
    public float runNoiseRadius = 10f;
    public float crouchNoiseRadius = 1f;

    [Header("Debug")]
    public bool showNoiseGizmo = true;

    // Estado actual
    public enum MoveState { Idle, Walking, Running, Crouching }
    public MoveState currentState { get; private set; } = MoveState.Idle;
    public float currentNoiseRadius { get; private set; } = 0f;
    public bool isCrouching { get; private set; } = false;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Camera mainCam;

    private bool controlsEnabled = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // que no se caiga sola
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();
        mainCam = Camera.main;
    }

    void Update()
    {
        if (!controlsEnabled) return;

        // Input movimiento (WASD / flechas)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0, v).normalized;

        // Toggle crouch (Ctrl izq o C)
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrouch();
        }

        UpdateState();
    }

    void FixedUpdate()
    {
        if (!controlsEnabled)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Move();
    }

    void Move()
    {
        // movimiento relativo a la rotacion del jugador (que ahora rota con la camara)
        Vector3 forward = transform.forward * moveInput.z;
        Vector3 right = transform.right * moveInput.x;
        Vector3 dir = (forward + right).normalized;

        float speed = GetCurrentSpeed();
        Vector3 velocity = dir * speed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    void UpdateState()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        if (!isMoving)
        {
            currentState = MoveState.Idle;

            currentNoiseRadius = 0f;
        }
        else if (isCrouching)
        {
            currentState = MoveState.Crouching;
            currentNoiseRadius = crouchNoiseRadius;
        }
        else if (isRunning)
        {
            currentState = MoveState.Running;
            currentNoiseRadius = runNoiseRadius;
        }
        else
        {
            currentState = MoveState.Walking;
            currentNoiseRadius = walkNoiseRadius;
        }
    }

    float GetCurrentSpeed()
    {
        switch (currentState)
        {
            case MoveState.Running: return runSpeed;
            case MoveState.Crouching: return crouchSpeed;
            case MoveState.Walking: return walkSpeed;
            default: return 0f;
        }
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        if (playerCollider != null)
        {
            playerCollider.height = isCrouching ? crouchHeight : standHeight;
            // ajustamos el centro para que no flote
            playerCollider.center = new Vector3(0, playerCollider.height / 2f, 0);
        }
    }

    // Permite habilitar/deshabilitar controles (usado por el ciudadano si detiene al jugador)
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            // parar inmediatamente
            rb.linearVelocity = Vector3.zero;
        }
    }

    void OnDrawGizmos()
    {
        if (!showNoiseGizmo) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, currentNoiseRadius);
    }
}
