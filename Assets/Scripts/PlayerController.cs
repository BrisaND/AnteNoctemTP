using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Inputs (Input System)")]
    public InputActionReference controlMove;
    public InputActionReference controlCrouch;
    public InputActionReference controlSprint;

    [Header("Velocidades")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;

    [Header("Referencias")]
    public CapsuleCollider playerCollider;
    public Animator animator;

    [Header("Alturas")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    [Header("Ruido (IA)")]
    public float walkNoiseRadius = 4f;
    public float runNoiseRadius = 10f;
    public float crouchNoiseRadius = 1f;

    // Propiedades para la IA y otros scripts
    public enum MoveState { Idle, Walking, Running, Crouching }
    public MoveState currentState { get; private set; } = MoveState.Idle;
    public float currentNoiseRadius { get; private set; } = 0f;
    public bool isCrouching { get; private set; } = false;
    public bool isRunning { get; private set; } = false;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool controlsEnabled = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            UpdateVisuals(Vector2.zero); // Forzar idle en animaciones si está detenido
            return;
        }

        HandleInput();
        UpdateVisuals(moveInput);
    }

    private void FixedUpdate()
    {
        if (!controlsEnabled) return;
        ApplyMovement();
    }

    private void HandleInput()
    {
        // Leer movimiento
        moveInput = controlMove.action.ReadValue<Vector2>();

        // Toggle Agachado
        if (controlCrouch.action.triggered)
        {
            isCrouching = !isCrouching;
            ActualizarFisicaCrouch();
        }

        // Sprint (Solo si hay movimiento y no está agachado)
        isRunning = controlSprint.action.IsPressed() && !isCrouching && moveInput.magnitude > 0.1f;

        // Definir estado para la IA
        UpdateMoveState();
    }

    private void UpdateMoveState()
    {
        if (moveInput.magnitude < 0.1f)
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

    private void ApplyMovement()
    {
        Vector3 dir = transform.forward * moveInput.y + transform.right * moveInput.x;
        float speed = (currentState == MoveState.Crouching) ? crouchSpeed :
                      (currentState == MoveState.Running) ? runSpeed : walkSpeed;

        if (moveInput.magnitude > 0.1f)
        {
            Vector3 targetVelocity = dir.normalized * speed;
            targetVelocity.y = rb.linearVelocity.y; // Mantener gravedad
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void UpdateVisuals(Vector2 input)
    {
        if (animator == null) return;

        bool moving = input.magnitude > 0.1f;

        // Sincronización con los nombres exactos de tu Animator
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isRunning", isRunning);

        // isWalking es true si se mueve pero NO está corriendo
        animator.SetBool("isWalking", moving && !isRunning);
    }

    private void ActualizarFisicaCrouch()
    {
        if (playerCollider != null)
        {
            float targetHeight = isCrouching ? crouchHeight : standHeight;
            playerCollider.height = targetHeight;
            playerCollider.center = new Vector3(0, targetHeight / 2f, 0);
        }
    }

    // --- MÉTODOS PARA CITIZEN Y WARDEN ---

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            isRunning = false;
        }
    }

    public void ResetMovementState()
    {
        isCrouching = false;
        isRunning = false;
        ActualizarFisicaCrouch();
    }

    private void OnDrawGizmos()
    {
        if (currentNoiseRadius > 0)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, currentNoiseRadius);
        }
    }
}