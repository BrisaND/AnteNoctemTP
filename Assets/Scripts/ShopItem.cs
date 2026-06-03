using System;
using UnityEngine;

[Serializable]
public class ShopItem
{
    public string itemId; // "boots", "socks", "gloves"
    public string displayName; // "Botas Veloces"

    [TextArea(2, 4)]
    public string description; // descripcion narrativa que dice el chapucero

    [Header("Costo")]
    public int hiloCost;
    public int telaCost;
    public int cueroCost;

    [Header("Estado")]
    public bool isPurchased = false;
}