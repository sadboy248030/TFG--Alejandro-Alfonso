using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
// using TuJuego.Inventario; // Descomenta si tus clases de inventario y personaje están en este namespace
// using TopDown; // Si Character.cs está en el namespace TopDown

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [Header("Panel Principal del Inventario")]
    [SerializeField] private GameObject inventoryPanelRoot;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private int maxSlotsToDisplay = 24;
    // La tecla de toggle ahora la gestiona el UIManager principal, pero la dejamos por si se usa en otro lado.
    // [SerializeField] private KeyCode toggleInventoryKey = KeyCode.I; 

    [Header("Panel de Detalles/Acciones del Objeto (Inventario Principal)")]
    [SerializeField] private GameObject itemInfoActionPanel_MainInv;
    [SerializeField] private RectTransform itemInfoActionPanelRect_MainInv;
    [SerializeField] private TextMeshProUGUI itemNameText_MainInv;
    [SerializeField] private TextMeshProUGUI itemDescriptionText_MainInv;
    [SerializeField] private TextMeshProUGUI itemStatsText_MainInv;
    [SerializeField] private Button useButton_MainInv;
    [SerializeField] private Button equipButton_MainInv;
    [SerializeField] private Button discardButton_MainInv;
    [SerializeField] private Button closeInfoButton_MainInv;

    [Header("Configuración de Posición del Panel de Info")]
    [SerializeField] private float infoPanelOffsetX_MainInv = 10f;
    [SerializeField] private float infoPanelOffsetY_MainInv = 0f;

    [Header("Referencias del Jugador")]
    [SerializeField] private GameObject playerGameObject;
    private Character _playerCharacterComponent;

    private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();
    private InventorySlot _currentlySelectedSlotData_MainInv;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Validaciones
        if (inventoryPanelRoot == null) Debug.LogError("InventoryUIManager: 'inventoryPanelRoot' no asignado.", this);
        if (slotsContainer == null) Debug.LogError("InventoryUIManager: 'slotsContainer' no asignado.", this);
        if (inventorySlotPrefab == null) Debug.LogError("InventoryUIManager: 'inventorySlotPrefab' no asignado.", this);

        if (itemInfoActionPanel_MainInv != null)
        {
            if (itemInfoActionPanelRect_MainInv == null) itemInfoActionPanelRect_MainInv = itemInfoActionPanel_MainInv.GetComponent<RectTransform>();
            if (itemInfoActionPanelRect_MainInv == null) Debug.LogError("InventoryUIManager: 'itemInfoActionPanel_MainInv' no tiene RectTransform.", this);
        }
        else
        {
            Debug.LogWarning("InventoryUIManager: 'itemInfoActionPanel_MainInv' no asignado.", this);
        }

        if (playerGameObject != null)
            _playerCharacterComponent = playerGameObject.GetComponent<Character>();
        else
        {
            // Intentar encontrar el jugador si no está asignado
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerGameObject = player;
                _playerCharacterComponent = player.GetComponent<Character>();
            }
            else
            {
                Debug.LogWarning("InventoryUIManager: 'playerGameObject' no asignado y no se encontró ningún objeto con el tag 'Player'.", this);
            }
        }
    }

    void OnEnable()
    {
        // En OnEnable solo nos suscribimos al evento.
        // La actualización inicial se hará en Start.
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    void OnDisable()
    {
        // Es importante desuscribirse cuando el panel se desactiva/destruye.
        if (PlayerInventory.Instance != null)
            PlayerInventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    void Start()
    {
        // Ocultar el panel de información de ítem.
        if (itemInfoActionPanel_MainInv != null) itemInfoActionPanel_MainInv.SetActive(false);

        // 1. Primero, creamos los slots visuales de la UI.
        InitializeInventoryUI();

        // 2. AHORA, una vez que los slots ya existen, los poblamos con los datos del inventario.
        RefreshInventoryDisplay();

        // 3. Finalmente, configuramos los listeners de los botones.
        if (useButton_MainInv != null) useButton_MainInv.onClick.AddListener(OnUseButtonClicked_MainInv);
        if (equipButton_MainInv != null) equipButton_MainInv.onClick.AddListener(OnEquipButtonClicked_MainInv);
        if (discardButton_MainInv != null) discardButton_MainInv.onClick.AddListener(OnDiscardButtonClicked_MainInv);
        if (closeInfoButton_MainInv != null) closeInfoButton_MainInv.onClick.AddListener(HideItemInfoActionPanel_MainInv);
    }

    void Update()
    {
        // El UIManager principal ahora maneja el toggle, por lo que el Input aquí ya no es necesario
        // para abrir/cerrar. Lo mantenemos por si el jugador quiere cerrar el panel de info con ESC.
        if (itemInfoActionPanel_MainInv != null && itemInfoActionPanel_MainInv.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideItemInfoActionPanel_MainInv();
        }
    }

    private void InitializeInventoryUI()
    {
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        uiSlots.Clear();
        for (int i = 0; i < maxSlotsToDisplay; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsContainer);
            slotGO.name = "InventorySlotUI_Main_" + i;
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>();
            if (slotUIComponent != null)
            {
                uiSlots.Add(slotUIComponent);
                slotUIComponent.OnSlotClickedAction = HandleInventorySlotClicked_MainInv;
                slotUIComponent.ClearSlotDisplay();
            }
            else
                Debug.LogError("InventoryUIManager: El prefab 'inventorySlotPrefab' no tiene el componente InventorySlotUI.", this);
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshInventoryDisplay();

        if (_currentlySelectedSlotData_MainInv != null &&
            (_currentlySelectedSlotData_MainInv.item == null || _currentlySelectedSlotData_MainInv.quantity <= 0 ||
                (PlayerInventory.Instance != null && !PlayerInventory.Instance.HasItem(_currentlySelectedSlotData_MainInv.item, 1))))
        {
            HideItemInfoActionPanel_MainInv();
        }
        else if (_currentlySelectedSlotData_MainInv != null && itemInfoActionPanel_MainInv != null && itemInfoActionPanel_MainInv.activeSelf)
        {
            DisplayItemInfoAndActions_MainInv(_currentlySelectedSlotData_MainInv.item, null);
        }
    }

    public void RefreshInventoryDisplay()
    {
        if (PlayerInventory.Instance == null) return;

        // Asegurarnos de que tenemos la cantidad correcta de slots visuales
        if (uiSlots.Count != maxSlotsToDisplay)
        {
            InitializeInventoryUI();
        }

        List<InventorySlot> playerSlotsData = PlayerInventory.Instance.inventorySlots;
        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < playerSlotsData.Count)
                uiSlots[i].UpdateSlotDisplay(playerSlotsData[i]);
            else
                uiSlots[i].ClearSlotDisplay();
        }
    }

    public void HandleInventorySlotClicked_MainInv(InventorySlot clickedSlotData, RectTransform clickedSlotRectTransform)
    {
        if (itemInfoActionPanel_MainInv == null) return;

        if (clickedSlotData == null || clickedSlotData.item == null)
        {
            HideItemInfoActionPanel_MainInv();
            _currentlySelectedSlotData_MainInv = null;
            return;
        }

        if (itemInfoActionPanel_MainInv.activeSelf && _currentlySelectedSlotData_MainInv == clickedSlotData)
        {
            HideItemInfoActionPanel_MainInv();
            return;
        }

        _currentlySelectedSlotData_MainInv = clickedSlotData;
        DisplayItemInfoAndActions_MainInv(clickedSlotData.item, clickedSlotRectTransform);
    }

    private void DisplayItemInfoAndActions_MainInv(ItemData item, RectTransform clickedUITransform)
    {
        if (itemInfoActionPanel_MainInv == null || item == null) { HideItemInfoActionPanel_MainInv(); return; }

        if (itemNameText_MainInv != null) itemNameText_MainInv.text = item.itemName;
        if (itemDescriptionText_MainInv != null) itemDescriptionText_MainInv.text = item.description;

        if (itemStatsText_MainInv != null)
        {
            if (item.isEquipable)
            {
                StringBuilder statsBuilder = new StringBuilder();
                if (item.attackBonus != 0) statsBuilder.AppendLine("Ataque: " + item.attackBonus);
                // ... (más stats)
                itemStatsText_MainInv.text = statsBuilder.ToString();
                itemStatsText_MainInv.gameObject.SetActive(statsBuilder.Length > 0);
            }
            else
                itemStatsText_MainInv.gameObject.SetActive(false);
        }

        if (useButton_MainInv != null) useButton_MainInv.gameObject.SetActive(item.isConsumable);
        if (equipButton_MainInv != null) equipButton_MainInv.gameObject.SetActive(item.isEquipable);
        if (discardButton_MainInv != null) discardButton_MainInv.gameObject.SetActive(true);

        if (clickedUITransform != null && itemInfoActionPanelRect_MainInv != null)
        {
            itemInfoActionPanel_MainInv.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemInfoActionPanelRect_MainInv);
            Vector3[] slotCorners = new Vector3[4];
            clickedUITransform.GetWorldCorners(slotCorners);
            Vector2 targetPositionForInfoPanel = new Vector2(
                slotCorners[2].x + infoPanelOffsetX_MainInv,
                slotCorners[2].y + infoPanelOffsetY_MainInv
            );
            itemInfoActionPanelRect_MainInv.position = targetPositionForInfoPanel;
        }
        else if (itemInfoActionPanel_MainInv != null && !itemInfoActionPanel_MainInv.activeSelf)
        {
            itemInfoActionPanel_MainInv.SetActive(true);
        }
    }

    public void HideItemInfoActionPanel_MainInv()
    {
        if (itemInfoActionPanel_MainInv != null) itemInfoActionPanel_MainInv.SetActive(false);
        _currentlySelectedSlotData_MainInv = null;
    }

    public void OnUseButtonClicked_MainInv()
    {
        if (_currentlySelectedSlotData_MainInv != null && _currentlySelectedSlotData_MainInv.item != null && _currentlySelectedSlotData_MainInv.item.isConsumable)
        {
            if (_playerCharacterComponent == null) { Debug.LogError("InventoryUIManager: _playerCharacterComponent no asignado.", this); return; }
            bool itemWasUsedSuccessfully = _currentlySelectedSlotData_MainInv.item.Use(_playerCharacterComponent);
            if (itemWasUsedSuccessfully)
            {
                PlayerInventory.Instance.RemoveItem(_currentlySelectedSlotData_MainInv.item, 1);
            }
        }
    }

    public void OnEquipButtonClicked_MainInv()
    {
        if (_currentlySelectedSlotData_MainInv != null && _currentlySelectedSlotData_MainInv.item != null && _currentlySelectedSlotData_MainInv.item.isEquipable)
        {
            // En lugar de llamar a EquipmentScreenManager.Instance.OpenForEquipping,
            // ahora podríamos llamar al UIManager principal para que gestione el cambio de pantalla.
            if (UIManager.Instance != null)
            {
                // Primero cerramos este panel de inventario
                UIManager.Instance.ToggleInventoryUI();
                // Luego abrimos el de equipamiento
                UIManager.Instance.ToggleEquipmentUI(); // Asumiendo que EquipmentScreenManager se encargará del resto
            }

            HideItemInfoActionPanel_MainInv();
        }
    }

    public void OnDiscardButtonClicked_MainInv()
    {
        if (_currentlySelectedSlotData_MainInv != null && _currentlySelectedSlotData_MainInv.item != null && PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.RemoveItem(_currentlySelectedSlotData_MainInv.item, _currentlySelectedSlotData_MainInv.quantity);
            HideItemInfoActionPanel_MainInv();
        }
    }
}