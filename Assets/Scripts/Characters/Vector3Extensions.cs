using UnityEngine;

// ===== NAMESPACE =====
namespace AnteNoctem.Core
{
    
    /// Los "metodos de extension" son una forma de agregarle funciones a clases que ya existen
    /// (como Vector3 de Unity) sin tener que modificar la clase original.
    /// Asi podemos hacer cosas como mi_vector.IsNearlyZero() en vez de IsNearlyZero(mi_vector).
    /// Es una sintaxis mas natural y limpia.
    
    // ===== EXTENSIONS =====
    // 'static' porque los metodos de extension siempre van en clases estaticas.
    // 'this Vector3' antes del parametro es la magia que convierte el metodo en una extension.
    public static class Vector3Extensions
    {
        // Distancia entre dos puntos ignorando la altura (Y)
        public static float HorizontalDistance(this Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Devuelve el vector aplastado al plano horizontal (sin componente Y)
        public static Vector3 Flattened(this Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        // True si el vector es casi cero (con tolerancia para errores de precision)
        public static bool IsNearlyZero(this Vector3 v, float tolerance = 0.01f)
        {
            return v.sqrMagnitude < tolerance * tolerance;
        }
    }
}