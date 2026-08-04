//TPFinal - Pedro Valle

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using AnteNoctem.Core;

public class ChapuceroShop : Singleton<ChapuceroShop>
{
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

    [Header("UI de Materiales del Jugador (En el Shop)")]
    public TMP_Text hiloCountText;
    public TMP_Text telaCountText;
    public TMP_Text cueroCountText;

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

        UpdateMaterialUI(); // Actualizamos las cantidades al abrir
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

        StartCoroutine(ForceLockNextFrame());
    }

    private System.Collections.IEnumerator ForceLockNextFrame()
    {
        yield return null;
        yield return null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

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

        string txt = yaLoTiene ? alreadyPurchasedText : $"{item.displayName}: {item.description}";
        if (dialogueTextBuy != null) dialogueTextBuy.text = txt;

        // Muestra el costo en pantalla de manera explícita
        if (costText != null)
        {
            costText.text = $"Hilo {item.hiloCost} | Tela {item.telaCost} | Cuero {item.cueroCost}";
        }

        SetActivePanel(showNext: false, showBuy: true, showSelector: false);

        if (buyButton != null) buyButton.interactable = !yaLoTiene;
    }

    void SetActivePanel(bool showNext, bool showBuy, bool showSelector)
    {
        if (dialogueBoxNext != null) dialogueBoxNext.SetActive(showNext);
        if (dialogueBoxBuy != null) dialogueBoxBuy.SetActive(showBuy);
        if (itemSelectorContainer != null) itemSelectorContainer.gameObject.SetActive(showSelector);
    }

    void OnNextClicked() { ShowItemList(); }
    void OnRejectClicked() { ShowItemList(); }

    void OnBuyClicked()
    {
        if (currentlyViewedItem == null) return;

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

        var inv = MaterialInventory.Instance;
        if (inv != null)
        {
            inv.RemoveMaterials(MaterialInventory.MaterialType.Hilo, currentlyViewedItem.hiloCost);
            inv.RemoveMaterials(MaterialInventory.MaterialType.Tela, currentlyViewedItem.telaCost);
            inv.RemoveMaterials(MaterialInventory.MaterialType.Cuero, currentlyViewedItem.cueroCost);
        }

        // Actualizamos los contadores inmediatamente tras descontar
        UpdateMaterialUI();

        EquipItem(currentlyViewedItem);
        currentlyViewedItem.isPurchased = true;

        if (dialogueTextBuy != null) dialogueTextBuy.text = purchaseSuccessText;
        if (buyButton != null) buyButton.interactable = false;
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
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var powerUps = player.GetComponent<PlayerPowerUps>();
        if (powerUps == null) return;

        switch (item.itemId)
        {
            case "boots": powerUps.EquipBoots(); break;
            case "socks": powerUps.EquipSocks(); break;
            case "gloves": powerUps.EquipGloves(); break;
        }
    }

    public void UpdateMaterialUI()
    {
        var inv = MaterialInventory.Instance;
        if (inv == null)
        {
            Debug.LogError("[Shop UI Error] MaterialInventory.Instance es NULL. ¿Existe el GameObject en la escena?");
            return;
        }

        int hilo = inv.GetCount(MaterialInventory.MaterialType.Hilo);
        int tela = inv.GetCount(MaterialInventory.MaterialType.Tela);
        int cuero = inv.GetCount(MaterialInventory.MaterialType.Cuero);

        Debug.Log($"[Shop UI Info] Datos del inventario -> Hilo: {hilo}, Tela: {tela}, Cuero: {cuero}");

        if (hiloCountText != null)
        {
            hiloCountText.text = hilo.ToString();
            Debug.Log("[Shop UI] hiloCountText actualizado a " + hilo);
        }
        else Debug.LogWarning("[Shop UI Error] hiloCountText NO está asignado en el Inspector.");

        if (telaCountText != null)
        {
            telaCountText.text = tela.ToString();
            Debug.Log("[Shop UI] telaCountText actualizado a " + tela);
        }
        else Debug.LogWarning("[Shop UI Error] telaCountText NO está asignado en el Inspector.");

        if (cueroCountText != null)
        {
            cueroCountText.text = cuero.ToString();
            Debug.Log("[Shop UI] cueroCountText actualizado a " + cuero);
        }
        else Debug.LogWarning("[Shop UI Error] cueroCountText NO está asignado en el Inspector.");
    }
}