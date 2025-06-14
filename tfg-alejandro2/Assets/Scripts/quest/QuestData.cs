using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Define el estado actual de una misi�n para el jugador.
/// </summary>
public enum QuestStatus
{
    Inactive,   // La misi�n no ha sido aceptada todav�a.
    Active,     // La misi�n est� en curso.
    Completed,  // Los objetivos se han cumplido, pero no se ha entregado.
    Claimed     // La misi�n ha sido entregada y las recompensas recibidas.
}

/// <summary>
/// Define el tipo de objetivo para una misi�n.
/// </summary>
public enum GoalType
{
    Kill,   // Objetivo de matar a un n�mero de enemigos de un tipo espec�fico.
    Collect // Objetivo de recolectar un n�mero de objetos de un tipo espec�fico.
    // (Se pueden a�adir m�s tipos, como: Talk, Reach, Escort...)
}

/// <summary>
/// Clase serializable que define un �nico objetivo dentro de una misi�n.
/// Al ser [System.Serializable], podremos editarla en el Inspector de QuestData.
/// </summary>
[System.Serializable]
public class QuestGoal
{
    [Tooltip("El tipo de este objetivo (Matar, Recolectar, etc.).")]
    public GoalType goalType;

    [Tooltip("El ID del objetivo (ej: el 'enemyID' del enemigo a matar, o el 'itemID' del objeto a recolectar).")]
    public string requiredID;

    [Tooltip("La cantidad necesaria para completar este objetivo.")]
    public int requiredAmount;

    // El progreso actual se manejar� en el QuestManager, no aqu�,
    // ya que este es solo el ScriptableObject de datos.
}

/// <summary>
/// ScriptableObject que sirve como plantilla para cada misi�n del juego.
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "TuJuego/Crear Misi�n (Quest)")]
public class QuestData : ScriptableObject
{
    [Header("Informaci�n General")]
    [Tooltip("ID �nico de la misi�n (ej: 'main_quest_01', 'kill_slimes_01').")]
    public string questID;
    [Tooltip("T�tulo de la misi�n que ver� el jugador.")]
    public string title;
    [Tooltip("Descripci�n detallada de la misi�n para el diario de misiones.")]
    [TextArea(4, 8)]
    public string description;

    [Header("Objetivos de la Misi�n")]
    [Tooltip("Lista de todos los objetivos que se deben cumplir para completar la misi�n.")]
    public List<QuestGoal> goals = new List<QuestGoal>();

    [Header("Recompensas")]
    [Tooltip("Cantidad de Puntos de Experiencia (XP) que se obtienen al completar la misi�n.")]
    public int experienceReward;
    // public int goldReward; // Podr�as a�adir oro
    [Tooltip("Lista de objetos que se obtienen como recompensa.")]
    public List<ItemData> itemRewards = new List<ItemData>();
    // (Para las recompensas de objetos, necesitar�s una forma de especificar la cantidad de cada uno si es variable)

    // El estado actual de la misi�n (Active, Completed, etc.) lo gestionar� el QuestManager,
    // ya que es espec�fico del progreso del jugador, no un dato de la misi�n en s�.
}
