using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using TopDown; // Asumiendo que PlayerMovement y Character est�n aqu�
// Aseg�rate de que el namespace de tus otras clases sea correcto si los usas
// using TuJuego.Inventario; 
// using TuJuego.Personajes; 

public class EquipmentScreenManager : MonoBehaviour
{
    public static EquipmentScreenManager Instance { get; private set; }

    [Header("Paneles Principales de la UI")]
    [SerializeField] private GameObject equipmentScreenPanel;
    [SerializeField] private TextMeshProUGUI screenTitleText;

    [Header("Panel Izquierdo - Detalles del Personaje")]
    [SerializeField] private GameObject characterDisplayPanel;
    [SerializeField] private Image characterSpriteImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [SerializeField] private TextMeshProUGUI characterStatsTextDisplay;

    [Header("Panel Izquierdo - Slots de Equipamiento del Personaje")]
    [SerializeField] private Transform characterEquipmentSlotsContainer;
    // [SerializeField] private GameObject characterEquipmentSlotUIPrefab; 

    [Header("Panel Izquierdo - Selecci�n de Miembros de la Party")]
    [Tooltip("Transform padre donde se instanciar�n los iconos/botones para seleccionar miembros de la party.")]
    [SerializeField] private Transform partyMemberSelectionContainer;
    [Tooltip("Prefab para un icono/bot�n de selecci�n de miembro de la party.")]
    [SerializeField] private GameObject partyMemberSelectIconPrefab;

    [Header("Panel Derecho - Inventario del Jugador")]
    [SerializeField] private GameObject inventoryDisplayPanel_EquipmentScreen;
    [SerializeField] private GameObject inventorySlotUIPrefab_ForEquipScreen;

    [Header("Panel de Info/Acciones del �tem")]
    [Tooltip("Panel para mostrar detalles y acciones del �tem (equipado o del inventario).")]
    [SerializeField] private GameObject itemInfoActionPanel;
    [SerializeField] private RectTransform itemInfoActionPanelRect;
    [SerializeField] private TextMeshProUGUI itemNameText_InfoPanel;
    [SerializeField] private TextMeshProUGUI itemDescriptionText_InfoPanel;
    [SerializeField] private TextMeshProUGUI itemStatsText_InfoPanel;
    [SerializeField] private Button useButton_InfoPanel;
    [SerializeField] private Button equipButton_InfoPanel;
    [SerializeField] private Button unequipButton_InfoPanel;
    [SerializeField] private Button discardButton_InfoPanel;
    [SerializeField] private Button closeInfoButton_InfoPanel;

    // --- NUEVO: Bot�n para ir a la Pantalla de Estado/Datos ---
    [Header("Navegaci�n Adicional")]
    [Tooltip("Bot�n en la pantalla de equipamiento para abrir la pantalla de estado/datos del personaje actual.")]
    [SerializeField] private Button viewCharacterStatsButton;

    [Header("Configuraci�n de Posici�n del Panel de Info")]
    [SerializeField] private float infoPanelOffsetX = 10f;
    [SerializeField] private float infoPanelOffsetY = 0f;

    [Header("Input")]
    [SerializeField] private KeyCode toggleEquipmentScreenKey = KeyCode.U;

    private Character _currentlyDisplayedCharacter;
    private InventorySlot _currentlySelectedItemFromInventory;
    private ItemData _currentlySelectedItemFromEquipment;
    private EquipmentSlot _currentlySelectedEquipmentSlot;

    private List<PartyMemberSelectIconUI> _partyMemberIconUIs = new List<PartyMemberSelectIconUI>();
    private Dictionary<EquipmentSlot, CharacterEquipmentSlotUI> _characterEquipmentSlotUIs = new Dictionary<EquipmentSlot, CharacterEquipmentSlotUI>();
    private List<InventorySlotUI> _inventorySlotUIs_EquipmentScreen = new List<InventorySlotUI>();


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Validaciones
        if (equipmentScreenPanel == null) Debug.LogError("ESM: 'equipmentScreenPanel' no asignado.", this);
        if (partyMemberSelectionContainer == null) Debug.LogError("ESM: 'partyMemberSelectionContainer' no asignado.", this);
        if (partyMemberSelectIconPrefab == null) Debug.LogError("ESM: 'partyMemberSelectIconPrefab' no asignado. No se podr�n crear los iconos de selecci�n de party.", this);
        if (viewCharacterStatsButton == null) Debug.LogWarning("ESM: 'viewCharacterStatsButton' no asignado. No se podr� navegar a la pantalla de stats desde aqu�.", this); // NUEVA VALIDACI�N
        if (characterDisplayPanel == null) Debug.LogError("ESM: 'characterDisplayPanel' no asignado.", this);
        if (characterSpriteImage == null) Debug.LogError("ESM: 'characterSpriteImage' no asignado.", this);
        if (characterNameText == null) Debug.LogError("ESM: 'characterNameText' no asignado.", this);
        if (characterLevelText == null) Debug.LogError("ESM: 'characterLevelText' no asignado.", this);
        if (characterStatsTextDisplay == null) Debug.LogError("ESM: 'characterStatsTextDisplay' no asignado.", this);
        if (characterEquipmentSlotsContainer == null) Debug.LogError("ESM: 'characterEquipmentSlotsContainer' no asignado.", this);
        if (inventoryDisplayPanel_EquipmentScreen == null) Debug.LogError("ESM: 'inventoryDisplayPanel_EquipmentScreen' no asignado.", this);
        else if (inventoryDisplayPanel_EquipmentScreen.GetComponent<GridLayoutGroup>() == null) Debug.LogError("ESM: 'inventoryDisplayPanel_EquipmentScreen' NO tiene GridLayoutGroup.", this);
        if (inventorySlotUIPrefab_ForEquipScreen == null) Debug.LogError("ESM: 'inventorySlotUIPrefab_ForEquipScreen' no asignado.", this);
        if (itemInfoActionPanel != null)
        {
            if (itemInfoActionPanelRect == null) itemInfoActionPanelRect = itemInfoActionPanel.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("ESM: 'itemInfoActionPanel' NO EST� ASIGNADO.", this);
        }
    }

    void OnEnable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.OnInventoryChanged += HandlePlayerInventoryChanged;
        else
            Debug.LogWarning("ESM: PlayerInventory.Instance es null en OnEnable.");
        if (PartyManager.Instance != null)
        {
            PartyManager.OnSelectedMenuCharacterChanged += HandleSelectedMenuCharacterChanged;
            PartyManager.OnPartyRosterChanged += HandlePartyRosterChanged; // Para refrescar iconos si la party cambia
        }
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.OnInventoryChanged -= HandlePlayerInventoryChanged;
        if (PartyManager.Instance != null)
        {
            PartyManager.OnSelectedMenuCharacterChanged -= HandleSelectedMenuCharacterChanged;
            PartyManager.OnPartyRosterChanged -= HandlePartyRosterChanged;
        }
    }

    void Start()
    {
        // 1. Ocultar los sub-paneles al inicio
        if (itemInfoActionPanel != null) itemInfoActionPanel.SetActive(false);

        // 2. Crear todos los elementos visuales de la UI
        InitializeCharacterEquipmentSlots();
        InitializeInventoryForEquipmentScreen();
        PopulatePartySelection();

        // 3. Determinar el personaje inicial a mostrar
        Character characterToDisplay = null;
        if (PartyManager.Instance != null && PartyManager.Instance.CurrentPartyMembers.Count > 0)
        {
            characterToDisplay = PartyManager.Instance.CurrentPartyMembers[0];
        }
        else // Fallback si no hay PartyManager
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) characterToDisplay = playerGO.GetComponent<Character>();
        }

        // 4. Poblar toda la UI con los datos iniciales
        SelectCharacterForDisplay(characterToDisplay);
        // La l�nea de arriba ya llama a los m�todos para refrescar stats y equipo.
        // Tambi�n refrescamos el inventario.
        RefreshInventoryForEquipmentScreen();

        // 5. Asignar los listeners de los botones
        if (useButton_InfoPanel != null) useButton_InfoPanel.onClick.AddListener(OnUseItemClicked_InfoPanel);
        if (equipButton_InfoPanel != null) equipButton_InfoPanel.onClick.AddListener(OnEquipItemClicked_InfoPanel);
        if (unequipButton_InfoPanel != null) unequipButton_InfoPanel.onClick.AddListener(OnUnequipItemClicked_InfoPanel);
        if (discardButton_InfoPanel != null) discardButton_InfoPanel.onClick.AddListener(OnDiscardItemClicked_InfoPanel);
        if (closeInfoButton_InfoPanel != null) closeInfoButton_InfoPanel.onClick.AddListener(HideItemInfoActionPanel);
        if (viewCharacterStatsButton != null) viewCharacterStatsButton.onClick.AddListener(OnViewCharacterStatsClicked);
    }

    void Update()
    {
        
        if (equipmentScreenPanel != null && equipmentScreenPanel.activeSelf &&
            itemInfoActionPanel != null && itemInfoActionPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            HideItemInfoActionPanel();
        }
    }

    public void ToggleEquipmentScreen()
    {
        if (equipmentScreenPanel == null) return;
        bool isActive = equipmentScreenPanel.activeSelf;
        equipmentScreenPanel.SetActive(!isActive);

        if (equipmentScreenPanel.activeSelf)
        {
            PopulatePartySelection(); // Refrescar/Crear los iconos de la party

            Character characterToDisplay = null;
            // Usar el personaje seleccionado por PartyManager o el primero si no hay ninguno
            if (PartyManager.Instance != null)
            {
                characterToDisplay = PartyManager.Instance.SelectedMenuCharacter; // Usar el que PartyManager tiene como seleccionado
                if (characterToDisplay == null && PartyManager.Instance.CurrentPartyMembers.Count > 0)
                {
                    characterToDisplay = PartyManager.Instance.CurrentPartyMembers[0]; // Fallback al primero de la party
                    PartyManager.Instance.SetSelectedMenuCharacter(characterToDisplay); // Establecerlo como seleccionado
                }
            }
            // Fallback final si no hay PartyManager o party vac�a
            if (characterToDisplay == null)
            {
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null) characterToDisplay = pm.GetComponent<Character>();
            }

            SelectCharacterForDisplay(characterToDisplay);
            RefreshInventoryForEquipmentScreen();
            HideItemInfoActionPanel();
        }
        else
        {
            HideItemInfoActionPanel();
        }
    }

    /// <summary>
    /// Crea o actualiza los iconos de selecci�n para cada miembro de la party en la UI.
    /// </summary>
    private void PopulatePartySelection()
    {
        if (partyMemberSelectionContainer == null || partyMemberSelectIconPrefab == null)
        {
            Debug.LogError("ESM: PopulatePartySelection - Falta 'partyMemberSelectionContainer' o 'partyMemberSelectIconPrefab'.");
            return;
        }

        foreach (Transform child in partyMemberSelectionContainer)
        {
            Destroy(child.gameObject);
        }
        _partyMemberIconUIs.Clear();

        // --- OBTENER LA PARTY DESDE PARTYMANAGER ---
        List<Character> partyToDisplay = new List<Character>();
        if (PartyManager.Instance != null)
        {
            partyToDisplay = PartyManager.Instance.CurrentPartyMembers;
            Debug.Log("ESM: PopulatePartySelection - Obtenidos " + partyToDisplay.Count + " miembros desde PartyManager.");
        }
        else
        {
            Debug.LogWarning("ESM: PopulatePartySelection - PartyManager.Instance es NULL. No se pueden poblar los iconos de party.");
            // Como fallback, podr�as intentar a�adir el jugador principal si no hay PartyManager (como antes)
            // PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            // if (pm != null) { Character mc = pm.GetComponent<Character>(); if (mc != null) partyToDisplay.Add(mc); }
        }
        // --- FIN OBTENER LA PARTY ---

        if (partyToDisplay.Count == 0)
        {
            Debug.Log("ESM: PopulatePartySelection - No hay personajes en la party para mostrar iconos.");
            SelectCharacterForDisplay(null); // Limpiar la UI si no hay personajes
            return;
        }

        foreach (Character member in partyToDisplay)
        {
            if (member == null) continue;

            GameObject iconGO = Instantiate(partyMemberSelectIconPrefab, partyMemberSelectionContainer);
            iconGO.name = "PartyIcon_" + member.characterName;
            PartyMemberSelectIconUI iconUI = iconGO.GetComponent<PartyMemberSelectIconUI>();
            if (iconUI != null)
            {
                iconUI.SetupIcon(member); // El script del icono se encarga de su bot�n y de llamar a SelectCharacterForDisplay
                _partyMemberIconUIs.Add(iconUI);
            }
            else
            {
                Debug.LogError("ESM: El prefab 'partyMemberSelectIconPrefab' no tiene el componente PartyMemberSelectIconUI.", this);
            }
        }
    }

    private void InitializeCharacterEquipmentSlots()
    {
        if (characterEquipmentSlotsContainer == null) return; // Salir si el contenedor no est� asignado.
        _characterEquipmentSlotUIs.Clear(); // Limpiar el diccionario por si se llama varias veces.

        // Obtener todos los scripts CharacterEquipmentSlotUI que son hijos del contenedor.
        CharacterEquipmentSlotUI[] slotsInScene = characterEquipmentSlotsContainer.GetComponentsInChildren<CharacterEquipmentSlotUI>();

        foreach (CharacterEquipmentSlotUI slotUI in slotsInScene)
        {
            EquipmentSlot type = slotUI.GetSlotType(); // Obtener el tipo de slot configurado en el Inspector del slotUI.

            if (type != EquipmentSlot.None) // Asegurarse de que el slot tenga un tipo v�lido asignado.
            {
                if (!_characterEquipmentSlotUIs.ContainsKey(type)) // Si no hay ya un UI para este tipo de slot.
                {
                    _characterEquipmentSlotUIs.Add(type, slotUI); // A�adir al diccionario.
                    slotUI.ClearDisplay(); // Asegurar que empiece visualmente vac�o.
                }
            }
        }
    }

    /// <summary>
    /// Establece el personaje cuyos datos se mostrar�n en el panel izquierdo y actualiza la UI.
    /// </summary>
    /// <param name="characterToDisplay">El personaje a mostrar. Puede ser null para limpiar la UI.</param>
    public void SelectCharacterForDisplay(Character characterToDisplay)
    {
        _currentlyDisplayedCharacter = characterToDisplay;
        if (_currentlyDisplayedCharacter == null)
        {
            if (characterNameText != null) characterNameText.text = "---";
            if (characterLevelText != null) characterLevelText.text = "Nvl: --";
            if (characterSpriteImage != null) { characterSpriteImage.sprite = null; characterSpriteImage.enabled = false; }
            if (characterStatsTextDisplay != null) characterStatsTextDisplay.text = "";
            ClearCharacterEquipmentSlotsDisplay();
            Debug.LogWarning("ESM: SelectCharacterForDisplay - Personaje nulo. UI limpiada.");
            return;
        }

        Debug.Log("ESM: Mostrando informaci�n y equipo para: " + _currentlyDisplayedCharacter.characterName);
        if (characterNameText != null) characterNameText.text = _currentlyDisplayedCharacter.characterName;
        if (characterLevelText != null) characterLevelText.text = "Nvl: " + _currentlyDisplayedCharacter.level.ToString();
        if (characterSpriteImage != null)
        {
            characterSpriteImage.sprite = _currentlyDisplayedCharacter.portraitSprite;
            characterSpriteImage.enabled = (characterSpriteImage.sprite != null);
        }
        UpdateCharacterStatsDisplay();
        UpdateCharacterEquipmentSlotsDisplay();
        RefreshInventoryForEquipmentScreen(true);
        HideItemInfoActionPanel(); // Ocultar panel de info de item al cambiar de personaje
    }

    // Actualiza los textos de los stats del personaje actualmente mostrado.
    private void UpdateCharacterStatsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || characterStatsTextDisplay == null) return;
        StringBuilder statsBuilder = new StringBuilder(); // Usar StringBuilder para construir el string de stats eficientemente.
        // Asumimos que Character.cs tiene estas propiedades (MaxHP, MaxMP, Attack, Defense) que ya incluyen bonos de equipo.
        statsBuilder.AppendLine("HP: " + _currentlyDisplayedCharacter.currentHP + "/" + _currentlyDisplayedCharacter.MaxHP);
        statsBuilder.AppendLine("MP: " + _currentlyDisplayedCharacter.currentMP + "/" + _currentlyDisplayedCharacter.MaxMP);
        statsBuilder.AppendLine("Ataque: " + _currentlyDisplayedCharacter.Attack);
        statsBuilder.AppendLine("Defensa: " + _currentlyDisplayedCharacter.Defense);
        // Aqu� podr�as a�adir m�s stats si los tienes definidos en tu clase Character.
        characterStatsTextDisplay.text = statsBuilder.ToString(); // Asignar el string construido al TextMeshPro.
    }

    // Limpia la visualizaci�n de todos los slots de equipo del personaje (los hace parecer vac�os).
    private void ClearCharacterEquipmentSlotsDisplay()
    {
        if (_characterEquipmentSlotUIs == null) return;
        foreach (CharacterEquipmentSlotUI slotUI in _characterEquipmentSlotUIs.Values)
        { // Itera por todos los UI de slot de equipo registrados.
            if (slotUI != null) slotUI.DisplayEquippedItem(null); // Llama al m�todo del slot UI para mostrarlo como vac�o.
        }
    }

    // Actualiza los iconos en los slots de equipo del personaje actualmente mostrado.
    private void UpdateCharacterEquipmentSlotsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || _characterEquipmentSlotUIs == null) { ClearCharacterEquipmentSlotsDisplay(); return; }
        if (_currentlyDisplayedCharacter.equippedItems != null)
        { // Si el personaje tiene datos de equipo.
            // Iterar por cada tipo de slot de equipo definido en el enum EquipmentSlot.
            foreach (EquipmentSlot slotTypeKey in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slotTypeKey == EquipmentSlot.None) continue; // Ignorar el tipo 'None'.

                ItemData equippedItem = null;
                // Intentar obtener el �tem equipado por el personaje en este slotType.
                _currentlyDisplayedCharacter.equippedItems.TryGetValue(slotTypeKey, out equippedItem);

                // Intentar obtener el componente UI para este slotType del diccionario.
                if (_characterEquipmentSlotUIs.TryGetValue(slotTypeKey, out CharacterEquipmentSlotUI slotUIComponent))
                {
                    if (slotUIComponent != null) slotUIComponent.DisplayEquippedItem(equippedItem); // Actualizar el slot UI.
                }
            }
        }
        else { ClearCharacterEquipmentSlotsDisplay(); } // Limpiar si no hay datos de equipo.
    }

    // Inicializa los slots de UI para la vista del inventario en esta pantalla.
    private void InitializeInventoryForEquipmentScreen()
    {
        if (inventoryDisplayPanel_EquipmentScreen == null || inventorySlotUIPrefab_ForEquipScreen == null) return;
        // El contenedor de los slots es el propio panel derecho que tiene el GridLayoutGroup.
        Transform containerTransform = inventoryDisplayPanel_EquipmentScreen.transform;

        // Limpiar slots antiguos para evitar duplicados si este m�todo se llama m�s de una vez.
        foreach (Transform child in containerTransform) Destroy(child.gameObject);
        _inventorySlotUIs_EquipmentScreen.Clear(); // Limpiar la lista de referencias.

        // Determinar cu�ntos slots visuales crear. Idealmente, basado en PlayerInventory.maxInventorySlots.
        int slotsToDisplay = PlayerInventory.Instance != null ? PlayerInventory.Instance.maxInventorySlots : 20; // Usar 20 como fallback.

        // Instanciar un prefab de slot por cada slot a mostrar.
        for (int i = 0; i < slotsToDisplay; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotUIPrefab_ForEquipScreen, containerTransform); // Instanciar como hijo del contenedor.
            slotGO.name = "InventorySlotUI_EquipScreen_" + i; // Ponerle un nombre descriptivo en la Jerarqu�a.
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>(); // Obtener el script del slot.
            if (slotUIComponent != null)
            {
                _inventorySlotUIs_EquipmentScreen.Add(slotUIComponent); // A�adir a la lista de slots de UI.
                // Suscribir el m�todo HandleInventorySlotSelectionOnEquipScreen de ESTE manager al evento OnSlotClickedAction del slot UI.
                slotUIComponent.OnSlotClickedAction = HandleInventorySlotSelectionOnEquipScreen;
                slotUIComponent.ClearSlotDisplay(); // Asegurar que el slot se vea vac�o inicialmente.
            }
            else Debug.LogError("ESM: El prefab 'inventorySlotUIPrefab_ForEquipScreen' no tiene el componente InventorySlotUI.", this);
        }
    }

    // Actualiza la visualizaci�n de los slots del inventario en esta pantalla.
    public void RefreshInventoryForEquipmentScreen(bool filter = false)
    { // El par�metro 'filter' no se usa a�n.
        if (inventoryDisplayPanel_EquipmentScreen == null || PlayerInventory.Instance == null) return;
        List<InventorySlot> playerSlotsData = PlayerInventory.Instance.inventorySlots; // Obtener los datos actuales del inventario del jugador.

        // Comprobar si el n�mero de slots UI creados coincide con el m�ximo del inventario.
        int expectedSlots = PlayerInventory.Instance != null ? PlayerInventory.Instance.maxInventorySlots : 0;
        if (_inventorySlotUIs_EquipmentScreen.Count != expectedSlots && expectedSlots > 0)
        {
            InitializeInventoryForEquipmentScreen(); // Re-inicializar si es necesario.
        }

        // Iterar por todos los slots de UI que hemos creado para esta pantalla.
        for (int i = 0; i < _inventorySlotUIs_EquipmentScreen.Count; i++)
        {
            if (i < playerSlotsData.Count)
            { // Si hay un objeto en el inventario del jugador para este slot de UI.
                _inventorySlotUIs_EquipmentScreen[i].gameObject.SetActive(true); // Asegurar que el slot est� activo.
                _inventorySlotUIs_EquipmentScreen[i].UpdateSlotDisplay(playerSlotsData[i]); // Actualizar el slot de UI con los datos del objeto.
            }
            else
            { // Si no hay m�s objetos en el inventario para los slots de UI restantes.
                _inventorySlotUIs_EquipmentScreen[i].ClearSlotDisplay(); // Limpiar el slot de UI para que se vea vac�o.
            }
        }
    }

    // --- MANEJO DE CLICS EN SLOTS ---

    // M�todo llamado por los InventorySlotUI del panel derecho (inventario) de ESTA pantalla.
    private void HandleInventorySlotSelectionOnEquipScreen(InventorySlot slotData, RectTransform slotRectTransform)
    {
        if (itemInfoActionPanel == null) return; // Si no hay panel de info, no hacer nada.

        // Si se hizo clic en un slot vac�o del inventario.
        if (slotData == null || slotData.item == null)
        {
            HideItemInfoActionPanel(); // Ocultar el panel de info.
            _currentlySelectedItemFromInventory = null; // No hay �tem seleccionado.
            _currentlySelectedItemFromEquipment = null; // Asegurar que no haya selecci�n de equipo.
            return;
        }

        // Si el panel de info ya est� activo Y se hizo clic en el MISMO �tem del inventario que ya estaba seleccionado.
        if (itemInfoActionPanel.activeSelf && _currentlySelectedItemFromInventory == slotData && _currentlySelectedItemFromEquipment == null)
        {
            HideItemInfoActionPanel(); // Ocultar el panel (comportamiento de toggle).
            return;
        }

        _currentlySelectedItemFromInventory = slotData; // Guardar el slot del inventario que fue clickeado.
        _currentlySelectedItemFromEquipment = null;   // Limpiar cualquier selecci�n previa de un �tem equipado.
        _currentlySelectedEquipmentSlot = EquipmentSlot.None; // No hay un slot de equipo del personaje seleccionado.
        DisplayItemInfoAndActions(slotData.item, slotRectTransform, false); // Mostrar info, 'false' porque viene del inventario.
    }

    // M�todo p�blico llamado por los CharacterEquipmentSlotUI del panel izquierdo.
    public void OnCharacterEquipmentSlotClicked(EquipmentSlot slotType, ItemData equippedItemData, RectTransform slotRectTransform)
    {
        if (itemInfoActionPanel == null) return; // Si no hay panel de info, no hacer nada.
        Debug.Log("ESM: Clic en slot de equipo del personaje: " + slotType);

        _currentlySelectedItemFromInventory = null; // Limpiar cualquier selecci�n previa del inventario.
        _currentlySelectedEquipmentSlot = slotType; // Guardar el slot de equipo del personaje que fue clickeado.

        // Obtener el �tem realmente equipado desde los datos del personaje actual.
        ItemData actualItemInSlot = null;
        if (_currentlyDisplayedCharacter != null && _currentlyDisplayedCharacter.equippedItems != null)
        {
            _currentlyDisplayedCharacter.equippedItems.TryGetValue(slotType, out actualItemInSlot);
        }

        if (actualItemInSlot != null) // Si hay un �tem equipado en este slot del personaje.
        {
            _currentlySelectedItemFromEquipment = actualItemInSlot; // Guardar el �tem que est� equipado.
            // Mostrar el panel de informaci�n para este �tem equipado.
            // 'true' porque el �tem viene de un slot de equipo del personaje.
            DisplayItemInfoAndActions(actualItemInSlot, slotRectTransform, true);
        }
        else // Si el slot de equipo del personaje est� vac�o.
        {
            Debug.Log("ESM: Slot de equipo " + slotType + " est� vac�o.");
            _currentlySelectedItemFromEquipment = null; // No hay �tem de equipo seleccionado.

            // Si ten�amos un �tem seleccionado previamente del inventario Y es compatible con este slot vac�o,
            // intentar equiparlo directamente.
            if (_currentlySelectedItemFromInventory != null &&
                _currentlySelectedItemFromInventory.item != null &&
                _currentlySelectedItemFromInventory.item.isEquipable &&
                _currentlySelectedItemFromInventory.item.equipmentSlot == slotType)
            {
                Debug.Log("ESM: Slot de equipo vac�o, intentando equipar �tem del inventario: " + _currentlySelectedItemFromInventory.item.itemName + " en slot " + slotType);
                PerformEquipAction(_currentlySelectedItemFromInventory.item, slotType);
            }
            else
            {
                // Si el slot est� vac�o y no hay nada del inventario para equipar, ocultar el panel de info.
                HideItemInfoActionPanel();
            }
        }
    }

    // Muestra el panel de informaci�n del �tem y configura sus botones.
    private void DisplayItemInfoAndActions(ItemData item, RectTransform clickedUITransform, bool isFromEquipmentSlot)
    {
        if (itemInfoActionPanel == null || item == null) { HideItemInfoActionPanel(); return; }

        // Poblar los campos de texto del panel de informaci�n.
        if (itemNameText_InfoPanel != null) itemNameText_InfoPanel.text = item.itemName;
        if (itemDescriptionText_InfoPanel != null) itemDescriptionText_InfoPanel.text = item.description;

        // Mostrar Stats si el objeto es equipable.
        if (itemStatsText_InfoPanel != null)
        {
            if (item.isEquipable)
            {
                StringBuilder statsBuilder = new StringBuilder();
                if (item.attackBonus != 0) statsBuilder.AppendLine("Ataque: " + item.attackBonus);
                if (item.defenseBonus != 0) statsBuilder.AppendLine("Defensa: " + item.defenseBonus);
                // A�adir m�s stats si los tienes en ItemData.cs
                itemStatsText_InfoPanel.text = statsBuilder.ToString();
                itemStatsText_InfoPanel.gameObject.SetActive(statsBuilder.Length > 0); // Mostrar solo si hay stats
            }
            else itemStatsText_InfoPanel.gameObject.SetActive(false); // Ocultar si no es equipable
        }

        // Configurar visibilidad de los botones de acci�n seg�n el contexto.
        if (useButton_InfoPanel != null) useButton_InfoPanel.gameObject.SetActive(item.isConsumable && !isFromEquipmentSlot); // "Usar" solo para consumibles del inventario.
        if (equipButton_InfoPanel != null) equipButton_InfoPanel.gameObject.SetActive(item.isEquipable && !isFromEquipmentSlot); // "Equipar" solo para equipables del inventario.
        if (unequipButton_InfoPanel != null) unequipButton_InfoPanel.gameObject.SetActive(item.isEquipable && isFromEquipmentSlot); // "Desequipar" solo para �tems ya equipados.
        if (discardButton_InfoPanel != null) discardButton_InfoPanel.gameObject.SetActive(!isFromEquipmentSlot); // "Tirar" solo para �tems del inventario.

        // Posicionar el panel de informaci�n al lado del slot/UI clickeado.
        if (clickedUITransform != null && itemInfoActionPanelRect != null)
        {
            itemInfoActionPanel.SetActive(true); // Asegurar que est� activo antes de calcular layout.
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemInfoActionPanelRect); // Forzar actualizaci�n si tiene LayoutGroups internos.

            Vector3[] slotCorners = new Vector3[4];
            clickedUITransform.GetWorldCorners(slotCorners); // Obtiene las esquinas del slot en coordenadas mundiales.
            // slotCorners[2] es la esquina superior derecha del slot.
            Vector2 targetPositionForInfoPanel = new Vector2(
                slotCorners[2].x + infoPanelOffsetX, // Posicionar a la derecha del slot.
                slotCorners[2].y + infoPanelOffsetY  // Alinear verticalmente con la parte superior del slot (o ajustar con offset).
            );
            itemInfoActionPanelRect.position = targetPositionForInfoPanel; // Establece la posici�n mundial del pivote del panel de info.
        }
        else if (itemInfoActionPanel != null && !itemInfoActionPanel.activeSelf)
        { // Si solo se est�n refrescando datos y el panel estaba oculto.
            itemInfoActionPanel.SetActive(true); // Mostrar el panel.
        }
        // Si ya estaba activo y no hay clickedUITransform (ej: refrescando datos), no se reposiciona.
    }

    // Oculta el panel de informaci�n del �tem y limpia las selecciones actuales.
    public void HideItemInfoActionPanel()
    {
        if (itemInfoActionPanel != null) itemInfoActionPanel.SetActive(false);
        _currentlySelectedItemFromInventory = null;
        _currentlySelectedItemFromEquipment = null;
        _currentlySelectedEquipmentSlot = EquipmentSlot.None;
    }

    // --- M�TODOS LLAMADOS POR LOS BOTONES DEL PANEL DE INFORMACI�N ---
    // Estos m�todos ahora son p�blicos para ser enlazados a los botones desde el Inspector.

    public void OnUseItemClicked_InfoPanel()
    {
        // DEBUG: Para confirmar que este m�todo espec�fico se est� llamando
        Debug.Log("ESM: OnUseItemClicked_InfoPanel - M�TODO LLAMADO.");
        // Solo se puede usar un �tem si fue seleccionado del inventario.
        if (_currentlySelectedItemFromInventory != null && _currentlySelectedItemFromInventory.item != null && _currentlySelectedItemFromInventory.item.isConsumable)
        {

            // Debug.Log("ESM: Bot�n Usar presionado para (del inventario): " + _currentlySelectedItemFromInventory.item.itemName); // Log m�s verboso
            if (_currentlyDisplayedCharacter == null) // Necesitamos un personaje para aplicar el efecto.
            {
                Debug.LogError("ESM: No hay personaje seleccionado (_currentlyDisplayedCharacter es null) para usar el objeto " + _currentlySelectedItemFromInventory.item.itemName, this);
                return;
            }
            bool itemWasUsedSuccessfully = _currentlySelectedItemFromInventory.item.Use(_currentlyDisplayedCharacter);
            if (itemWasUsedSuccessfully)
            {
                // Debug.Log(_currentlySelectedItemFromInventory.item.itemName + " fue usado con �xito en " + _currentlyDisplayedCharacter.characterName); // Log m�s verboso
                PlayerInventory.Instance.RemoveItem(_currentlySelectedItemFromInventory.item, 1);
                // El evento OnInventoryChanged se encargar� de refrescar la UI del inventario.
                // HandlePlayerInventoryChanged tambi�n ocultar� el panel de info si el �tem se agot�.
            }
            else
            {
                Debug.Log(_currentlySelectedItemFromInventory.item.itemName + " no se pudo usar o no tuvo efecto (ej: HP ya al m�ximo).");
            }
        }
        else
        {
            Debug.LogWarning("ESM: OnUseItemClicked_InfoPanel - No hay un objeto consumible seleccionado del inventario, o no hay personaje objetivo.");
        }
    }

    public void OnEquipItemClicked_InfoPanel()
    {
        // Este m�todo se llama cuando se presiona "Equipar" en un �tem seleccionado DEL INVENTARIO.
        if (_currentlySelectedItemFromInventory != null && _currentlySelectedItemFromInventory.item != null &&
            _currentlySelectedItemFromInventory.item.isEquipable && _currentlyDisplayedCharacter != null)
        {
            ItemData itemToEquip = _currentlySelectedItemFromInventory.item;
            // El slot objetivo es el slot natural del �tem (ej: una espada va a MainHand).
            EquipmentSlot targetSlot = itemToEquip.equipmentSlot;

            // Debug.Log("ESM: Bot�n Equipar (desde inventario) para: " + itemToEquip.itemName + " en " + _currentlyDisplayedCharacter.characterName + ", slot: " + targetSlot); // Log m�s verboso
            PerformEquipAction(itemToEquip, targetSlot); // Llama al m�todo auxiliar para equipar.
        }
        // HideItemInfoActionPanel(); // PerformEquipAction ya oculta el panel.
    }

    public void OnUnequipItemClicked_InfoPanel()
    {
        // Este m�todo se llama cuando se presiona "Desequipar" en un �tem que estaba en un SLOT DE EQUIPO del personaje.
        if (_currentlyDisplayedCharacter != null && _currentlySelectedEquipmentSlot != EquipmentSlot.None &&
            _currentlySelectedItemFromEquipment != null && // Usar el �tem que estaba seleccionado desde el equipo.
            PlayerInventory.Instance != null)
        {
            // Debug.Log("ESM: Bot�n Desequipar para slot: " + _currentlySelectedEquipmentSlot + " que tiene " + _currentlySelectedItemFromEquipment.itemName); // Log m�s verboso

            // Llama al m�todo UnequipItem del personaje, pas�ndole el slot del que se quiere desequipar.
            ItemData unequippedItem = _currentlyDisplayedCharacter.UnequipItem(_currentlySelectedEquipmentSlot, PlayerInventory.Instance);

            if (unequippedItem != null) // Si se desequip� algo con �xito.
            {
                UpdateCharacterStatsDisplay(); // Actualizar los stats del personaje en la UI.
                UpdateCharacterEquipmentSlotsDisplay(); // Actualizar los slots de equipo en la UI.
                // El inventario (panel derecho) se refrescar� autom�ticamente por el evento OnInventoryChanged
                // que se dispara cuando el objeto vuelve al PlayerInventory.
            }
        }
        HideItemInfoActionPanel(); // Siempre ocultar el panel de info despu�s de la acci�n.
    }

    public void OnDiscardItemClicked_InfoPanel()
    {
        // Este m�todo se llama cuando se presiona "Tirar" en un �tem seleccionado DEL INVENTARIO.
        if (_currentlySelectedItemFromInventory != null && _currentlySelectedItemFromInventory.item != null && PlayerInventory.Instance != null)
        {
            ItemData itemToDiscard = _currentlySelectedItemFromInventory.item;
            int quantityToDiscard = _currentlySelectedItemFromInventory.quantity; // Tirar todo el stack del slot.
            // Debug.Log("ESM: Bot�n Tirar (desde inventario) para: " + itemToDiscard.itemName + " x" + quantityToDiscard); // Log m�s verboso
            PlayerInventory.Instance.RemoveItem(itemToDiscard, quantityToDiscard);
            // El evento OnInventoryChanged de PlayerInventory deber�a refrescar la UI de slots.
            // HandlePlayerInventoryChanged tambi�n se encargar� de ocultar el panel de info si el �tem desaparece.
        }
        // No permitir tirar �tems directamente equipados (deben desequiparse primero).
        else if (_currentlySelectedItemFromEquipment != null)
        {
            Debug.LogWarning("ESM: Intentando tirar un �tem que est� actualmente equipado (" + _currentlySelectedItemFromEquipment.itemName + "). Primero debe ser desequipado del personaje.");
        }
        HideItemInfoActionPanel(); // Ocultar el panel de info despu�s de intentar tirar.
    }

    // M�todo auxiliar para la acci�n de equipar un �tem en un slot espec�fico.
    private void PerformEquipAction(ItemData itemToEquip, EquipmentSlot targetSlot)
    {
        if (itemToEquip == null || _currentlyDisplayedCharacter == null || PlayerInventory.Instance == null || targetSlot == EquipmentSlot.None)
        {
            Debug.LogError("ESM: PerformEquipAction - Faltan datos necesarios o targetSlot es None.");
            return;
        }
        // Comprobar si el �tem es para el slot correcto.
        if (itemToEquip.equipmentSlot != targetSlot)
        {
            Debug.LogWarning($"ESM: {itemToEquip.itemName} (slot natural: {itemToEquip.equipmentSlot}) no es para el slot objetivo {targetSlot}. No se equipar�.");
            return;
        }

        // 1. Quitar el objeto del inventario (asumimos que se equipa una unidad).
        bool removedFromInv = PlayerInventory.Instance.RemoveItem(itemToEquip, 1);
        if (removedFromInv)
        {
            // 2. Equipar en el personaje. El m�todo EquipItem en Character.cs se encarga de
            //    desequipar el objeto anterior (si lo hay) y devolverlo al inventario.
            bool equipped = _currentlyDisplayedCharacter.EquipItem(itemToEquip, PlayerInventory.Instance);
            if (equipped)
            {
                // Debug.Log("ESM: " + itemToEquip.itemName + " equipado con �xito en " + targetSlot); // Log m�s verboso
                UpdateCharacterStatsDisplay(); // Actualizar stats del personaje en la UI.
                UpdateCharacterEquipmentSlotsDisplay(); // Actualizar los slots de equipo del personaje en la UI.
            }
            else
            {
                // Si no se pudo equipar (ej: restricci�n de clase en Character.EquipItem, o alg�n otro error),
                // devolver el objeto que se quit� del inventario.
                Debug.LogWarning("ESM: No se pudo equipar " + itemToEquip.itemName + " (EquipItem en Character devolvi� false). Devolviendo al inventario.");
                PlayerInventory.Instance.AddItem(itemToEquip, 1);
            }
        }
        else
        {
            Debug.LogError("ESM: No se pudo quitar " + itemToEquip.itemName + " del inventario para equipar. �Realmente estaba en el inventario?");
        }

        _currentlySelectedItemFromInventory = null; // Limpiar la selecci�n del inventario despu�s de intentar equipar.
        HideItemInfoActionPanel(); // Ocultar el panel de info despu�s de la acci�n.
    }

    // M�todo llamado por el evento OnInventoryChanged de PlayerInventory.
    private void HandlePlayerInventoryChanged()
    {
        // Debug.Log("ESM: Evento OnInventoryChanged de PlayerInventory recibido."); // Log m�s verboso
        if (equipmentScreenPanel != null && equipmentScreenPanel.activeSelf) // Solo si esta pantalla est� activa.
        {
            RefreshInventoryForEquipmentScreen(); // Refrescar la vista del inventario en esta pantalla.

            // Si un �tem estaba seleccionado en el panel de informaci�n (y ven�a del inventario)
            // y ese �tem ya no est� (o su cantidad es cero) en el inventario, ocultar el panel de informaci�n.
            if (_currentlySelectedItemFromInventory != null &&
                (_currentlySelectedItemFromInventory.item == null ||
                 _currentlySelectedItemFromInventory.quantity <= 0 ||
                 (PlayerInventory.Instance != null && !PlayerInventory.Instance.HasItem(_currentlySelectedItemFromInventory.item, 1))))
            {
                HideItemInfoActionPanel();
            }
        }
    }

    // --- M�TODO P�BLICO PARA SER LLAMADO DESDE InventoryUIManager ---
    /// <summary>
    /// Abre la pantalla de equipamiento y opcionalmente preselecciona un �tem del inventario
    /// para mostrar su panel de informaci�n.
    /// </summary>
    /// <param name="itemToPreselect">El ItemData del objeto a preseleccionar (puede ser null).</param>
    public void OpenForEquipping(ItemData itemToPreselect)
    {
        if (equipmentScreenPanel == null) return;

        if (!equipmentScreenPanel.activeSelf)
        {
            ToggleEquipmentScreen(); // Esto ya llama a PopulatePartySelection y SelectCharacterForDisplay(default)
        }
        else // Si ya estaba activo, asegurar que la party y el personaje est�n actualizados
        {
            PopulatePartySelection();
            // Si _currentlyDisplayedCharacter es null o queremos re-seleccionar el primero de la party
            if (_currentlyDisplayedCharacter == null && PartyManager.Instance != null && PartyManager.Instance.CurrentPartyMembers.Count > 0)
            {
                SelectCharacterForDisplay(PartyManager.Instance.CurrentPartyMembers[0]);
            }
            else if (_currentlyDisplayedCharacter == null)
            {
                // Fallback si no hay party manager
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null) SelectCharacterForDisplay(pm.GetComponent<Character>());
            }
            RefreshInventoryForEquipmentScreen();
        }

        if (itemToPreselect != null)
        {
            bool itemFoundAndDisplayed = false;
            for (int i = 0; i < _inventorySlotUIs_EquipmentScreen.Count; i++)
            {
                InventorySlotUI slotUI = _inventorySlotUIs_EquipmentScreen[i];
                if (slotUI.CurrentSlotData != null && slotUI.CurrentSlotData.item == itemToPreselect)
                {
                    HandleInventorySlotSelectionOnEquipScreen(slotUI.CurrentSlotData, slotUI.GetComponent<RectTransform>());
                    itemFoundAndDisplayed = true;
                    break;
                }
            }
            if (!itemFoundAndDisplayed) HideItemInfoActionPanel();
        }
        else
        {
            HideItemInfoActionPanel();
        }
    }
    public void OnViewCharacterStatsClicked()
    {
        Debug.Log("[ESM] Bot�n 'Ver Datos/Estado' presionado.");

        // Comprobar que tenemos un personaje seleccionado y que el UIManager existe
        if (_currentlyDisplayedCharacter != null && UIManager.Instance != null)
        {
            // Le pedimos al UIManager que cambie a la pantalla de estado para el personaje actual
            // En lugar de llamar a CharacterStatsScreenManager.Instance, llamamos a UIManager.Instance
            UIManager.Instance.OpenStatsScreen(_currentlyDisplayedCharacter);
        }
        else
        {
            if (_currentlyDisplayedCharacter == null) Debug.LogWarning("ESM: No hay personaje seleccionado para mostrar sus stats.");
            if (UIManager.Instance == null) Debug.LogError("ESM: UIManager.Instance no encontrado. No se puede cambiar de pantalla.");
        }
    }
    // --- NUEVOS M�TODOS HANDLER PARA EVENTOS DE PARTYMANAGER ---
    private void HandleSelectedMenuCharacterChanged(Character selectedCharacter)
    {
        Debug.Log("ESM: Evento OnSelectedMenuCharacterChanged recibido para: " + (selectedCharacter != null ? selectedCharacter.characterName : "NULL"));
        SelectCharacterForDisplay(selectedCharacter); // Actualizar la UI con el nuevo personaje
    }

    private void HandlePartyRosterChanged()
    {
        Debug.Log("ESM: Evento OnPartyRosterChanged recibido. Repoblando iconos de party.");
        PopulatePartySelection(); // Volver a crear los iconos de selecci�n de party

        // Opcional: Si el personaje actualmente mostrado ya no est� en la party, seleccionar uno nuevo
        if (_currentlyDisplayedCharacter != null && PartyManager.Instance != null &&
            !PartyManager.Instance.CurrentPartyMembers.Contains(_currentlyDisplayedCharacter))
        {
            if (PartyManager.Instance.CurrentPartyMembers.Count > 0)
            {
                PartyManager.Instance.SetSelectedMenuCharacter(PartyManager.Instance.CurrentPartyMembers[0]);
                // SelectCharacterForDisplay se llamar� a trav�s del evento OnSelectedMenuCharacterChanged
            }
            else
            {
                SelectCharacterForDisplay(null); // No hay nadie en la party
            }
        }
    }



}