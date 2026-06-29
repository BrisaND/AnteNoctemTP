using System;
using UnityEngine;

[Serializable]
public class ShopItem
{
    // ID interno usado en el codigo para identificar el item ("boots", "socks", "gloves")
    public string itemId;
    // Nombre que ve el jugador en la UI
    public string displayName;

    // Descripcion que dice el chapucero cuando el jugador selecciona el item
    [TextArea(2, 4)]
    public string description;

    [Header("Costo")]
    public int hiloCost;
    public int telaCost;
    public int cueroCost;

    [Header("Estado")]
    public bool isPurchased = false;
}