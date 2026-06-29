using UnityEngine;

// ===== NAMESPACE =====
// Los namespaces son como "carpetas" para organizar el codigo.
// Ayudan a evitar que dos clases con el mismo nombre se choquen.
namespace AnteNoctem.Core
{
    
    /// Esta clase nos sirve para los "managers" del juego (GameManager, PauseManager, etc).
    /// Todos los managers necesitan ser unicos: que haya solo uno y se pueda acceder facilmente desde cualquier lado.
    /// En vez de copiar el mismo codigo en cada manager, hicimos esta clase base que se encarga de todo.
    
    // ===== GENERICS + CLASE ABSTRACTA + HERENCIA =====
    // GENERICS: el <T> permite reutilizar la clase para distintos tipos. Cada manager pone su propio tipo.
    // Por ejemplo, GameManager hereda de Singleton<GameManager> y PauseManager de Singleton<PauseManager>.
    // CLASE ABSTRACTA: la palabra 'abstract' significa que no se puede usar esta clase directamente,
    // solo heredando de ella. Asi forzamos que se use siempre con un tipo especifico.
    // HERENCIA: hereda de MonoBehaviour para poder funcionar como un componente de Unity.
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        // ===== GETTER/SETTER =====
        // 'get' es publico (cualquiera puede leer Instance), pero 'set' es privado (solo esta clase lo modifica).
        // Asi protegemos la variable de que cualquier otro script la cambie por error.
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            // Si ya existe otra instancia distinta de esta, destruimos esta para que quede solo una.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            // Si nos destruyen, limpiamos la referencia para que no quede colgada
            if (Instance == this) Instance = null;
        }
    }
}