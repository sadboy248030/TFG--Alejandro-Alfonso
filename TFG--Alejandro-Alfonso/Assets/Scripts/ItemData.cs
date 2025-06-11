using UnityEngine;
using TopDown; // Asumiendo que Character.cs est� en este namespace

/// <summary>
/// Define los diferentes tipos de objetos que pueden existir en el juego.
/// </summary>
public enum ItemType
{
    Weapon,     // Armas que se pueden equipar.
    Armor,      // Piezas de armadura que se pueden equipar (Casco, Pechera, Botas).
    Consumable, // Objetos que se usan y generalmente se gastan (pociones, comida, etc.).
    Quest       // Objetos espec�ficos de misi�n, a menudo necesarios para progresar en la historia.
    // Key y Material eliminados seg�n tu definici�n
}

/// <summary>
/// Define las ranuras de equipamiento donde un objeto puede ser equipado por un personaje.
/// </summary>
public enum EquipmentSlot
{
    None,       // El objeto no es equipable o no ocupa un slot espec�fico.
    MainHand,   // Para el arma principal del personaje.
    Head,       // Para cascos, sombreros, etc.
    Body,       // Para armaduras de cuerpo, pecheras, ropas.
    Feet        // Para botas, calzado.
    // Accessory1 y Accessory2 eliminados seg�n tu definici�n
}

/// <summary>
/// ScriptableObject para definir los datos base de cada tipo de objeto en el juego.
/// Puedes crear assets de este tipo desde el men� de Unity: Assets > Create > TuJuego > Inventory > ItemData
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "TuJuego/Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Informaci�n General del Objeto")]
    [Tooltip("Identificador �nico para este objeto (ej: 'wpn_iron_sword', 'quest_ancient_seal'). �til para referencias internas, guardado/carga.")]
    public string itemID;

    [Tooltip("Nombre del objeto tal como se mostrar� al jugador en la UI.")]
    public string itemName = "Nuevo Objeto";

    [Tooltip("Icono del objeto para el inventario y la UI. Idealmente 16x16 p�xeles para mantener el estilo.")]
    public Sprite icon;

    [Tooltip("Descripci�n del objeto que se mostrar� al jugador (puede ser multil�nea para m�s detalle).")]
    [TextArea(3, 6)]
    public string description = "Descripci�n del objeto.";

    [Tooltip("El tipo de objeto (Arma, Armadura, Consumible, Quest). Determina su uso principal y c�mo interact�a con otros sistemas.")]
    public ItemType itemType = ItemType.Consumable;

    [Tooltip("�Puede este objeto apilarse en un solo slot del inventario (ej: pociones)? Si no, cada uno ocupar� un slot.")]
    public bool isStackable = false;

    [Tooltip("Si es apilable, �cu�ntos caben como m�ximo en un solo slot? Si no es apilable, este valor se ignora (o se considera 1).")]
    public int maxStackSize = 1;

    [Header("Detalles de Equipamiento")]
    [Tooltip("�Es este objeto equipable por un personaje? (Marcado para Armas y Armaduras).")]
    public bool isEquipable = false;

    [Tooltip("Si es equipable, �en qu� ranura se equipa? (Debe coincidir con los valores del enum EquipmentSlot).")]
    public EquipmentSlot equipmentSlot = EquipmentSlot.None;

    // --- Stats que el objeto otorga al ser equipado ---
    [Space(10)]
    [Tooltip("Bonificaci�n al ataque f�sico que proporciona este objeto.")]
    public int attackBonus = 0;
    [Tooltip("Bonificaci�n a la defensa f�sica que proporciona este objeto.")]
    public int defenseBonus = 0;
    [Tooltip("Bonificaci�n al ataque m�gico que proporciona este objeto.")]
    public int magicAttackBonus = 0;
    [Tooltip("Bonificaci�n a la defensa m�gica que proporciona este objeto.")]
    public int magicDefenseBonus = 0;
    [Tooltip("Bonificaci�n a la velocidad del personaje.")]
    public int speedBonus = 0;
    [Tooltip("Bonificaci�n a los Puntos de Vida (HP) m�ximos del personaje.")]
    public int maxHpBonus = 0;
    [Tooltip("Bonificaci�n a los Puntos de Man� (MP) m�ximos del personaje.")]
    public int maxMpBonus = 0;

    [Header("Detalles de Consumible")]
    [Tooltip("�Es este objeto un consumible (se gasta/desaparece despu�s de usarse)?")]
    public bool isConsumable = false;

    [Space(10)]
    [Tooltip("Cantidad de Puntos de Vida (HP) que este objeto restaura al usarse.")]
    public int hpToRestore = 0;
    [Tooltip("Cantidad de Puntos de Man� (MP) que este objeto restaura al usarse.")]
    public int mpToRestore = 0;

    public virtual bool Use(Character targetCharacter)
    {
        if (!isConsumable)
        {
            Debug.LogWarning($"ItemData: '{itemName}' no es consumible y se intent� usar.");
            return false;
        }

        if (targetCharacter == null && (hpToRestore > 0 || mpToRestore > 0))
        {
            Debug.LogWarning($"ItemData: '{itemName}' requiere un objetivo para restaurar HP/MP, pero targetCharacter es null.");
            return false;
        }

        bool effectApplied = false;

        if (hpToRestore > 0 && targetCharacter != null)
        {
            if (targetCharacter.Heal(hpToRestore))
            {
                Debug.Log($"{targetCharacter.characterName} us� {itemName}, restaur� {hpToRestore} HP. Ahora tiene {targetCharacter.currentHP}/{targetCharacter.MaxHP} HP.");
                effectApplied = true;
            }
            else
            {
                Debug.Log($"{itemName} no se us� en {targetCharacter.characterName} para HP (ya estaba al m�ximo o no se pudo curar).");
            }
        }

        if (mpToRestore > 0 && targetCharacter != null)
        {
            if (targetCharacter.RestoreMana(mpToRestore))
            {
                Debug.Log($"{targetCharacter.characterName} us� {itemName}, restaur� {mpToRestore} MP. Ahora tiene {targetCharacter.currentMP}/{targetCharacter.MaxMP} MP.");
                effectApplied = true;
            }
            else
            {
                Debug.Log($"{itemName} no se us� en {targetCharacter.characterName} para MP (ya estaba al m�ximo o no se pudo restaurar).");
            }
        }

        if (effectApplied)
        {
            Debug.Log($"ItemData: '{itemName}' se us� con �xito y tuvo efecto. Deber�a consumirse.");
            return true;
        }
        else
        {
            Debug.Log($"ItemData: '{itemName}' se intent� usar, pero no tuvo ning�n efecto aplicable. No deber�a consumirse.");
            return false;
        }
    }

    public virtual void OnEquip(Character characterEquipping)
    {
        if (isEquipable)
        {
            // Debug.Log((characterEquipping != null ? characterEquipping.characterName : "Alguien") + " equip� " + itemName);
        }
    }

    public virtual void OnUnequip(Character characterUnequipping)
    {
        if (isEquipable)
        {
            // Debug.Log((characterUnequipping != null ? characterUnequipping.characterName : "Alguien") + " desequip� " + itemName);
        }
    }
}
