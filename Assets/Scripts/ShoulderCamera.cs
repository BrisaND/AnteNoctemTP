using UnityEngine;

public class ShoulderCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // el jugador
    public Vector3 shoulderOffset = new Vector3(0.6f, 1.6f, 0f); // derecha, arriba, atras

    [Header("Distancia camara")]
    public float distance = 2.5f;
    public float minDistance = 0.5f;

    [Header("Sensibilidad mouse")]
    public float mouseSensitivityX = 200f;
    public float mouseSensitivityY = 150f;
    public float minPitch = -40f;
    public float maxPitch = 70f;

    [Header("Suavizado")]
    public float positionSmooth = 15f;
    public float rotationSmooth = 20f;

    [Header("Rotacion del jugador")]
    [Tooltip("Si es true, el jugador rota con la camara (estilo RE/shooter). Si es false, rota solo cuando se mueve.")]
    public bool rotatePlayerWithCamera = true;
    public Transform playerBody; // si rotatePlayerWithCamera = true, asignar el transform del jugador

    [Header("Colision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.2f;

    [Header("Cursor")]
    public bool lockCursor = true;

    private float yaw = 0f;
    private float pitch = 10f;

    private bool snapNextFrame = false;


    [Header("Configuración Primera Persona (Escondite)")]
    [Tooltip("Ángulo máximo en grados que el jugador puede mirar a la izquierda o derecha a través de la ranura")]
    public float limitAngleSlot = 45f;

    // NUEVAS VARIABLES PARA PRIMERA PERSONA
    private bool isFirstPerson = false;
    private Transform fpTarget;
    private float fpMinYaw;
    private float fpMaxYaw;

    // NUEVO: Método público para llamar desde el PlayerController
    public void SnapToTarget()
    {
        snapNextFrame = true;
    }

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    void Update()
    {
        // toggle cursor con Tab (util para debug)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // input mouse (solo si el cursor esta lockeado)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            if (isFirstPerson)
            {
                yaw = Mathf.Clamp(yaw, fpMinYaw, fpMaxYaw);
            }
        }

        // rotar al jugador con el yaw solo si no esta en primera persona (para evitar que gire el cuerpo dentro del escondite)
        if (rotatePlayerWithCamera && playerBody != null && !isFirstPerson)
        {
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // calculamos rotacion deseada
        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

        // NUEVO: Comportamiento de Primera Persona dentro del tacho
        if (isFirstPerson && fpTarget != null)
        {
            transform.position = fpTarget.position;
            transform.rotation = targetRot;
            return; // Saltamos todo el cálculo de tercera persona y colisiones
        }

        // pivot en el hombro del jugador
        Vector3 pivot = target.position
                      + target.right * shoulderOffset.x
                      + Vector3.up * shoulderOffset.y
                      + target.forward * shoulderOffset.z;

        // posicion deseada hacia atras desde el pivot
        Vector3 desiredPos = pivot - (targetRot * Vector3.forward) * distance;

        // chequeo de colision con paredes
        Vector3 dirFromPivot = desiredPos - pivot;
        float castDist = dirFromPivot.magnitude;
        if (Physics.SphereCast(pivot, collisionRadius, dirFromPivot.normalized, out RaycastHit hit, castDist, collisionMask))
        {
            float adjustedDist = Mathf.Max(hit.distance, minDistance);
            desiredPos = pivot + dirFromPivot.normalized * adjustedDist;
        }

        // NUEVA LÓGICA DE SUAVIZADO/SALTO
        if (snapNextFrame)
        {
            // Salto instantáneo, sin Lerp
            transform.position = desiredPos;
            transform.rotation = targetRot;
            snapNextFrame = false; // Apagamos la bandera para que el suavizado vuelva en el siguiente frame
        }
        else
        {
            // Suavizado normal
            transform.position = Vector3.Lerp(transform.position, desiredPos, positionSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
        }

    }
    // MÉTODOS PÚBLICOS PARA ACTIVAR/DESACTIVAR DESDE EL JUGADOR
    public void EnterFirstPersonMode(Transform cameraPoint)
    {
        fpTarget = cameraPoint;
        isFirstPerson = true;

        // Seteamos el centro de visión hacia donde apunte la ranura
        yaw = cameraPoint.eulerAngles.y;
        pitch = cameraPoint.eulerAngles.x;

        // Calculamos los límites izquierdo/derecho basados en la ranura
        fpMinYaw = yaw - limitAngleSlot;
        fpMaxYaw = yaw + limitAngleSlot;
    }

    public void ExitFirstPersonMode()
    {
        isFirstPerson = false;
        fpTarget = null;

        // Sincronizamos el yaw actual con el cuerpo del jugador para que no gire bruscamente al salir
        if (playerBody != null)
        {
            yaw = playerBody.eulerAngles.y;
        }
    }
}
