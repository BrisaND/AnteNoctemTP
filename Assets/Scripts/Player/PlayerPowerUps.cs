using UnityEngine;

/// <summary>
/// Centraliza los power-ups del jugador. Reemplaza a los scripts sueltos
/// (BootsPowerUp, SocksPowerUp, GlovesPowerUp) que eran para el sistema viejo del suelo.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerPowerUps : MonoBehaviour
{
    [Header("Estado (se guarda en disco)")]
    public bool hasBoots = false;
    public bool hasSocks = false;
    public bool hasGloves = false;

    [Header("Efectos")]
    [Tooltip("1.35 = 35% mas rapido")]
    public float bootsSpeedMultiplier = 1.35f;
    [Tooltip("0.5 = mitad de ruido/deteccion")]
    public float socksNoiseMultiplier = 0.5f;
    [Tooltip("Segundos extra en minijuegos de escape")]
    public float glovesEscapeBonus = 3f;

    // Claves de disco
    private const string KEY_BOOTS = "PowerUp_Boots";
    private const string KEY_SOCKS = "PowerUp_Socks";
    private const string KEY_GLOVES = "PowerUp_Gloves";

    private PlayerController playerCtrl;

    void Awake()
    {
        playerCtrl = GetComponent<PlayerController>();
    }

    void Start()
    {
        // Cargamos el estado desde disco
        LoadFromDisk();
        // Aplicamos los efectos que correspondan
        ApplyAllEffects();
    }

    public void EquipBoots()
{
    Debug.Log("[PowerUps] EquipBoots llamado!");
    hasBoots = true;
    SaveToDisk();
    ApplyAllEffects();
}

    public void EquipSocks()
    {
        hasSocks = true;
        SaveToDisk();
        ApplyAllEffects();
    }

    public void EquipGloves()
    {
        hasGloves = true;
        SaveToDisk();
        ApplyAllEffects();
    }

    void ApplyAllEffects()
    {
        if (playerCtrl == null) return;

        playerCtrl.speedMultiplier = hasBoots ? bootsSpeedMultiplier : 1f;
        playerCtrl.noiseMultiplier = hasSocks ? socksNoiseMultiplier : 1f;
        playerCtrl.escapeTimeBonus = hasGloves ? glovesEscapeBonus : 0f;

        Debug.Log($"[PowerUps] Boots={hasBoots} | Socks={hasSocks} | Gloves={hasGloves}");
    }

    void LoadFromDisk()
    {
        hasBoots = PlayerPrefs.GetInt(KEY_BOOTS, 0) == 1;
        hasSocks = PlayerPrefs.GetInt(KEY_SOCKS, 0) == 1;
        hasGloves = PlayerPrefs.GetInt(KEY_GLOVES, 0) == 1;
        Debug.Log($"[PowerUps] Cargado desde disco: Boots={hasBoots} Socks={hasSocks} Gloves={hasGloves}");
    }

    void SaveToDisk()
    {
        PlayerPrefs.SetInt(KEY_BOOTS, hasBoots ? 1 : 0);
        PlayerPrefs.SetInt(KEY_SOCKS, hasSocks ? 1 : 0);
        PlayerPrefs.SetInt(KEY_GLOVES, hasGloves ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[PowerUps] Guardado en disco: Boots={hasBoots} Socks={hasSocks} Gloves={hasGloves}");
    }

    public void ResetPowerUps()
    {
        hasBoots = false;
        hasSocks = false;
        hasGloves = false;
        PlayerPrefs.DeleteKey(KEY_BOOTS);
        PlayerPrefs.DeleteKey(KEY_SOCKS);
        PlayerPrefs.DeleteKey(KEY_GLOVES);
        PlayerPrefs.Save();
        ApplyAllEffects();
    }
}