//TPFinal - Brisa Desouches

using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [Tooltip("Stamina maxima (segundos de corrida al 100%)")]
    public float maxStamina = 10f;
    [Tooltip("Tiempo de bloqueo despues de agotarse (no puede correr)")]
    public float exhaustionLockDuration = 3f;

    [Header("Recarga")]
    [Tooltip("Cuanto se recarga por segundo cuando estas quieto o caminando")]
    public float regenRate = 5f;
    [Tooltip("Cuanto se recarga por segundo cuando estas agachado (recarga mas rapido)")]
    public float regenRateCrouching = 12f;

    [Header("Estado (solo lectura)")]
    public float currentStamina;
    public bool isExhausted = false;

    // Cualquiera puede leer el porcentaje 0-1 para la UI
    public float StaminaPercent => currentStamina / maxStamina;
    public bool CanRun => !isExhausted && currentStamina > 0f;

    private PlayerController playerCtrl;
    private float exhaustionTimer = 0f;

    void Awake()
    {
        playerCtrl = GetComponent<PlayerController>();
    }

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (playerCtrl == null) return;

        // Si esta bloqueado por agotamiento, contamos el timer
        if (isExhausted)
        {
            exhaustionTimer += Time.deltaTime;
            if (exhaustionTimer >= exhaustionLockDuration)
            {
                isExhausted = false;
                exhaustionTimer = 0f;
            }
            // Mientras esta agotado igual recarga stamina
            Regenerate();
            return;
        }

        // Si esta corriendo Y no esta agotado, gasta stamina
        if (playerCtrl.currentState == PlayerController.MoveState.Running)
        {
            currentStamina -= Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
            }
        }
        else
        {
            // Cualquier otro estado (idle, walking, crouching): recarga
            Regenerate();
        }
    }

    void Regenerate()
    {
        // Si esta agachado, recarga mas rapido
        float rate = (playerCtrl.currentState == PlayerController.MoveState.Crouching)
            ? regenRateCrouching
            : regenRate;

        currentStamina += rate * Time.deltaTime;
        if (currentStamina > maxStamina) currentStamina = maxStamina;
    }
}