using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ChapuceroShop : MonoBehaviour
{
    public static ChapuceroShop Instance { get; private set; }

    public enum ShopState { Closed, Greeting, ShowingItem, Confirmation }

    [Header("UI Principal")]
    public GameObject shopCanvas;

    [Header("Cuadro Next (saludo)")]
    public GameObject dialogueBoxNext;
    public TMP_Text dialogueTextNext;
    public Button nextButton;

    [Header("Cuadro Buy (compra)")]
    public GameObject dialogueBoxBuy;
    public TMP_Text dialogueTextBuy;
    public Button rejectButton;
    public Button buyButton;
    public TMP_Text costText;

    [Header("Selector de items")]
    public Transform itemSelectorContainer;
    public Button itemButtonPrefab;

    [Header("Items disponibles")]
    public List<ShopItem> items = new List<ShopItem>();

    [Header("Textos del chapucero")]
    [TextArea(2, 4)] public string greetingText = "Buenas, pibe. Veni, mira lo que tengo. Si tenes los materiales, te lo armo.";
    [TextArea(2, 4)] public string itemListText = "Esto es lo que puedo armarte. Elegi.";
    [TextArea(2, 4)] public string notEnoughMaterialsText = "Mmm, te falta algo. Volve cuando tengas todo.";
    [TextArea(2, 4)] public string purchaseSuccessText = "Listo, ahi lo tenes. Que te sirva.";
    [TextArea(2, 4)] public string alreadyPurchasedText = "Eso ya te lo arme antes, no necesitas otro.";

    private ShopState currentState = ShopState.Closed;
    private ShopItem currentlyViewedItem;
    private List<Button> spawnedItemButtons = new List<Button>();

    public bool IsOpen => currentState != ShopState.Closed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (shopCanvas != null) shopCanvas.SetActive(false);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (rejectButton != null) rejectButton.onClick.AddListener(OnRejectClicked);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
    }

    void Update()
    {
        if (currentState != ShopState.Closed && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }
    }

    public void OpenShop()
    {
        if (currentState != ShopState.Closed) return;
        shopCanvas.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowGreeting();
    }

    public void CloseShop()
    {
        shopCanvas.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentState = ShopState.Closed;
        currentlyViewedItem = null;
    }

    // ===== ESTADOS =====
    void ShowGreeting()
    {
        currentState = ShopState.Greeting;
        if (dialogueTextNext != null) dialogueTextNext.text = greetingText;
        SetActivePanel(showNext: true, showBuy: false, showSelector: false);
    }

    void ShowItemList()
    {
        currentState = ShopState.ShowingItem;
        if (dialogueTextNext != null) dialogueTextNext.text = itemListText;
        SetActivePanel(showNext: false, showBuy: false, showSelector: true);
        BuildItemButtons();
    }

    void ShowItem(ShopItem item)
    {
        currentState = ShopState.Confirmation;
        currentlyViewedItem = item;

        // 1. Buscamos al Player en la escena de la base
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // 2. Comprobamos si el jugador YA tiene el item equipado/activado
        bool yaLoTiene = false;
        if (player != null)
        {
            switch (item.itemId)
            {
                case "boots":
                    var boots = player.GetComponentInChildren<BootsPowerUp>(true);
                    if (boots != null && boots.enabled) yaLoTiene = true;
                    break;
                case "socks":
                    var socks = player.GetComponentInChildren<SocksPowerUp>(true);
                    if (socks != null && socks.enabled) yaLoTiene = true;
                    break;
                case "gloves":
                    var gloves = player.GetComponentInChildren<GlovesPowerUp>(true);
                    if (gloves != null && gloves.enabled) yaLoTiene = true;
                    break;
            }
        }

        // 3. Mostramos el texto adecuado seg�n el estado real del componente
        string txt;
        if (yaLoTiene) txt = alreadyPurchasedText;
        else txt = item.displayName + ": " + item.description;

        if (dialogueTextBuy != null) dialogueTextBuy.text = txt;

        if (costText != null)
            costText.text = "Hilo: " + item.hiloCost + "   Tela: " + item.telaCost + "   Cuero: " + item.cueroCost;

        SetActivePanel(showNext: false, showBuy: true, showSelector: false);

        // El bot�n de comprar solo ser� interactuable si el jugador NO lo tiene equipado
        if (buyButton != null) buyButton.interactable = !yaLoTiene;
    }

    void SetActivePanel(bool showNext, bool showBuy, bool showSelector)
    {
        if (dialogueBoxNext != null) dialogueBoxNext.SetActive(showNext);
        if (dialogueBoxBuy != null) dialogueBoxBuy.SetActive(showBuy);
        if (itemSelectorContainer != null) itemSelectorContainer.gameObject.SetActive(showSelector);
    }

    // ===== BOTONES =====
    void OnNextClicked() { ShowItemList(); }
    void OnRejectClicked() { ShowItemList(); }

    void OnBuyClicked()
    {
        if (currentlyViewedItem == null) return;

        // 1. Validamos si tiene los materiales necesarios
        if (!PlayerHasEnoughMaterials(currentlyViewedItem))
        {
            if (dialogueTextBuy != null) dialogueTextBuy.text = notEnoughMaterialsText;
            return;
        }

        var inv = MaterialInventory.Instance;
        if (inv != null)
        {
            // 2. MODIFICACI�N: Usamos el m�todo seguro RemoveMaterials que dispara los eventos de UI correspondientes
            inv.RemoveMaterials(MaterialInventory.MaterialType.Hilo, currentlyViewedItem.hiloCost);
            inv.RemoveMaterials(MaterialInventory.MaterialType.Tela, currentlyViewedItem.telaCost);
            inv.RemoveMaterials(MaterialInventory.MaterialType.Cuero, currentlyViewedItem.cueroCost);
        }

        // 3. Aplicamos la l�gica de equipamiento original
        EquipItem(currentlyViewedItem);
        currentlyViewedItem.isPurchased = true;

        if (dialogueTextBuy != null) dialogueTextBuy.text = purchaseSuccessText;
        if (buyButton != null) buyButton.interactable = false;
    }

    // ===== HELPERS =====
    bool PlayerHasEnoughMaterials(ShopItem item)
    {
        var inv = MaterialInventory.Instance;
        if (inv == null) return false;
        return inv.GetCount(MaterialInventory.MaterialType.Hilo) >= item.hiloCost
            && inv.GetCount(MaterialInventory.MaterialType.Tela) >= item.telaCost
            && inv.GetCount(MaterialInventory.MaterialType.Cuero) >= item.cueroCost;
    }

    void BuildItemButtons()
    {
        foreach (var b in spawnedItemButtons)
            if (b != null) Destroy(b.gameObject);
        spawnedItemButtons.Clear();

        if (itemButtonPrefab == null || itemSelectorContainer == null) return;

        foreach (var item in items)
        {
            Button btn = Instantiate(itemButtonPrefab, itemSelectorContainer);
            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = item.displayName;
            ShopItem localItem = item;
            btn.onClick.AddListener(() => ShowItem(localItem));
            spawnedItemButtons.Add(btn);
        }
    }

    void EquipItem(ShopItem item)
    {
        Debug.Log("Item equipado: " + item.itemId);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        switch (item.itemId)
        {
            case "boots":
                var boots = player.GetComponentInChildren<BootsPowerUp>(true);
                if (boots != null) boots.enabled = true;
                break;
            case "socks":
                var socks = player.GetComponentInChildren<SocksPowerUp>(true);
                if (socks != null) socks.enabled = true;
                break;
            case "gloves":
                var gloves = player.GetComponentInChildren<GlovesPowerUp>(true);
                if (gloves != null) gloves.enabled = true;
                break;
        }
    }
}