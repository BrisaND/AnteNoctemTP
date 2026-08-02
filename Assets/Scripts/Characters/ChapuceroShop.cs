//TPFinal - Pedro Valle

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using AnteNoctem.Core;

// El <ChapuceroShop> es el parametro generico: le dice al Singleton de que tipo es esta instancia.
public class ChapuceroShop : Singleton<ChapuceroShop>
{
    // Los estados posibles de la tienda. Asi sabemos en que momento del flujo esta.
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

    // Variables privadas: solo este script puede modificarlas. Asi protegemos el estado interno.
    private ShopState currentState = ShopState.Closed;
    private ShopItem currentlyViewedItem;
    private List<Button> spawnedItemButtons = new List<Button>();

    // Otros scripts pueden leer si la tienda esta abierta, pero no pueden cambiarlo desde afuera
    public bool IsOpen => currentState != ShopState.Closed;

    // Bandera para avisar al PauseManager que este frame la tienda uso el Esc
    public bool JustClosedThisFrame { get; private set; } = false;

    void Start()
    {
        if (shopCanvas != null) shopCanvas.SetActive(false);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (rejectButton != null) rejectButton.onClick.AddListener(OnRejectClicked);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
    }

    void Update()
    {
        // Reseteamos la bandera al comienzo del frame
        JustClosedThisFrame = false;

        if (currentState != ShopState.Closed && Input.GetKeyDown(KeyCode.Q))
        {
            CloseShop();
            JustClosedThisFrame = true;
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
        Debug.Log("[Shop] CloseShop llamado. Lockeando cursor.");
        shopCanvas.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentState = ShopState.Closed;
        currentlyViewedItem = null;

        // Forzar el lockeo un frame despues (por si algo lo destildea)
        StartCoroutine(ForceLockNextFrame());
    }

    private System.Collections.IEnumerator ForceLockNextFrame()
    {
        yield return null;
        yield return null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[Shop] Cursor forzado a Locked despues de 2 frames.");
    }

    // Cada metodo Show* maneja un estado distinto de la tienda

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

        // Buscamos al Player y vemos si ya tiene equipado este item
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        bool yaLoTiene = false;
        if (player != null)
        {
            var powerUps = player.GetComponent<PlayerPowerUps>();
            if (powerUps != null)
            {
                switch (item.itemId)
                {
                    case "boots": yaLoTiene = powerUps.hasBoots; break;
                    case "socks": yaLoTiene = powerUps.hasSocks; break;
                    case "gloves": yaLoTiene = powerUps.hasGloves; break;
                }
            }
        }

        // Mostramos el texto adecuado segun si ya lo tiene o no
        string txt;
        if (yaLoTiene) txt = alreadyPurchasedText;
        else txt = item.displayName + ": " + item.description;

        if (dialogueTextBuy != null) dialogueTextBuy.text = txt;

        // Creamos un MaterialRequirement con el costo del item y usamos su ToString para mostrarlo
        if (costText != null)
        {
            var cost = new MaterialRequirement(item.hiloCost, item.telaCost, item.cueroCost);
            costText.text = cost.ToString();
        }

        SetActivePanel(showNext: false, showBuy: true, showSelector: false);

        // El boton solo es interactuable si el jugador NO lo tiene equipado
        if (buyButton != null) buyButton.interactable = !yaLoTiene;
    }

    void SetActivePanel(bool showNext, bool showBuy, bool showSelector)
    {
        if (dialogueBoxNext != null) dialogueBoxNext.SetActive(showNext);
        if (dialogueBoxBuy != null) dialogueBoxBuy.SetActive(showBuy);
        if (itemSelectorContainer != null) itemSelectorContainer.gameObject.SetActive(showSelector);
    }

    // Botones del flujo de la tienda
    void OnNextClicked() { ShowItemList(); }
    void OnRejectClicked() { ShowItemList(); }

    void OnBuyClicked()
    {
        if (currentlyViewedItem == null) return;

        // Armamos el costo del item como un MaterialRequirement y le preguntamos si el jugador puede pagarlo
        var requirement = new MaterialRequirement(
            currentlyViewedItem.hiloCost,
            currentlyViewedItem.telaCost,
            currentlyViewedItem.cueroCost
        );

        if (!requirement.IsAffordable(MaterialInventory.Instance))
        {
            if (dialogueTextBuy != null) dialogueTextBuy.text = notEnoughMaterialsText;
            return;
        }

        // Descontamos los materiales
        var inv = MaterialInventory.Instance;
        if (inv != null)
        {
            inv.RemoveMaterials(MaterialInventory.MaterialType.Hilo, currentlyViewedItem.hiloCost);
            inv.RemoveMaterials(MaterialInventory.MaterialType.Tela, currentlyViewedItem.telaCost);
            inv.RemoveMaterials(MaterialInventory.MaterialType.Cuero, currentlyViewedItem.cueroCost);
        }

        // Equipamos el item al jugador
        EquipItem(currentlyViewedItem);
        currentlyViewedItem.isPurchased = true;

        if (dialogueTextBuy != null) dialogueTextBuy.text = purchaseSuccessText;
        if (buyButton != null) buyButton.interactable = false;
    }

    void BuildItemButtons()
    {
        // Limpiamos los botones del listado anterior (si quedaron)
        foreach (var b in spawnedItemButtons)
            if (b != null) Destroy(b.gameObject);
        spawnedItemButtons.Clear();

        if (itemButtonPrefab == null || itemSelectorContainer == null) return;

        // Por cada item disponible, instanciamos un boton clickeable
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

    // Activa el componente del power-up correspondiente en el Player
    void EquipItem(ShopItem item)
    {
        Debug.Log("Item equipado: " + item.itemId);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var powerUps = player.GetComponent<PlayerPowerUps>();
        if (powerUps == null)
        {
            Debug.LogWarning("ChapuceroShop: el player no tiene PlayerPowerUps");
            return;
        }

        switch (item.itemId)
        {
            case "boots": powerUps.EquipBoots(); break;
            case "socks": powerUps.EquipSocks(); break;
            case "gloves": powerUps.EquipGloves(); break;
        }
    }
}