// Brisa Desouches

using UnityEngine;
using System.Collections;
using AnteNoctem.Core;

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
    private AudioClip currentFloorClip;

    public LayerMask maskFloor;

    private RaycastHit _lastFloor;
    private bool _lastStateWasRunning;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();
        if (animator == null) animator = GetComponent<Animator>();
        mainCam = Camera.main;

        currentFloorClip = defaultSound;

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

    void UpdateSoundWalk()
    {
        bool runningActual = (currentState == MoveState.Running);

        // Cambiamos -Vector2.up por -Vector3.up porque estamos en un entorno 3D de Rigidbody
        if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit, 20f, maskFloor))
        {
            // Si pisamos el mismo suelo Y no cambiamos el ritmo (caminar/correr), evitamos procesar de más
            if (_lastFloor.Equals(hit) && _lastStateWasRunning == runningActual)
                return;

            var s = hit.collider.gameObject.GetComponent<ISoundFloor>();

            if (s != null)
            {
                currentFloorClip = s.GetClip(runningActual);
            }
            else
            {
                currentFloorClip = defaultSound;
            }

            _lastFloor = hit;
            _lastStateWasRunning = runningActual;
        }
        else
        {
            _lastFloor = default;
            currentFloorClip = defaultSound;
            _lastStateWasRunning = runningActual;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        bool tieneInputMovimiento = !moveInput.IsNearlyZero();
        bool presionaShift = Input.GetKey(KeyCode.LeftShift);

        // SPRINT INTERRUMPIDO DESDE AGACHADO:
        if (tieneInputMovimiento && isCrouching && presionaShift)
        {
            animator.SetBool("isCrouching", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", true);
        }
        else
        {
            // COMPORTAMIENTO NORMAL
            animator.SetBool("isCrouching", isCrouching);
            animator.SetBool("isWalking", tieneInputMovimiento && !presionaShift);
            animator.SetBool("isRunning", tieneInputMovimiento && !isCrouching && presionaShift);
        }

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
        bool isMoving = !moveInput.IsNearlyZero();
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        if (!isMoving)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            currentState = MoveState.Idle;
            currentNoiseRadius = 0f;
        }
        else if (isCrouching && !isRunning)
        {
            // Si está agachado y no intenta correr, se pausa el sonido de pasos (Sigilo absoluto)
            if (audioSource.isPlaying) audioSource.Pause();
            currentState = MoveState.Crouching;
            currentNoiseRadius = crouchNoiseRadius * noiseMultiplier;
        }
        else if (isRunning)
        {
            // Estado de Sprint (Cancela visualmente el Crouch gracias a UpdateAnimations)
            currentState = MoveState.Running;
            currentNoiseRadius = runNoiseRadius * noiseMultiplier;

            if (!audioSource.isPlaying || audioSource.clip != currentFloorClip)
            {
                audioSource.clip = currentFloorClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            // Caminata Normal de Pie
            currentState = MoveState.Walking;
            currentNoiseRadius = walkNoiseRadius * noiseMultiplier;

            if (!audioSource.isPlaying || audioSource.clip != currentFloorClip)
            {
                audioSource.clip = currentFloorClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }

    float GetCurrentSpeed()
    {
        float baseSpeed = 0f;

        // Si hay input de movimiento y mantenés shift, tu velocidad física es de Run, 
        // incluso si venías de estar agachado
        if (!moveInput.IsNearlyZero() && Input.GetKey(KeyCode.LeftShift))
        {
            return runSpeed * speedMultiplier;
        }

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

        if (camaraJugador != null)
        {
            camaraJugador.enabled = true;
            camaraJugador.EnterFirstPersonMode(cameraPoint);
        }
    }

    private IEnumerator TransitionToExit(Transform targetPos)
    {
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

        if (camaraJugador != null) camaraJugador.enabled = true;
    }

    void OnDrawGizmos()
    {
        if (!showNoiseGizmo) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, currentNoiseRadius);
    }
}