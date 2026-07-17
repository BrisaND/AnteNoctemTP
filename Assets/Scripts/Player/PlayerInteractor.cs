using UnityEngine;
using TMPro;
using AnteNoctem.Interactions;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Configuracion")]
    public float interactionRadius = 2.5f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactableLayers = ~0;

    [Header("UI")]
    [Tooltip("Texto donde aparece el prompt")]
    public TMP_Text promptText;

    private PlayerController playerCtrl;

    // ===== POLIMORFISMO via INTERFACE =====
    // Esta variable puede guardar CUALQUIER cosa que implemente IInteractable: un tacho, el chapucero, lo que sea.
    // El polimorfismo es eso: una misma variable puede tomar muchas formas distintas.
    // Asi este script no necesita saber con qué objeto especifico esta hablando, solo necesita saber que cumple el contrato.
    private IInteractable currentInteractable;

    void Awake()
    {
        playerCtrl = GetComponent<PlayerController>();
        if (promptText != null) promptText.text = "";
    }

    void Update()
    {
        // En cada frame buscamos cual es el objeto interactuable mas cercano,
        // mostramos su texto, y si el jugador aprieta E lo activamos
        FindNearestInteractable();
        UpdatePromptUI();

        if (Input.GetKeyDown(interactKey) && currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact(playerCtrl);
        }
    }

    void FindNearestInteractable()
    {
        // Hacemos una esfera invisible alrededor del jugador y buscamos colliders dentro
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayers);

        IInteractable nearest = null;
        float minDist = float.MaxValue;

        foreach (var col in hits)
        {
            Debug.Log("PlayerInteractor detecta collider: " + col.name);

            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                Debug.Log("  → NO tiene IInteractable");
                continue;
            }
            Debug.Log("  → SÍ tiene IInteractable: " + interactable.GetType().Name);

            if (!interactable.CanInteract()) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = interactable;
            }
        }

        currentInteractable = nearest;
    }

    void UpdatePromptUI()
    {
        if (promptText == null) return;
        if (currentInteractable != null)
            promptText.text = currentInteractable.GetPromptText();
        else
            promptText.text = "";
    }

    void OnDrawGizmosSelected()
    {
        // Dibuja una esfera azul en el editor para ver el rango de interaccion
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}