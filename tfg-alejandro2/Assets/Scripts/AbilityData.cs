using UnityEngine;
using System.Collections.Generic;
using TopDown; // Aseg�rate de que Character sea accesible

// Los enums de AbilityData s� est�n bien aqu�.

/// <summary>
/// Define los posibles tipos de objetivos para una habilidad.
/// </summary>
public enum AbilityTargetType
{
    None,           // No requiere un objetivo espec�fico (ej: un buff personal)
    Self,           // Se aplica al propio lanzador
    SingleAlly,     // Un solo aliado
    AllAllies,      // Todos los aliados
    SingleEnemy,    // Un solo enemigo
    AllEnemies      // Todos los enemigos
}

/// <summary>
/// Define los posibles tipos o categor�as de una habilidad.
/// </summary>
public enum AbilityEffectType
{
    Damage,         // Causa da�o
    Heal,           // Restaura HP
    RestoreMP,      // Restaura MP
    Buff,           // Aplica un estado beneficioso (mejora de stats)
    Debuff,         // Aplica un estado perjudicial (empeoramiento de stats)
    StatusEffect,   // Aplica un estado alterado (veneno, par�lisis, etc.)
    CureStatus,     // Cura un estado alterado
    Special         // Otro tipo de efecto especial
}

/// <summary>
/// ScriptableObject para definir los datos base de cada habilidad o magia en el juego.
/// Crea assets de este tipo desde el men�: Assets > Create > TuJuego > Habilidades > AbilityData
/// </summary>
[CreateAssetMenu(fileName = "NewAbilityData", menuName = "TuJuego/Crear Habilidad")]
public class AbilityData : ScriptableObject
{
    [Header("Informaci�n General de la Habilidad")]
    [Tooltip("Identificador �nico para esta habilidad (ej: 'fireball_lvl1', 'minor_heal').")]
    public string abilityID;

    [Tooltip("Nombre de la habilidad tal como se mostrar� al jugador.")]
    public string abilityName = "Nueva Habilidad";

    [Tooltip("Icono de la habilidad para mostrar en men�s o en la UI de combate.")]
    public Sprite icon;

    [Tooltip("Descripci�n detallada de lo que hace la habilidad. Se mostrar� en la UI.")]
    [TextArea(3, 6)]
    public string description = "Descripci�n de la habilidad.";

    [Header("Costes y Requisitos")]
    [Tooltip("Coste de Puntos de Man� (MP) para usar esta habilidad. Poner 0 si no tiene coste de MP.")]
    public int mpCost = 0;

    [Header("Efectos y Objetivos")]
    [Tooltip("El tipo principal de efecto que produce esta habilidad (Da�o, Curaci�n, Buff, etc.).")]
    public AbilityEffectType effectType = AbilityEffectType.Damage;

    [Tooltip("A qui�n o qui�nes afecta esta habilidad.")]
    public AbilityTargetType targetType = AbilityTargetType.SingleEnemy;

    [Tooltip("Potencia base de la habilidad (ej: cantidad de da�o, cantidad de curaci�n, etc.).")]
    public float power = 10f;

    // --- CAMPOS PARA ANIMACI�N DE HABILIDAD ESPEC�FICA ---
    [Header("Animaci�n y Efectos de Combate")]
    [Tooltip("Nombre del par�metro Trigger en el Animator Controller del personaje que lanza esta habilidad. Si est� vac�o, se podr�a usar un trigger gen�rico como 'AttackTrigger'.")]
    public string animationTriggerName;

    [Tooltip("Duraci�n aproximada de la animaci�n de esta habilidad en segundos. Si es 0 o negativo, CombatManager usar� una duraci�n por defecto.")]
    public float animationDuration = 0.8f; // Valor por defecto, aj�stalo por habilidad

    [Tooltip("Referencia a un Prefab de efecto visual (VFX) que se instanciar� en el objetivo o en el lanzador. Si la habilidad lanza un proyectil, este ser�a el prefab del proyectil.")]
    public GameObject vfxPrefab;
    // --- MODIFICADO: Campo de sfxClip renombrado y a�adido sfxImpactClip ---
    [Tooltip("Sonido que se reproduce cuando se LANZA la habilidad.")]
    public AudioClip launchSound;
    [Tooltip("Sonido que se reproduce cuando la habilidad IMPACTA (si es proyectil) o en el objetivo (si es efecto directo).")]
    public AudioClip impactSound;
    // --- FIN MODIFICACI�N ---


    // El m�todo ExecuteEffect se mantiene como lo ten�as, ya que la l�gica de aplicar
    // el efecto real (da�o, curaci�n) y disparar la animaci�n se maneja en CombatManager.cs
    // para este prototipo. Si quisieras que cada AbilityData tuviera una l�gica de efecto
    // completamente �nica, este m�todo se expandir�a mucho o usar�as clases derivadas.
    public virtual void ExecuteEffect(Character caster, List<Character> targets)
    {
        if (caster == null)
        {
            Debug.LogWarning("ExecuteEffect: Caster es null para " + abilityName);
            return;
        }

        // La deducci�n de MP y la l�gica principal de efectos ahora est�n en CombatManager.ExecuteSkill.
        // Este m�todo podr�a usarse en el futuro para efectos muy espec�ficos de la habilidad
        // que no son cubiertos por la l�gica gen�rica del CombatManager.
        // Por ahora, solo un log para indicar que se llam�.
        Debug.Log($"AbilityData.ExecuteEffect llamada para {abilityName} por {caster.characterName}. El efecto principal es manejado por CombatManager.");
    }
}
