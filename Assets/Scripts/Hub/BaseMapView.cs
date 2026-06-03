using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la vista del mapa en el campamento (Base).
/// CONFIGURACIÓN EN UNITY (escena Base):
/// 1. Main Camera: agregar Cinemachine Brain (Add Component).
/// 2. Crear hijo vacío "MapCamera" con Cinemachine Camera, posicionarlo mirando el objeto mapa.
/// 3. Desactivar el GameObject MapCamera al inicio.
/// 4. Crear Canvas con panel y botón "Volver"; asignar referencias abajo.
/// </summary>
public class BaseMapView : MonoBehaviour
{
    public static BaseMapView Instance { get; private set; }

    [Header("Cámaras")]
    public ShoulderCamera playerCamera;
    public CinemachineCamera mapCamera;

    [Header("Jugador")]
    public PlayerController player;
    [Tooltip("Hijo del jugador con el modelo 3D (se oculta al abrir el mapa).")]
    public GameObject playerVisual;

    [Header("UI")]
    public GameObject mapUIPanel;
    public Button backButton;
    public HubLevelSelectUI levelSelectUI;

    const int MapCameraPriority = 20;

    public bool IsOpen { get; private set; }

    PlayerController cachedPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (levelSelectUI == null)
            levelSelectUI = GetComponent<HubLevelSelectUI>();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.GetComponent<ShoulderCamera>();

        if (mapCamera != null)
        {
            mapCamera.Priority.Value = MapCameraPriority;
            mapCamera.gameObject.SetActive(false);
        }

        if (mapUIPanel != null)
            mapUIPanel.SetActive(false);

        if (backButton != null)
            backButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (IsOpen) return;

        if (ChapuceroShop.Instance != null && ChapuceroShop.Instance.IsOpen)
            return;

        if (mapCamera == null)
        {
            Debug.LogWarning("BaseMapView: falta asignar la Cinemachine Camera del mapa.");
            return;
        }

        IsOpen = true;
        CachePlayer();

        if (cachedPlayer != null)
            cachedPlayer.enabled = false;

        if (playerVisual != null)
            playerVisual.SetActive(false);

        if (playerCamera != null)
            playerCamera.enabled = false;

        mapCamera.gameObject.SetActive(true);

        if (mapUIPanel != null)
            mapUIPanel.SetActive(true);

        if (levelSelectUI != null)
            levelSelectUI.OnMapOpened(mapUIPanel);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!IsOpen) return;

        if (levelSelectUI != null)
            levelSelectUI.OnMapClosed();

        IsOpen = false;

        if (mapCamera != null)
            mapCamera.gameObject.SetActive(false);

        if (playerCamera != null)
            playerCamera.enabled = true;

        if (cachedPlayer != null)
            cachedPlayer.enabled = true;

        if (playerVisual != null)
            playerVisual.SetActive(true);

        if (mapUIPanel != null)
            mapUIPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void CachePlayer()
    {
        if (player != null)
        {
            cachedPlayer = player;
            return;
        }

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            cachedPlayer = playerGo.GetComponent<PlayerController>();
    }
}
