using UnityEngine;

public class RobberySystem : MonoBehaviour
{
        public static RobberySystem Instance { get; private set; }

    public enum DifficultyLevel { Easy, Medium, Hard }

    [System.Serializable]
    public struct DifficultyReward
    {
        [Min(1)] public int minPoints;
        [Min(1)] public int maxPoints;
    }

    [Header("Puntos por robo (Base)")]
    [Min(1)] public int minStealPoints = 5;
    [Min(1)] public int maxStealPoints = 15;

    [Header("Configuración por Dificultad (Ciudadanos)")]
    public DifficultyReward easyConfig = new DifficultyReward { minPoints = 1, maxPoints = 4 };
    public DifficultyReward mediumConfig = new DifficultyReward { minPoints = 5, maxPoints = 9 };
    public DifficultyReward hardConfig = new DifficultyReward { minPoints = 10, maxPoints = 15 };

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

    public int RollStealPoints(DifficultyLevel difficulty)
    {
        DifficultyReward config = difficulty switch
        {
            DifficultyLevel.Easy => easyConfig,
            DifficultyLevel.Medium => mediumConfig,
            DifficultyLevel.Hard => hardConfig,
            _ => easyConfig
        };
        return Random.Range(config.minPoints, config.maxPoints + 1);
    }

    public int AwardStealPoints(string sourceLabel = "Robo")
    {
        int points = RollStealPoints();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(points);
        }

        // Intentar dropear un material
        if (MaterialInventory.Instance != null)
        {
            var dropped = MaterialInventory.Instance.TryDropMaterial();
            if (debugLogs)
            {
                if (dropped.HasValue)
                    Debug.Log(sourceLabel + ": +" + points + " puntos + material: " + dropped.Value);
                else
                    Debug.Log(sourceLabel + ": +" + points + " puntos (sin material)");
            }
        }
        else if (debugLogs)
        {
            Debug.Log(sourceLabel + ": +" + points + " puntos");
        }

        return points;
    }

    public int AwardStealPoints(DifficultyLevel difficulty, string sourceLabel = "Robo")
    {
        int points = RollStealPoints(difficulty);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(points);
        }

        // Intentar dropear un material
        if (MaterialInventory.Instance != null)
        {
            var dropped = MaterialInventory.Instance.TryDropMaterial();
            if (debugLogs)
            {
                if (dropped.HasValue)
                    Debug.Log(sourceLabel + ": +" + points + " puntos + material: " + dropped.Value);
                else
                    Debug.Log(sourceLabel + ": +" + points + " puntos (sin material)");
            }
        }
        else if (debugLogs)
        {
            Debug.Log(sourceLabel + ": +" + points + " puntos");
        }

        return points;
    }
}
