using System;

// ===== NAMESPACE =====
namespace AnteNoctem.Core
{
    
    /// Representa los materiales que cuesta craftear un item en la tienda del chapucero.
    /// Por ejemplo, las botas cuestan 3 hilo, 4 tela y 1 cuero, eso es un MaterialRequirement.
    
    [Serializable]
    // ===== STRUCT =====
    // Un struct es parecido a una class pero MAS LIVIANO.
    // Lo usamos para valores chiquitos y simples (como un costo, un punto en el espacio, etc).
    // La diferencia clave: cuando pasas un struct a una funcion, se COPIA el contenido (no la referencia).
    // Eso lo hace mas eficiente para datos pequeños.
    public struct MaterialRequirement
    {
        public int hilo;
        public int tela;
        public int cuero;

        // Constructor: arma un MaterialRequirement con los valores que le pasamos
        public MaterialRequirement(int hilo, int tela, int cuero)
        {
            this.hilo = hilo;
            this.tela = tela;
            this.cuero = cuero;
        }

        
        /// Chequea si el jugador tiene suficiente de cada material para pagar este costo.
        
        public bool IsAffordable(MaterialInventory inventory)
        {
            if (inventory == null) return false;
            return inventory.GetCount(MaterialInventory.MaterialType.Hilo) >= hilo
                && inventory.GetCount(MaterialInventory.MaterialType.Tela) >= tela
                && inventory.GetCount(MaterialInventory.MaterialType.Cuero) >= cuero;
        }

        // Devuelve el costo en texto, para mostrar en la UI de la tienda
        public override string ToString()
        {
            return $"Hilo: {hilo} | Tela: {tela} | Cuero: {cuero}";
        }
    }
}