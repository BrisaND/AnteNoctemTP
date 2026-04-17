using UnityEngine;

public abstract class Human : MonoBehaviour
{
    [Header("Atributos Base")]
    public int health = 3;
    public bool isAlive = true;

    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        isAlive = false;
    }
}