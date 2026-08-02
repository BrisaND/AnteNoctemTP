//TPFinal - Pedro Valle

using UnityEngine;

// Ayudan a evitar que dos clases con el mismo nombre se choquen.
namespace AnteNoctem.Core
{
    /// En vez de copiar el mismo codigo en cada manager, se hace esta clase base que se encarga de todo.
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
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