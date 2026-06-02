using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float rotationSpeed = 10f;

    [Header("Estado de Sigilo")]
    public bool isHidden { get; private set; } = false;
    [Tooltip("Arrastra aquí el GameObject que contiene TODO el modelo visual (mesh, armature, etc.)")]
    public GameObject contenedorModeloVisual;

    [Header("Crouch")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public CapsuleCollider playerCollider;

    [Header("Ruido (lo escuchan los enemigos)")]
    [Tooltip("Radio de ruido en metros segun el estado")]
    public Animator animator;
    public float walkNoiseRadius = 4f;
    public float runNoiseRadius = 10f;
    public float crouchNoiseRadius = 1f;

    [Header("Debug")]
    public bool showNoiseGizmo = true;

    [Header("Configuración del Escondite")]
    public float transitionDuration = 0.8f; // Tiempo en segundos que tarda en meterse/salir
    private Coroutine hidingCoroutine;

    [HideInInspector] public HidingSpot currentHidingSpot;

    // Estado actual
    public enum MoveState { Idle, Walking, Running, Crouching }
    public MoveState currentState { get; private set; } = MoveState.Idle;
    public float currentNoiseRadius { get; private set; } = 0f;
    public bool isCrouching { get; private set; } = false;
    private ShoulderCamera camaraJugador;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Camera mainCam;
    private bool controlsEnabled = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // que no se caiga sola
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();
        if (animator == null) animator = GetComponent<Animator>();
        mainCam = Camera.main;

        if (mainCam != null)
        {
            camaraJugador = mainCam.GetComponent<ShoulderCamera>();
        }

        // Fallback: Si por alguna razón el script está en otro objeto, lo busca en la escena
        if (camaraJugador == null)
        {
            camaraJugador = FindFirstObjectByType<ShoulderCamera>();
        }
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
        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Sincronizar parámetros con el Animator Controller
        animator.SetBool("isWalking", isMoving && !isCrouching && !Input.GetKey(KeyCode.LeftShift));
        animator.SetBool("isRunning", isMoving && !isCrouching && Input.GetKey(KeyCode.LeftShift));
        animator.SetBool("isCrouching", isCrouching);
    }

    void FixedUpdate()
    {
        if (!controlsEnabled)
        {
            // Si hay Rigidbody y no es kinematic, detenerlo; si es kinematic no tocarlo.
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
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

        // conservar componente Y actual si existe Rigidbody no-kinematic
        float currentY = 0f;
        if (rb != null && !rb.isKinematic)
        {
            currentY = rb.linearVelocity.y;
            velocity.y = currentY;
            rb.linearVelocity = velocity;
        }
        else
        {
            // Si Rigidbody es kinematic (por ejemplo forzado por PressurePlate), mover por transform como fallback
            velocity.y = 0f;
            transform.position += velocity * Time.fixedDeltaTime;
        }
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
            float center = playerCollider.center.y;
            float targetHeight = isCrouching ? crouchHeight : standHeight;
            playerCollider.height = targetHeight;
            // ajustamos el centro para que no flote
            playerCollider.center = new Vector3(0, center, 0);
        }
    }

    // Resetea el movimiento del jugador luego de caerse, para evitar que quede con velocidad residual o en estado incorrecto
    public void ResetMovementState()
    {
        moveInput = Vector3.zero;
        currentState = MoveState.Idle;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Permite habilitar/deshabilitar controles (usado por el ciudadano si detiene al jugador)
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            // parar inmediatamente solo si Rigidbody está en modo físico
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
    }

    public void EnterHide(Transform puntoEscondite, Transform puntoCamaraInterna)
    {
        if (hidingCoroutine != null) StopCoroutine(hidingCoroutine);
        hidingCoroutine = StartCoroutine(TransitionToHide(puntoEscondite, puntoCamaraInterna));
    }

    public void ExitHide(Transform puntoSalida)
    {
        if (hidingCoroutine != null) StopCoroutine(hidingCoroutine);
        hidingCoroutine = StartCoroutine(TransitionToExit(puntoSalida));
    }

    private IEnumerator TransitionToHide(Transform targetPos, Transform cameraPoint)
    {
        isHidden = true;
        controlsEnabled = false;

        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        if (playerCollider != null) playerCollider.enabled = false;

        // TRANSICIÓN FLUIDA: Desplazamiento suave hacia adentro del tacho
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / transitionDuration;

            // Suavizado de interpolación (SmoothStep)
            float t = percent * percent * (3f - 2f * percent);

            transform.position = Vector3.Lerp(startPos, targetPos.position, t);
            transform.rotation = Quaternion.Slerp(startRot, targetPos.rotation, t);
            yield return null;
        }

        // Aseguramos posición final exacta
        transform.position = targetPos.position;
        transform.rotation = targetPos.rotation;

        // CAMBIO A PRIMERA PERSONA: Ocultamos malla y activamos el modo de visualización ranura
        if (contenedorModeloVisual != null) contenedorModeloVisual.SetActive(false);
        if (camaraJugador != null) camaraJugador.EnterFirstPersonMode(cameraPoint);
    }

    private IEnumerator TransitionToExit(Transform targetPos)
    {
        // Volvemos a activar la tercera persona y el render antes de salir físicamente
        if (camaraJugador != null) camaraJugador.ExitFirstPersonMode();
        if (contenedorModeloVisual != null) contenedorModeloVisual.SetActive(true);

        // TRANSICIÓN FLUIDA: Salida controlada hacia el punto exterior seguro
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / transitionDuration;
            float t = percent * percent * (3f - 2f * percent);

            transform.position = Vector3.Lerp(startPos, targetPos.position, t);
            transform.rotation = Quaternion.Slerp(startRot, targetPos.rotation, t);
            yield return null;
        }

        transform.position = targetPos.position;
        transform.rotation = targetPos.rotation;

        // Reactivación total de físicas normales
        if (playerCollider != null) playerCollider.enabled = true;
        if (rb != null) rb.isKinematic = false;

        controlsEnabled = true;
        isHidden = false;
    }
    void OnDrawGizmos()
    {
        if (!showNoiseGizmo) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, currentNoiseRadius);
    }
}
