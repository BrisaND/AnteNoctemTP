//TPFinal - Pedro Valle

using AnteNoctem.Core;

namespace AnteNoctem.Core
{
    
    /// Aca declaramos los delegates propios del juego.
    // Cualquier metodo con esa misma firma puede ser asignado a este delegate.
    public static class GameDelegates
    {
        // Delegate que avisa cuando cambia el estado del dia (Dia, Tarde, Noche)
        public delegate void DayStateChangedHandler(GameManager.DayState newState);

        // Delegate que avisa cuando cambia el puntaje (manda el puntaje nuevo y el objetivo)
        public delegate void ScoreChangedHandler(int newScore, int targetScore);

        // Delegate que avisa cuando se agarra un material (manda el tipo y la cantidad nueva)
        public delegate void MaterialCollectedHandler(MaterialInventory.MaterialType type, int newAmount);
    }
}