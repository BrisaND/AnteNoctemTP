using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Player : Human
{
    [Header("Estado del Jugador")]
    public bool isStealing = false;
    public int points = 0;

    public static Player Instance;

    private void Awake()
    {
        Instance = this;
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
    }
}