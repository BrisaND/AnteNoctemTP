using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BaseMapView : MonoBehaviour
{
    public static BaseMapView Instance { get; private set; }

    [Header("Cámaras")]
    public ShoulderCamera playerCamera; 
    public CinemachineCamera mapCamera;

    [Header("Jugador")]
    public PlayerController player;
    public GameObject playerVisual;

    [Header("UI")]
    public GameObject mapUIPanel;
    public Button backButton;
    public HubLevelSelectUI levelSelectUI;

    public bool IsOpen { get; private set; }
    PlayerController cachedPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.GetComponent<ShoulderCamera>();

        if (mapCamera != null)
            mapCamera.gameObject.SetActive(false); 

        if (mapUIPanel != null) mapUIPanel.SetActive(false);
        if (backButton != null) backButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (IsOpen) return;
        if (mapCamera == null) return;

        IsOpen = true;
        CachePlayer();

        if (cachedPlayer != null) cachedPlayer.enabled = false;

       
        StartCoroutine(ExecuteOpenTransition());
    }

    IEnumerator ExecuteOpenTransition()
    {
        
        mapCamera.gameObject.SetActive(true);

        
        yield return new WaitForEndOfFrame();

       
        if (playerCamera != null)
            playerCamera.enabled = false;

        if (playerVisual != null) playerVisual.SetActive(false);
        if (mapUIPanel != null) mapUIPanel.SetActive(true);
        if (levelSelectUI != null) levelSelectUI.OnMapOpened(mapUIPanel);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (levelSelectUI != null) levelSelectUI.OnMapClosed();
 
        StartCoroutine(ExecuteCloseTransition());
    }

    IEnumerator ExecuteCloseTransition()
    {
        
        if (playerCamera != null) playerCamera.enabled = true;
        if (playerVisual != null) playerVisual.SetActive(true);

       
        if (mapCamera != null)
            mapCamera.gameObject.SetActive(false);

        if (cachedPlayer != null) cachedPlayer.enabled = true;
        if (mapUIPanel != null) mapUIPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        yield return null;
    }

    void CachePlayer()
    {
        if (player != null) { cachedPlayer = player; return; }
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) cachedPlayer = playerGo.GetComponent<PlayerController>();
    }
}