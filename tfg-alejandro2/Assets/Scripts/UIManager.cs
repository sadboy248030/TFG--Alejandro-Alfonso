using UnityEngine;
using TopDown; // Aseg�rate de que este namespace es correcto para tu clase Character

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Referencias a Prefabs de Paneles")]
    [SerializeField] private GameObject inventoryUIPrefab;
    [SerializeField] private GameObject questLogUIPrefab;
    [SerializeField] private GameObject equipmentUIPrefab;
    [SerializeField] private GameObject characterStatsUIPrefab;

    // Una sola variable para el panel activo y otra para su tipo
    private GameObject currentUIInstance;
    private System.Type currentUIType;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Escuchar las teclas para abrir/cerrar los paneles
        if (Input.GetKeyDown(KeyCode.I)) ToggleInventoryUI();
        if (Input.GetKeyDown(KeyCode.M)) ToggleEquipmentUI(); // M para equipo (Map)
        if (Input.GetKeyDown(KeyCode.C)) ToggleStatsUI(); // C para stats (Character)
        // if (Input.GetKeyDown(KeyCode.U)) TogglePanel(typeof(QuestLogUIManager)); // U para misiones (qUest)
    }

    /// <summary>
    /// Cierra cualquier panel de UI que est� actualmente abierto.
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (currentUIInstance != null)
        {
            Destroy(currentUIInstance);
            currentUIInstance = null;
            currentUIType = null;
        }
    }

    // --- M�TODOS P�BLICOS ESPEC�FICOS ---
    // Estos m�todos sirven como una API p�blica clara para que otros scripts
    // puedan solicitar la apertura/cierre de paneles espec�ficos.
    public void ToggleInventoryUI()
    {
        TogglePanel(typeof(InventoryUIManager));
    }

    public void ToggleEquipmentUI()
    {
        TogglePanel(typeof(EquipmentScreenManager));
    }

    public void ToggleStatsUI()
    {
        TogglePanel(typeof(CharacterStatsScreenManager));
    }

    /// <summary>
    /// L�gica interna para gestionar la apertura y cierre de un panel.
    /// Si el panel solicitado ya est� abierto, lo cierra.
    /// Si otro panel est� abierto, lo cierra y abre el nuevo.
    /// </summary>
    private void TogglePanel(System.Type panelType)
    {
        if (currentUIInstance != null && currentUIType == panelType)
        {
            // Si el panel que se quiere abrir es el que ya est� abierto, lo cerramos.
            CloseCurrentPanel();
        }
        else
        {
            // Si no, cerramos el actual (si lo hay) y abrimos el nuevo.
            CloseCurrentPanel();

            // L�gica para abrir el panel solicitado
            if (panelType == typeof(InventoryUIManager))
            {
                OpenPanel(inventoryUIPrefab, typeof(InventoryUIManager));
            }
            else if (panelType == typeof(EquipmentScreenManager))
            {
                OpenPanel(equipmentUIPrefab, typeof(EquipmentScreenManager));
            }
            else if (panelType == typeof(CharacterStatsScreenManager))
            {
                // La pantalla de estado necesita un personaje para mostrarse, as� que tiene un caso especial
                Character defaultChar = GetDefaultCharacter();
                if (defaultChar != null)
                {
                    OpenStatsScreen(defaultChar);
                }
            }
            // A�adir m�s 'else if' para otros paneles
        }
    }

    // M�todo privado para abrir un panel gen�rico
    private void OpenPanel(GameObject panelPrefab, System.Type panelType)
    {
        Transform mainCanvas = FindMainCanvasTransform();
        if (mainCanvas != null && panelPrefab != null)
        {
            currentUIInstance = Instantiate(panelPrefab, mainCanvas);
            currentUIType = panelType;
        }
    }

    /// <summary>
    /// M�todo p�blico espec�fico para abrir la pantalla de estado para un personaje concreto.
    /// Es llamado por EquipmentScreenManager.
    /// </summary>
    public void OpenStatsScreen(Character characterToShow)
    {
        // Cerramos cualquier panel que estuviera abierto (ej: la pantalla de equipo)
        CloseCurrentPanel();

        Transform mainCanvas = FindMainCanvasTransform();
        if (mainCanvas != null && characterStatsUIPrefab != null && characterToShow != null)
        {
            // Instanciamos el nuevo panel de stats
            currentUIInstance = Instantiate(characterStatsUIPrefab, mainCanvas);
            currentUIType = typeof(CharacterStatsScreenManager);

            // Obtenemos su script gestor y le pasamos los datos del personaje
            CharacterStatsScreenManager statsManager = currentUIInstance.GetComponent<CharacterStatsScreenManager>();
            if (statsManager != null)
            {
                statsManager.DisplayCharacterData(characterToShow);
            }
        }
    }

    private Character GetDefaultCharacter()
    {
        // Intenta obtener el primer personaje de la party
        if (PartyManager.Instance != null && PartyManager.Instance.CurrentPartyMembers.Count > 0)
        {
            return PartyManager.Instance.CurrentPartyMembers[0];
        }

        // Si no, busca el GameObject del jugador por tag
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            return playerGO.GetComponent<Character>();
        }

        Debug.LogWarning("UIManager: No se pudo encontrar un personaje por defecto (ni PartyManager ni objeto con tag 'Player').");
        return null;
    }

    private Transform FindMainCanvasTransform()
    {
        GameObject mainCanvasGO = GameObject.FindGameObjectWithTag("MainCanvas");
        if (mainCanvasGO != null)
        {
            return mainCanvasGO.transform;
        }
        Debug.LogError("UIManager: �No se encontr� ning�n Canvas con el tag 'MainCanvas' en la escena!");
        return null;
    }
}
