using AnteNoctem.Core;

// ===== NAMESPACE =====
namespace AnteNoctem.Core
{
    
    /// Aca declaramos los delegates propios del juego.
    /// Un delegate es como una "referencia a una funcion": en vez de guardar un numero o un texto,
    /// guardas la direccion de un metodo. Despues podes "invocar" ese delegate y se ejecuta el metodo guardado.
    /// Los usamos en el GameManager para avisar a otros scripts cuando cambian cosas (puntaje, hora del dia, etc).
    
    // ===== DELEGATES =====
    // 'delegate' es la palabra clave para declarar uno. Definimos su firma (que parametros recibe).
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