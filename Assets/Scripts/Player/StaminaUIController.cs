using UnityEngine;
using UnityEngine.UI;

public class StaminaUIController : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("La imagen con Image Type = Filled que representa la barra")]
    public Image fillImage;

    [Header("Colores (opcional)")]
    public Color normalColor = Color.white;
    public Color exhaustedColor = Color.red;
    [Tooltip("Cambia a color de agotado cuando esta bajo este porcentaje")]
    [Range(0f, 1f)] public float lowThreshold = 0.2f;

    private PlayerStamina stamina;
    private bool foundPlayer = false;

    void Update()
    {
        if (!foundPlayer)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            stamina = player.GetComponent<PlayerStamina>();
            foundPlayer = true;
        }

        if (stamina == null || fillImage == null) return;

        // Actualizamos el fill
        fillImage.fillAmount = stamina.StaminaPercent;

        // Cambio de color cuando esta agotado o muy bajo
        if (stamina.isExhausted || stamina.StaminaPercent < lowThreshold)
            fillImage.color = exhaustedColor;
        else
            fillImage.color = normalColor;
    }
}