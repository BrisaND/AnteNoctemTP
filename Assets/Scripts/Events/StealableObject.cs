using UnityEngine;
using AnteNoctem.Interactions;

[RequireComponent(typeof(Collider))]
public class StealableObject : MonoBehaviour, IInteractable
{
    [Header("Interaccion")]
    public string playerTag = "Player";
    public bool oneTimeSteal = true;
    public float cooldown = 2f;

    [Header("Puntos (si no hay RobberySystem)")]
    [Min(1)] public int fallbackMinPoints = 5;
    [Min(1)] public int fallbackMaxPoints = 15;

    [Header("Debug")]
    public bool debugLogs = false;

    private bool alreadyStolen = false;
    private float nextStealTime = 0f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // IInteractable
    public string GetPromptText() => "Apretá E para robar";

    public bool CanInteract()
    {
        if (oneTimeSteal && alreadyStolen) return false;
        if (Time.time < nextStealTime) return false;
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract()) return;

        int points;
        if (RobberySystem.Instance != null)
        {
            points = RobberySystem.Instance.AwardStealPoints("Robo en objeto");
        }
        else if (GameManager.Instance != null)
        {
            int min = Mathf.Max(1, fallbackMinPoints);
            int max = Mathf.Max(min, fallbackMaxPoints);
            points = Random.Range(min, max + 1);
            GameManager.Instance.AddScore(points);
        }
        else
        {
            return;
        }

        if (debugLogs) Debug.Log(name + ": robo exitoso (+" + points + ")");
        alreadyStolen = true;
        nextStealTime = Time.time + Mathf.Max(0f, cooldown);
    }
}