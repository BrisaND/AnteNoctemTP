using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;





public class HubLevelSelectUI : MonoBehaviour
{
    [System.Serializable]
    public class HubZoneEntry
    {
        public string displayName;
        public string sceneName;
        public bool unlockedAtStart;
    }


    [Header("Zonas del Mapa")]
    public List<HubZoneEntry> zones = new List<HubZoneEntry>();

    [Header("Referencias de UI (Asignar en Inspector)")]
    [Tooltip("El objeto hijo del Panel donde se van a alinear los botones de los niveles.")]
    public RectTransform levelListContainer;

    [Tooltip("El prefab o GameObject del botón base que vamos a clonar.")]
    public Button buttonPrefab;

    [Header("Popup de Confirmación (Asignar en Inspector)")]
    public GameObject confirmationPanel;
    public TMP_Text confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private HubZoneEntry pendingZone;

    void Awake()
    {
        EnsureDefaultZones();

        // Enlazar eventos de los botones de confirmación
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);

        HideConfirmation();
    }

    void EnsureDefaultZones()
    {
        if (zones != null && zones.Count > 0) return;

        // Datos por defecto si te olvidás de rellenar la lista en el Inspector
        zones = new List<HubZoneEntry>
        {
            new HubZoneEntry { displayName = "Pueblo", sceneName = "LevelInit", unlockedAtStart = true },
            new HubZoneEntry { displayName = "Distrito Alto", sceneName = "", unlockedAtStart = false },
            new HubZoneEntry { displayName = "Zona de Infiltración", sceneName = "", unlockedAtStart = false },
        };
    }

    public bool IsZoneUnlocked(HubZoneEntry zone)
    {
        if (zone == null) return false;
        return zone.unlockedAtStart;
    }

    // Se ejecuta automáticamente al abrir la terminal interactiva del mapa
    public void OnMapOpened(GameObject mapUIPanel)
    {
        BuildLevelButtons();
        HideConfirmation();
    }

    public void OnMapClosed()
    {
        HideConfirmation();
    }

    void BuildLevelButtons()
    {
        ClearButtons();
        if (levelListContainer == null || buttonPrefab == null) return;

        foreach (var zone in zones)
        {
            bool unlocked = IsZoneUnlocked(zone);
            bool canTravel = unlocked && !string.IsNullOrEmpty(zone.sceneName);

            // Clonamos el botón de diseño que hizo la diseñadora
            Button btnInstance = Instantiate(buttonPrefab, levelListContainer);
            btnInstance.gameObject.name = "Btn_" + zone.displayName;
            Image img = btnInstance.GetComponent<Image>();
            Button btn = btnInstance;

            // Buscamos el texto del botón clonado para cambiarle el nombre
            TMP_Text btnText = btnInstance.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = canTravel ? zone.displayName : zone.displayName + " (Bloqueado)";
            }

            // Configuramos si el botón se puede cliquear o no
            btnInstance.interactable = canTravel;

            if (canTravel)
            {
                HubZoneEntry captured = zone;
                btnInstance.onClick.AddListener(() => OnZoneClicked(captured));
            }

            spawnedButtons.Add(btnInstance);
        }
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

        Time.timeScale = 1f; // Asegura que el juego no quede pausado en la otra escena
        SceneManager.LoadScene(pendingZone.sceneName);
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

    void ClearButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();
    }
}
