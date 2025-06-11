using UnityEngine;
using System.Collections.Generic;

// Puedes poner esto en un namespace si est�s organizando as� tu c�digo
// namespace TuJuego.Personajes
// {

// Aseg�rate de que ItemData, EquipmentSlot y AbilityData (si est� en otro namespace) sean accesibles
// using TuJuego.Inventario; 
// using TuJuego.Habilidades; 

public class Character : MonoBehaviour
{
    private static List<string> persistentCharacterIDs = new List<string>();
    [Header("Informaci�n B�sica del Personaje")]
    public string characterID; // �IMPORTANTE! A�ade este campo y as�gnale un ID �nico en el Inspector para cada personaje.
    public string characterName = "PersonajeDePrueba";
    public int level = 1;
    [Tooltip("Sprite del retrato del personaje para mostrar en la UI (men�s, party, etc.).")]
    public Sprite portraitSprite;
    // --- NUEVO: Animator Controller para el combate espec�fico del personaje ---
    [Tooltip("Animator Controller a usar para este personaje en combate. Debe contener estados como Idle_Combat, AttackTrigger, HitTrigger, DefeatTrigger, etc.")]
    public RuntimeAnimatorController combatAnimatorController;

    // --- NUEVO: Prefab para el proyectil de ataque b�sico de este personaje ---
    [Tooltip("Si el ataque b�sico de este personaje lanza un proyectil, asigna el Prefab del proyectil aqu�. Si es un ataque cuerpo a cuerpo, d�jalo como None.")]
    public GameObject basicAttackProjectilePrefab;
    // --- FIN NUEVO ---
    // --- NUEVO: Campos para Sonidos de Combate ---
    [Tooltip("Sonido que se reproduce cuando este personaje realiza un ataque b�sico.")]
    public AudioClip basicAttackSound;
    [Tooltip("Sonido que se reproduce cuando este personaje recibe un golpe.")]
    public AudioClip takeHitSound;
    // --- FIN NUEVO ---




    [Header("Experiencia y Progresi�n")]
    [Tooltip("Puntos de experiencia actuales del personaje.")]
    public int currentXP = 0;
    [Tooltip("Puntos de experiencia necesarios para alcanzar el siguiente nivel.")]
    public int experienceToNextLevel = 100;

    [Header("Habilidades del Personaje")]
    [Tooltip("Lista de las habilidades que este personaje conoce y puede usar en combate.")]
    public List<AbilityData> knownAbilities = new List<AbilityData>();

    [Header("Stats Base del Personaje")]
    public int baseMaxHP = 100;
    public int baseMaxMP = 50;
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseMagicAttack = 8;
    public int baseMagicDefense = 4;
    public int baseSpeed = 10;
    // --- NUEVO: Stats de crecimiento por nivel (ejemplos) ---
    [Header("Crecimiento de Stats por Nivel")]
    public int hpGrowth = 20;
    public int mpGrowth = 5;
    public int attackGrowth = 2;
    public int defenseGrowth = 1;
    // (A�ade m�s para los otros stats si quieres que crezcan)


    // Stats actuales
    public int currentHP;
    public int currentMP;

    [Header("Equipo Inicial de Prueba (Opcional)")]
    [SerializeField] private ItemData initialHeadEquipment;
    [SerializeField] private ItemData initialMainHandEquipment;
    [SerializeField] private ItemData initialBodyEquipment;
    [SerializeField] private ItemData initialFeetEquipment;

    public Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();

    // Propiedades para los stats totales (que consideran el equipo)
    public int MaxHP => GetStatValueWithEquipment(baseMaxHP, item => item.maxHpBonus);
    public int MaxMP => GetStatValueWithEquipment(baseMaxMP, item => item.maxMpBonus);
    public int Attack => GetStatValueWithEquipment(baseAttack, item => item.attackBonus);
    public int Defense => GetStatValueWithEquipment(baseDefense, item => item.defenseBonus);
    public int MagicAttack => GetStatValueWithEquipment(baseMagicAttack, item => item.magicAttackBonus);
    public int MagicDefense => GetStatValueWithEquipment(baseMagicDefense, item => item.magicDefenseBonus);
    public int Speed => GetStatValueWithEquipment(baseSpeed, item => item.speedBonus);

    // Evento para notificar cuando el personaje sube de nivel (�til para la UI)
    public static event System.Action<Character> OnCharacterLevelUp;


    void Awake()
    {
        // --- L�GICA DE PERSISTENCIA CORREGIDA ---
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogError($"El personaje '{gameObject.name}' no tiene un 'characterID' asignado. La persistencia no funcionar�.", this);
            return;
        }

        // Comprobar si ya existe un personaje con este ID en la lista de persistentes.
        if (persistentCharacterIDs.Contains(characterID))
        {
            // Si ya existe, este es un duplicado. Lo destruimos y detenemos la ejecuci�n.
            Debug.Log($"Se encontr� un duplicado del personaje con ID '{characterID}'. Destruyendo el nuevo.");
            Destroy(gameObject);
            return;
        }

        // Si es el primero, lo registramos y lo hacemos persistente.
        persistentCharacterIDs.Add(characterID);
        transform.SetParent(null); // Lo movemos a la ra�z para que DontDestroyOnLoad funcione.
        DontDestroyOnLoad(gameObject);
        Debug.Log($"Personaje con ID '{characterID}' creado y marcado como persistente.");
        // --- FIN L�GICA CORREGIDA ---
        // DontDestroyOnLoad(gameObject);
        InitializeEquipmentSlots();

        if (initialHeadEquipment != null) EquipItemInitially(initialHeadEquipment);
        if (initialMainHandEquipment != null) EquipItemInitially(initialMainHandEquipment);
        if (initialBodyEquipment != null) EquipItemInitially(initialBodyEquipment);
        if (initialFeetEquipment != null) EquipItemInitially(initialFeetEquipment);

        // Asegurarse de que la XP al siguiente nivel sea la correcta para el nivel inicial
        experienceToNextLevel = CalculateNextLevelXP(level);

        currentHP = MaxHP;
        currentMP = MaxMP;
    }
    private void OnApplicationQuit()
    {
        persistentCharacterIDs.Clear();
    }

    private void InitializeEquipmentSlots()
    {
        if (equippedItems == null)
        {
            equippedItems = new Dictionary<EquipmentSlot, ItemData>();
        }
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot != EquipmentSlot.None && !equippedItems.ContainsKey(slot))
            {
                equippedItems[slot] = null;
            }
        }
    }

    private void EquipItemInitially(ItemData itemToEquip)
    {
        if (itemToEquip != null && itemToEquip.isEquipable && itemToEquip.equipmentSlot != EquipmentSlot.None)
        {
            equippedItems[itemToEquip.equipmentSlot] = itemToEquip;
        }
    }

    private int GetStatValueWithEquipment(int baseValue, System.Func<ItemData, int> statSelector)
    {
        int totalBonus = 0;
        if (equippedItems != null)
        {
            foreach (ItemData item in equippedItems.Values)
            {
                if (item != null)
                {
                    totalBonus += statSelector(item);
                }
            }
        }
        return baseValue + totalBonus;
    }

    public bool EquipItem(ItemData itemToEquip, PlayerInventory inventory)
    {
        if (itemToEquip == null || !itemToEquip.isEquipable || itemToEquip.equipmentSlot == EquipmentSlot.None) return false;
        EquipmentSlot slotToEquipIn = itemToEquip.equipmentSlot;
        ItemData previouslyEquippedItem = null;
        if (equippedItems.TryGetValue(slotToEquipIn, out previouslyEquippedItem) && previouslyEquippedItem != null)
        {
            previouslyEquippedItem.OnUnequip(this);
            if (inventory != null) inventory.AddItem(previouslyEquippedItem, 1);
        }
        equippedItems[slotToEquipIn] = itemToEquip;
        itemToEquip.OnEquip(this);
        RecalculateCurrentHPMPAfterEquipmentChange();
        return true;
    }

    public ItemData UnequipItem(EquipmentSlot slotToUnequip, PlayerInventory inventory)
    {
        if (slotToUnequip == EquipmentSlot.None) return null;
        ItemData unequippedItem = null;
        if (equippedItems.TryGetValue(slotToUnequip, out unequippedItem) && unequippedItem != null)
        {
            unequippedItem.OnUnequip(this);
            equippedItems[slotToUnequip] = null;
            if (inventory != null) inventory.AddItem(unequippedItem, 1);
            RecalculateCurrentHPMPAfterEquipmentChange();
            return unequippedItem;
        }
        return null;
    }

    private void RecalculateCurrentHPMPAfterEquipmentChange()
    {
        if (currentHP > MaxHP) currentHP = MaxHP;
        if (currentMP > MaxMP) currentMP = MaxMP;
    }

    public bool Heal(int amount)
    {
        if (amount <= 0) return false;
        if (currentHP >= MaxHP) return false;
        currentHP += amount;
        if (currentHP > MaxHP) currentHP = MaxHP;
        return true;
    }

    public bool RestoreMana(int amount)
    {
        if (amount <= 0) return false;
        if (currentMP >= MaxMP) return false;
        currentMP += amount;
        if (currentMP > MaxMP) currentMP = MaxMP;
        return true;
    }

    // M�todo de ejemplo para recibir da�o (para poder bajar el HP para las pruebas)
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
        // Debug.Log(characterName + " recibi� " + amount + " de da�o. HP actual: " + currentHP + "/" + MaxHP); // Para depuraci�n
        if (currentHP == 0)
        {
            Debug.Log(characterName + " ha sido derrotado.");
            // L�gica de muerte aqu�
        }
    }

    // Gasta una cantidad de MP del personaje.
    public bool SpendMana(int cost)
    {
        if (cost < 0) return false;
        if (currentMP >= cost)
        {
            currentMP -= cost;
            return true;
        }
        return false;
    }

    // --- L�GICA DE EXPERIENCIA Y SUBIDA DE NIVEL ---
    /// <summary>
    /// A�ade la cantidad especificada de XP al personaje y comprueba si sube de nivel.
    /// </summary>
    public void GainXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;
        Debug.Log(characterName + " gan� " + amount + " XP. XP actual: " + currentXP + "/" + experienceToNextLevel);

        // Comprobar si se sube de nivel (puede haber m�ltiples subidas si se gana mucha XP)
        while (currentXP >= experienceToNextLevel)
        {
            LevelUp();
        }
    }

    /// <summary>
    /// Procesa la subida de nivel del personaje.
    /// </summary>
    private void LevelUp()
    {
        level++;
        currentXP -= experienceToNextLevel; // Restar la XP usada para este nivel
        // Si currentXP queda negativo, significa que sobr� XP del nivel anterior, pero para simplificar, lo dejamos as� o lo ajustamos a 0.
        // Una l�gica m�s precisa ser�a: currentXP = currentXP - experienceToNextLevel;
        if (currentXP < 0) currentXP = 0; // Evitar XP negativa

        experienceToNextLevel = CalculateNextLevelXP(level); // Calcular XP para el siguiente nuevo nivel

        // Incrementar stats base (ejemplos)
        baseMaxHP += hpGrowth;
        baseMaxMP += mpGrowth;
        baseAttack += attackGrowth;
        baseDefense += defenseGrowth;
        // A�ade aqu� el crecimiento para otros stats base que tengas

        // Restaurar HP y MP al nuevo m�ximo (com�n en muchos RPGs)
        currentHP = MaxHP;
        currentMP = MaxMP;

        Debug.Log(characterName + " subi� al Nivel " + level + "! Stats aumentados. Pr�ximo nivel a los " + experienceToNextLevel + " XP.");

        // Disparar evento para que la UI u otros sistemas se actualicen
        OnCharacterLevelUp?.Invoke(this);
    }

    /// <summary>
    /// Calcula la cantidad de XP necesaria para el siguiente nivel.
    /// Esta es una f�rmula de ejemplo, puedes ajustarla.
    /// </summary>
    /// <param name="newLevel">El nivel para el cual se calcula la XP necesaria.</param>
    /// <returns>XP necesaria para alcanzar el 'newLevel + 1'.</returns>
    private int CalculateNextLevelXP(int currentCharacterLevel)
    {
        // F�rmula de ejemplo: (nivel_actual^1.5) * 100
        // Para nivel 1 -> 100 XP para nivel 2
        // Para nivel 2 -> (2^1.5)*100 ~= 282 XP para nivel 3
        // Para nivel 3 -> (3^1.5)*100 ~= 519 XP para nivel 4
        if (currentCharacterLevel <= 0) currentCharacterLevel = 1; // Evitar errores con nivel 0 o negativo
        return Mathf.FloorToInt(Mathf.Pow(currentCharacterLevel, 1.5f) * 100f);
    }

}

// } // Fin del namespace (si lo usas)
