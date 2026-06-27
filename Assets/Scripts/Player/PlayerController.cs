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
    [Tooltip("GameObject que contiene TODO el modelo visual (mesh, armature, etc.)")]
    public GameObject contenedorModeloVisual;

    [Header("Crouch")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public CapsuleCollider playerCollider;

    [Header("Ruido (lo escuchan los enemigos)")]
    public Animator animator;
    public float walkNoiseRadius = 4f;
    public float runNoiseRadius = 10f;
    public float crouchNoiseRadius = 1f;

    [Header("Debug")]
    public bool showNoiseGizmo = true;

    [Header("Configuración del Escondite")]
    public float transitionDuration = 0.8f;
    private Coroutine hidingCoroutine;

    [Header("Power-Ups / Beneficios")]
    public float noiseMultiplier = 1.0f;
    public float escapeTimeBonus = 0f;
    public float speedMultiplier = 1.0f;

    [HideInInspector] public HidingSpot currentHidingSpot;
    public enum MoveState { Idle, Walking, Running, Crouching }
    public MoveState currentState { get; private set; } = MoveState.Idle;
    public float currentNoiseRadius { get; private set; } = 0f;
    public bool isCrouching { get; private set; } = false;
    private ShoulderCamera camaraJugador;
    private Vector3 camLocalOffset;
    private Quaternion camLocalRotationOffset;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Camera mainCam;
    private bool controlsEnabled = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip defaultSound;
    public AudioClip walkSound;

    public LayerMask maskFloor;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();
        if (animator == null) animator = GetComponent<Animator>();
        mainCam = Camera.main;
        walkSound = defaultSound;
        if (mainCam != null)
        {
            camaraJugador = mainCam.GetComponent<ShoulderCamera>();
        }

        if (camaraJugador == null)
        {
            camaraJugador = FindFirstObjectByType<ShoulderCamera>();
        }

        if (mainCam != null)
        {
            // Guardamos la posición y rotación de la cámara RELATIVAS al jugador
            camLocalOffset = transform.InverseTransformPoint(mainCam.transform.position);
            camLocalRotationOffset = Quaternion.Inverse(transform.rotation) * mainCam.transform.rotation;
        }
    }

    void Update()
    {
        if (!controlsEnabled) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0, v).normalized;

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrouch();
        }

        UpdateState();
        UpdateAnimations();
        UpdateSoundWalk();
    }
    RaycastHit _lastFloor;
    void UpdateSoundWalk()
    {
        if (Physics.Raycast(transform.position, -Vector2.up, out RaycastHit hit, 20f, maskFloor))
        {
            if (_lastFloor.Equals(hit))
                return;
            var s = hit.collider.gameObject.GetComponent<ISoundFloor>();

            if (s != null)
                walkSound = s.GetClip();

            _lastFloor = hit;
        }
        else
        {
            _lastFloor = default;
            walkSound = defaultSound;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        animator.SetBool("isWalking", isMoving && !isCrouching && !Input.GetKey(KeyCode.LeftShift));
        animator.SetBool("isRunning", isMoving && !isCrouching && Input.GetKey(KeyCode.LeftShift));
        animator.SetBool("isCrouching", isCrouching);

        float velocidadAnimacion = 1f;

        if (speedMultiplier < 1f)
        {
            velocidadAnimacion = speedMultiplier;
        }

        animator.SetFloat("Speed", velocidadAnimacion);
    }

    void FixedUpdate()
    {
        if (!controlsEnabled)
        {
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
        Vector3 forward = transform.forward * moveInput.z;
        Vector3 right = transform.right * moveInput.x;
        Vector3 dir = (forward + right).normalized;

        float speed = GetCurrentSpeed();
        Vector3 velocity = dir * speed;

        if (rb != null && !rb.isKinematic)
        {
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
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
            if (audioSource.isPlaying) audioSource.Stop();
            currentState = MoveState.Idle;
            currentNoiseRadius = 0f;
        }
        else if (isCrouching)
        {
            if (audioSource.isPlaying) audioSource.Pause();
            currentState = MoveState.Crouching;
            currentNoiseRadius = crouchNoiseRadius * noiseMultiplier;
        }
        else if (isRunning)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            currentState = MoveState.Running;
            currentNoiseRadius = runNoiseRadius * noiseMultiplier;
        }
        else
        {
            // Solo reproducir si no estaba sonando ya
            if (!audioSource.isPlaying || audioSource.clip != walkSound)
            {
                audioSource.clip = walkSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            currentState = MoveState.Walking;
            currentNoiseRadius = walkNoiseRadius * noiseMultiplier;
        }
    }

    float GetCurrentSpeed()
    {
        float baseSpeed = 0f;

        switch (currentState)
        {
            case MoveState.Running: baseSpeed = runSpeed; break;
            case MoveState.Crouching: baseSpeed = crouchSpeed; break;
            case MoveState.Walking: baseSpeed = walkSpeed; break;
            default: baseSpeed = 0f; break;
        }

        return baseSpeed * speedMultiplier;
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        if (playerCollider != null)
        {
            float targetHeight = isCrouching ? crouchHeight : standHeight;
            playerCollider.height = targetHeight;

            playerCollider.center = new Vector3(0f, targetHeight / 2f, 0f);
        }
    }

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

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
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

        // se apaga el script de tercera persona durante el viaje ---
        if (camaraJugador != null) camaraJugador.enabled = false;

        if (contenedorModeloVisual != null) contenedorModeloVisual.SetActive(false);

        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        if (playerCollider != null) playerCollider.enabled = false;

        transform.position = targetPos.position;
        transform.rotation = targetPos.rotation;

        Vector3 startCamPos = mainCam.transform.position;
        Quaternion startCamRot = mainCam.transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float percent = elapsed / transitionDuration;
            float t = percent * percent * (3f - 2f * percent);

            mainCam.transform.position = Vector3.Lerp(startCamPos, cameraPoint.position, t);
            mainCam.transform.rotation = Quaternion.Slerp(startCamRot, cameraPoint.rotation, t);

            yield return new WaitForFixedUpdate();
        }

        mainCam.transform.position = cameraPoint.position;
        mainCam.transform.rotation = cameraPoint.rotation;

        // --- se enciende el script de cámara justo antes de entrar a Primera Persona ---
        if (camaraJugador != null)
        {
            camaraJugador.enabled = true;
            camaraJugador.EnterFirstPersonMode(cameraPoint);
        }
    }

    private IEnumerator TransitionToExit(Transform targetPos)
    {
        // --- sale de 1ª persona y apagamos el script ---
        if (camaraJugador != null)
        {
            camaraJugador.ExitFirstPersonMode();
            camaraJugador.enabled = false;
        }

        Vector3 startCamPos = mainCam.transform.position;
        Quaternion startCamRot = mainCam.transform.rotation;

        transform.position = targetPos.position;
        transform.rotation = targetPos.rotation;

        Vector3 targetCamPos = transform.TransformPoint(camLocalOffset);
        Quaternion targetCamRot = transform.rotation * camLocalRotationOffset;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float percent = elapsed / transitionDuration;
            float t = percent * percent * (3f - 2f * percent);

            mainCam.transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);
            mainCam.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, t);

            yield return new WaitForFixedUpdate();
        }

        mainCam.transform.position = targetCamPos;
        mainCam.transform.rotation = targetCamRot;

        if (contenedorModeloVisual != null) contenedorModeloVisual.SetActive(true);

        if (playerCollider != null) playerCollider.enabled = true;
        if (rb != null) rb.isKinematic = false;

        controlsEnabled = true;
        isHidden = false;

        // --- enciende el script de cámara para que vuelva a seguir en 3ª persona ---
        if (camaraJugador != null) camaraJugador.enabled = true;
    }

    void OnDrawGizmos()
    {
        if (!showNoiseGizmo) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, currentNoiseRadius);
    }
}