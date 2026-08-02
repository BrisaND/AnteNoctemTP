//TPFinal - Juan Cruz Villarreo

using UnityEngine;

public class ScentMarker : MonoBehaviour
{
    public float lifetime = 8f;
    public float spawnTime { get; private set; }

    void Start()
    {
        spawnTime = Time.time;
        Destroy(gameObject, lifetime);
    }

    public float GetStrength()
    {
        float age = Time.time - spawnTime;
        return Mathf.Clamp01(1f - (age / lifetime));
    }
}