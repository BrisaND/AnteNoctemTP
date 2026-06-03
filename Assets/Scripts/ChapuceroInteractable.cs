using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChapuceroInteractable : MonoBehaviour
{
    [Header("Interaccion")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("UI prompt (opcional)")]
    public GameObject interactionPrompt; // texto "Presiona E para hablar"

    private bool playerInRange = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;

        if (BaseMapView.Instance != null && BaseMapView.Instance.IsOpen)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            if (ChapuceroShop.Instance != null)
            {
                ChapuceroShop.Instance.OpenShop();
            }
            else
            {
                Debug.LogWarning("ChapuceroInteractable: no se encontro ChapuceroShop.Instance");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }
}