using UnityEngine;

public class RobberySystem : MonoBehaviour
{
    public static RobberySystem Instance { get; private set; }

    [Header("Puntos por robo")]
    [Min(1)] public int minStealPoints = 5;
    [Min(1)] public int maxStealPoints = 15;

    [Header("Debug")]
    public bool debugLogs = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public int RollStealPoints()
    {
        int min = Mathf.Max(1, minStealPoints);
        int max = Mathf.Max(min, maxStealPoints);
        return Random.Range(min, max + 1);
    }

    public int AwardStealPoints(string sourceLabel = "Robo")
    {
        int points = RollStealPoints();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(points);
        }

        if (debugLogs)
        {
            Debug.Log(sourceLabel + ": +" + points + " puntos");
        }

        return points;
    }
}
