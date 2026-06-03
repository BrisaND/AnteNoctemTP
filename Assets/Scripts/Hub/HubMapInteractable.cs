using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HubMapInteractable : MonoBehaviour
{
    [Header("Interacción (igual que otros objetos)")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    [Header("Mapa")]
    public BaseMapView mapView;

    bool playerInRange;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange) return;

        if (mapView == null)
            mapView = BaseMapView.Instance;

        if (mapView == null || mapView.IsOpen) return;

        if (Input.GetKeyDown(interactKey))
            mapView.Open();
    }
}
