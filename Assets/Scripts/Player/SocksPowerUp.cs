
using UnityEngine;

public class SocksPowerUp : PowerUp
{
    [Header("Ajustes de Medias")]
    [Tooltip("0.5 significa que el perro necesita estar a la mitad de distancia para olerte")]
    public float noiseFactor = 0.5f;

    protected override void ApplyEffect(PlayerController player)
    {
        player.noiseMultiplier = noiseFactor;
    }
}