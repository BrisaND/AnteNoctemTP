//TPFinal - Malena Misson

using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ScentTrailEmitter : MonoBehaviour
{
    [Header("Emision")]
    [Tooltip("Distancia entre marcas mientras camina (metros)")]
    public float walkSpacing = 1.5f;
    [Tooltip("Distancia entre marcas mientras corre (mas chico = mas marcas)")]
    public float runSpacing = 0.8f;
    [Tooltip("Distancia entre marcas agachado (mas grande = menos rastro)")]
    public float crouchSpacing = 3f;

    [Header("Marca")]
    public float markerLifetime = 8f;

    [Header("Debug")]
    public bool showMarkers = true;

    private PlayerController pc;
    private Vector3 lastMarkPos;

    void Start()
    {
        pc = GetComponent<PlayerController>();
        lastMarkPos = transform.position;
    }

    void Update()
    {
        if (pc.currentState == PlayerController.MoveState.Idle) return;

        float spacing = GetSpacing();
        float dist = Vector3.Distance(transform.position, lastMarkPos);

        if (dist >= spacing)
        {
            DropMarker();
            lastMarkPos = transform.position;
        }
    }

    float GetSpacing()
    {
        switch (pc.currentState)
        {
            case PlayerController.MoveState.Running: return runSpacing;
            case PlayerController.MoveState.Crouching: return crouchSpacing;
            case PlayerController.MoveState.Walking: return walkSpacing;
            default: return walkSpacing;
        }
    }

    void DropMarker()
    {
        GameObject marker = new GameObject("ScentMarker");
        marker.transform.position = transform.position;
        var sm = marker.AddComponent<ScentMarker>();
        sm.lifetime = markerLifetime;

        if (showMarkers)
        {
            // Esfera visual chiquita para debug (solo se ve en editor)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.position = transform.position + Vector3.up * 0.1f;
            visual.transform.localScale = Vector3.one * 0.2f;
            visual.transform.SetParent(marker.transform);
            Destroy(visual.GetComponent<Collider>());
            var rend = visual.GetComponent<MeshRenderer>();
            rend.material.color = new Color(1f, 0.8f, 0f, 0.5f);
        }
    }
}