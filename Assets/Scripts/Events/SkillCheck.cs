//TPFinal - Malena Misson

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class SkillCheck : MonoBehaviour
{
    [Header("Configuración Visual")]
    public RectTransform safeZone;
    public float moveSpeed = 100f;
    private float currentSpeed;
    public float hitboxTolerancia = 20f;

    // Evento que notifica el resultado: true = éxito, false = fallo
    public UnityEvent<bool> OnSkillCheckResult;

    private RectTransform pointerTransform;
    private bool active = false;

    void Start()
    {
        pointerTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!active) return;

        // Usar unscaledDeltaTime para que funcione mientras el juego está pausado (timeScale = 0)
        transform.Rotate(0, 0, -currentSpeed * Time.unscaledDeltaTime);

        // detectar input con el nuevo InputSystem
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CheckSuccess();
        }
    }

    // Inicia la prueba
    public void StartSkillCheck()
    {
        active = true;
        if (currentSpeed <= 0) currentSpeed = moveSpeed;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeed = moveSpeed * multiplier;
    }

    // Cancela / detiene la prueba (si es necesario)
    public void StopSkillCheck()
    {
        active = false;
    }

    void CheckSuccess()
    {
        active = false;

        float angleDifference = Quaternion.Angle(pointerTransform.rotation, safeZone.rotation);

        bool success = angleDifference <= hitboxTolerancia;

        if (success)
        {
            Debug.Log("SkillCheck: éxito.");
        }
        else
        {
            Debug.Log("SkillCheck: Fallo.");
        }

        // Notificar resultado
        if (OnSkillCheckResult != null) OnSkillCheckResult.Invoke(success);
    }
}
