//TPFinal - Pedro Valle

using UnityEngine;

// ===== NAMESPACE =====
namespace AnteNoctem.Interactions
{
    
    /// Una interface es como un "contrato" que dice que cosas tiene que poder hacer una clase.
    /// Cualquier objeto del juego con el que se pueda interactuar apretando E (tachos, items para robar, el chapucero)
    /// implementa esta interface. Asi el sistema de interaccion del jugador no necesita saber con qué cosa esta hablando,
    /// solo necesita saber que ese objeto cumple este contrato.
    
    // ===== INTERFACE =====
    // Una interface define que METODOS debe tener una clase, pero no como funcionan adentro.
    // Cada clase que la implementa decide su propia logica.
    public interface IInteractable
    {
        // El texto que aparece arriba del jugador, tipo "Apreta E para revolver la basura"
        string GetPromptText();

        // Si la interaccion esta disponible ahora mismo (por ejemplo, los tachos no se pueden usar dos veces seguidas)
        bool CanInteract();

        // Lo que pasa cuando el jugador aprieta E cerca de este objeto
        void Interact(PlayerController player);
    }
}