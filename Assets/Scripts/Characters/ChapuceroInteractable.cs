using UnityEngine;
using AnteNoctem.Interactions;

[RequireComponent(typeof(Collider))]
// ===== INTERFACE =====
// Tambien implementa la interface IInteractable: eso significa que cumple el "contrato"
// que define IInteractable y debe tener los metodos GetPromptText, CanInteract e Interact.
public class ChapuceroInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaccion")]
    public string playerTag = "Player";

    // Unity llama Reset() al agregar el componente: forzamos que el collider sea trigger
    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // Metodos del contrato IInteractable

    public string GetPromptText() => "Apretá E para hablar con el Chapucero";

    public bool CanInteract()
    {
        // Si el mapa de la base esta abierto, no se puede hablar con el chapucero
        if (BaseMapView.Instance != null && BaseMapView.Instance.IsOpen) return false;
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract()) return;

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