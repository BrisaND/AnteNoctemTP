using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HubLevelSelectUI : MonoBehaviour
{
    [System.Serializable]
    public class HubZoneEntry
    {
        public string displayName;
        public string sceneName;
        public bool unlockedAtStart;
        [Tooltip("Arrastrá acá el botón físico que pusiste en el mapa para este nivel.")]
        public Button levelButton; 
    }

    [Header("Zonas del Mapa")]
    public List<HubZoneEntry> zones = new List<HubZoneEntry>();

    [Header("Popup de Confirmación (Asignar en Inspector)")]
    public GameObject confirmationPanel;
    public TMP_Text confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private HubZoneEntry pendingZone;

    void Awake()
    {
        // Enlazar eventos de los botones de confirmación
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);

        HideConfirmation();
    }

    void Start()
    {
        // Configuramos los botones una sola vez al iniciar el juego
        SetupMapButtons();
    }

    // Se ejecuta automáticamente al abrir la terminal interactiva del mapa
    public void OnMapOpened()
    {
        SetupMapButtons();
        HideConfirmation();
    }

    public void OnMapClosed()
    {
        HideConfirmation();
    }

    void SetupMapButtons()
    {
        foreach (var zone in zones)
        {
            if (zone.levelButton == null) continue;

            bool unlocked = IsZoneUnlocked(zone);
            bool canTravel = unlocked && !string.IsNullOrEmpty(zone.sceneName);

            // Configuramos la interactividad del botón de Unity
            zone.levelButton.interactable = canTravel;

            // Buscamos el texto para actualizarlo si es necesario
            TMP_Text btnText = zone.levelButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = canTravel ? zone.displayName : zone.displayName + " (Bloqueado)";
            }

            // Limpiamos listeners viejos para que no se acumulen y asignamos el evento
            zone.levelButton.onClick.RemoveAllListeners();
            if (canTravel)
            {
                HubZoneEntry captured = zone;
                zone.levelButton.onClick.AddListener(() => OnZoneClicked(captured));
            }

            // Avisamos al componente de efectos si está desbloqueado o no
            LevelButtonEffects effects = zone.levelButton.GetComponent<LevelButtonEffects>();
            if (effects != null)
            {
                effects.SetUnlocked(unlocked);
            }
        }
    }

    public bool IsZoneUnlocked(HubZoneEntry zone)
    {
        if (zone == null) return false;
        return zone.unlockedAtStart;
    }

    void OnZoneClicked(HubZoneEntry zone)
    {
        if (!IsZoneUnlocked(zone) || string.IsNullOrEmpty(zone.sceneName)) return;

        pendingZone = zone;
        if (confirmationText != null)
            confirmationText.text = "¿Viajar a " + zone.displayName + "?";

        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    void OnConfirmYes()
    {
        if (pendingZone == null || string.IsNullOrEmpty(pendingZone.sceneName)) return;

        Time.timeScale = 1f;
        LoadingScreen.LoadingScreenAsync(pendingZone.sceneName);
    }

    void OnConfirmNo()
    {
        HideConfirmation();
    }

    void HideConfirmation()
    {
        pendingZone = null;
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}