
using UnityEngine;

public class GlovesPowerUp : PowerUp
{
    [Header("Ajustes de Guantes")]
    public float segundosExtra = 3.0f; // Te da 3 segundos más en el minijuego

    protected override void ApplyEffect(PlayerController player)
    {
        player.escapeTimeBonus += segundosExtra;
    }
}