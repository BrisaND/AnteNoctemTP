using UnityEngine;
using System.Collections.Generic;
using System;

public class MaterialInventory : MonoBehaviour
{
    public static MaterialInventory Instance { get; private set; }

    public enum MaterialType { Hilo, Tela, Cuero }

    // Inventario en memoria
    private Dictionary<MaterialType, int> inventory = new Dictionary<MaterialType, int>();

    // Evento para que la UI se entere cuando cambia el inventario
    public Action<MaterialType, int> OnMaterialAdded;

    [Header("Probabilidades de drop")]
    [Tooltip("Probabilidad de que aparezca CUALQUIER material (0-1)")]
    [Range(0f, 1f)] public float dropChance = 0.7f;

    [Tooltip("Pesos relativos - mayor numero = mas comun")]
    public float hiloWeight = 60f;   // comun
    public float telaWeight = 30f;   // poco comun
    public float cueroWeight = 10f;  // muy raro

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Inicializamos el inventario en 0
        foreach (MaterialType mat in Enum.GetValues(typeof(MaterialType)))
        {
            inventory[mat] = 0;
        }
    }

    // Llamar despues de un robo exitoso. Devuelve el material que salio (o null si no salio nada).
    public MaterialType? TryDropMaterial()
    {
        // Primero chequeamos si DROPEA algo (no siempre dropea)
        if (UnityEngine.Random.value > dropChance) return null;

        // Sumamos los pesos
        float totalWeight = hiloWeight + telaWeight + cueroWeight;
        float roll = UnityEngine.Random.value * totalWeight;

        MaterialType selected;

        if (roll < hiloWeight)
            selected = MaterialType.Hilo;
        else if (roll < hiloWeight + telaWeight)
            selected = MaterialType.Tela;
        else
            selected = MaterialType.Cuero;

        AddMaterial(selected, 1);
        return selected;
    }

    public void AddMaterial(MaterialType type, int amount)
    {
        if (!inventory.ContainsKey(type)) inventory[type] = 0;
        inventory[type] += amount;
        OnMaterialAdded?.Invoke(type, inventory[type]);
    }

    public int GetCount(MaterialType type)
    {
        return inventory.ContainsKey(type) ? inventory[type] : 0;
    }
}