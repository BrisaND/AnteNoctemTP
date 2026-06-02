using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    protected bool playerInRange = false;
    protected PlayerController playerCtrl;

    [Header("Configuración Visual")]
    [Tooltip("Texto para mostrar en la UI si tenés un sistema de carteles")]
    public string powerUpName = "PowerUp";

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerCtrl = other.GetComponent<PlayerController>();
            Debug.Log($"Presioná 'E' para agarrar: {powerUpName}");
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerCtrl = null;
        }
    }

    protected virtual void Update()
    {
        if (playerInRange && playerCtrl != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Interaction();
            }
        }
    }

    private void Interaction()
    {
        // Aplica el efecto específico de cada objeto
        ApplyEffect(playerCtrl);

        Debug.Log($"¡Power-up {powerUpName} activado!");

        // El objeto se destruye del suelo
        Destroy(gameObject);
    }

    // metodo abstracto
    protected abstract void ApplyEffect(PlayerController player);
}