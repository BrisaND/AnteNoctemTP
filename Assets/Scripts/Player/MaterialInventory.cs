//TPFinal - Pedro Valle

using UnityEngine;
using AnteNoctem.Core;
using System;
using System.Collections.Generic;

public class MaterialInventory : Singleton<MaterialInventory>
{
    public enum MaterialType { Hilo, Tela, Cuero }

    private Dictionary<MaterialType, int> inventory = new Dictionary<MaterialType, int>();

    // Func para determinar si un material dropea segun la probabilidad dada
    public Func<float, bool> ShouldDropByChance = (chance) => UnityEngine.Random.value <= chance;

    public Action<MaterialType, int> OnMaterialAdded;

    [Header("Probabilidades de drop")]
    [Tooltip("Probabilidad de que aparezca CUALQUIER material (0-1)")]
    [Range(0f, 1f)] public float dropChance = 0.7f;
    [Tooltip("Pesos relativos - mayor numero = mas comun")]
    public float hiloWeight = 60f;
    public float telaWeight = 30f;
    public float cueroWeight = 10f;

    // Claves para PlayerPrefs
    private const string KEY_PREFIX = "MaterialInventory";

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        // Cargamos los materiales guardados en disco
        LoadFromDisk();
    }

    void LoadFromDisk()
    {
        foreach (MaterialType mat in Enum.GetValues(typeof(MaterialType)))
        {
            string key = KEY_PREFIX + mat.ToString();
            int saved = PlayerPrefs.GetInt(key, 0);
            inventory[mat] = saved;
            Debug.Log($"[MaterialInventory] Cargado: {mat} = {saved}");
        }
    }

    void SaveToDisk(MaterialType type)
    {
        string key = KEY_PREFIX + type.ToString();
        PlayerPrefs.SetInt(key, inventory[type]);
        PlayerPrefs.Save();
    }

    public MaterialType? TryDropMaterial()
    {
        if (!ShouldDropByChance(dropChance)) return null;

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

        // Guardamos en disco cada vez que cambia
        SaveToDisk(type);

        OnMaterialAdded?.Invoke(type, inventory[type]);
    }

    public int GetCount(MaterialType type)
    {
        return inventory.ContainsKey(type) ? inventory[type] : 0;
    }

    public bool RemoveMaterials(MaterialType type, int amount)
    {
        if (!inventory.ContainsKey(type)) inventory[type] = 0;
        if (inventory[type] >= amount)
        {
            inventory[type] -= amount;

            // Guardamos en disco cada vez que cambia
            SaveToDisk(type);

            OnMaterialAdded?.Invoke(type, inventory[type]);
            Debug.Log($"[Canje] Se quitaron {amount} de {type}. Total restante: {inventory[type]}");
            return true;
        }
        Debug.LogWarning($"[Canje] No hay suficiente {type}. Requerido: {amount}, Tienes: {inventory[type]}");
        return false;
    }

    /// <summary>
    /// Resetea el inventario a 0 y borra del disco. Util para testear o al iniciar nueva partida.
    /// </summary>
    public void ResetInventory()
    {
        foreach (MaterialType mat in Enum.GetValues(typeof(MaterialType)))
        {
            inventory[mat] = 0;
            PlayerPrefs.DeleteKey(KEY_PREFIX + mat.ToString());
        }
        PlayerPrefs.Save();
        Debug.Log("[MaterialInventory] Inventario reseteado.");
    }
}