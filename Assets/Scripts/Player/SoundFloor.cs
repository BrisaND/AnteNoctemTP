//TPFinal - Malena Misson

using UnityEngine;

public class SoundFloor : MonoBehaviour, ISoundFloor
{
    [Header("Configuración de Clips")]
    public AudioClip walkSound;
    public AudioClip runSound;

    public AudioClip GetClip(bool isRunning)
    {
        // Si está corriendo, devuelve el sonido pesado; de lo contrario, el de caminata
        if (isRunning)
        {
            return runSound != null ? runSound : walkSound;
        }
        return walkSound;
    }
}
