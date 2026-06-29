// Malena Farias

using UnityEngine;

public class SoundFloor : MonoBehaviour, ISoundFloor
{
    public AudioClip sound;
    public AudioClip GetClip()
    {
        return sound;
    }
}
