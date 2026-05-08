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
        }

        // rotar al jugador con el yaw
        if (rotatePlayerWithCamera && playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // calculamos rotacion deseada
        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

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

        // suavizado
        transform.position = Vector3.Lerp(transform.position, desiredPos, positionSmooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
    }
}