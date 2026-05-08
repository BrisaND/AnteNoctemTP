using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class SkillCheck : MonoBehaviour
{
    [Header("Configuración Visual")]
    public RectTransform safeZone;
    public float moveSpeed = 100f;
    public float hitboxTolerancia = 20f;

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

        // Usar unscaledDeltaTime para que funcione mientras el juego está pausado
        transform.Rotate(0, 0, -moveSpeed * Time.unscaledDeltaTime);

        // detectar input
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CheckSuccess();
        }
    }

    // Inicia la prueba
    public void StartSkillCheck()
    {
        active = true;
    }

    void CheckSuccess()
    {
        active = false;

        float angleDifference = Quaternion.Angle(pointerTransform.rotation, safeZone.rotation);

        bool success = angleDifference <= hitboxTolerancia;

        if (success)
        {
            Debug.Log("SkillCheck: Éxito.");
        }
        else
        {
            Debug.Log("SkillCheck: Fallo.");
        }

        // Notificar resultado
        if (OnSkillCheckResult != null) OnSkillCheckResult.Invoke(success);
    }
}