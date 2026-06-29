
using UnityEngine;

public class BootsPowerUp : PowerUp
{
    [Header("Ajustes de Botas")]
    [Tooltip("1.35 significa un 35% más de velocidad en todos tus estados de movimiento")]
    public float multiplicadorVelocidad = 1.35f;

    protected override void ApplyEffect(PlayerController player)
    {
        // Modifica el multiplicador central del PlayerController
        player.speedMultiplier = multiplicadorVelocidad;
    }
}