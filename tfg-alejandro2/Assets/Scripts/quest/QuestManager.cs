using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para .FirstOrDefault() y .All()
using TopDown; // Para tener acceso a Character y EnemyData

public class QuestManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static QuestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Para que el gestor persista entre escenas
            InitializeQuestManager();
        }
    }
    // --- Fin Singleton Pattern ---

    // Diccionario para almacenar el estado y progreso de cada misi�n para el jugador.
    // La clave es el ScriptableObject QuestData de la misi�n.
    // El valor es una clase que guarda el estado y el progreso de los objetivos.
    private Dictionary<QuestData, QuestStatusInfo> questLog = new Dictionary<QuestData, QuestStatusInfo>();

    // Eventos para notificar a la UI u otros sistemas sobre cambios en las misiones
    public static event System.Action<QuestData> OnQuestAccepted;
    public static event System.Action<QuestData> OnQuestObjectiveUpdated;
    public static event System.Action<QuestData> OnQuestCompleted;
    public static event System.Action<QuestData> OnQuestClaimed;


    private void InitializeQuestManager()
    {
        // En un juego real, aqu� se cargar�an los datos de misiones guardados del jugador.
        // Por ahora, simplemente inicializamos el diccionario vac�o.
        questLog = new Dictionary<QuestData, QuestStatusInfo>();
        Debug.Log("QuestManager inicializado.");
    }

    /// <summary>
    /// Intenta aceptar una nueva misi�n.
    /// </summary>
    /// <param name="quest">La misi�n a aceptar.</param>
    /// <returns>True si la misi�n fue aceptada, False si ya estaba en el diario.</returns>
    public bool AcceptQuest(QuestData quest)
    {
        if (quest == null) return false;

        // Comprobar si la misi�n ya ha sido aceptada o completada antes.
        if (questLog.ContainsKey(quest))
        {
            Debug.LogWarning($"QuestManager: Se intent� aceptar la misi�n '{quest.title}', pero ya est� en el diario.");
            return false;
        }

        // A�adir la nueva misi�n al diario con estado 'Active'.
        QuestStatusInfo newQuestStatus = new QuestStatusInfo(quest);
        questLog.Add(quest, newQuestStatus);

        Debug.Log($"Misi�n Aceptada: '{quest.title}'");
        OnQuestAccepted?.Invoke(quest); // Notificar a los suscriptores (UI, etc.)

        // Comprobar inmediatamente si los objetivos ya est�n cumplidos (ej: si ya tienes los objetos)
        CheckQuestCompletion(quest);

        return true;
    }

    /// <summary>
    /// Obtiene el estado actual de una misi�n.
    /// </summary>
    public QuestStatus GetQuestStatus(QuestData quest)
    {
        if (questLog.TryGetValue(quest, out QuestStatusInfo statusInfo))
        {
            return statusInfo.status;
        }
        return QuestStatus.Inactive; // Si no est� en el diario, est� inactiva.
    }

    /// <summary>
    /// Notifica al QuestManager que un enemigo ha sido derrotado.
    /// </summary>
    public void NotifyEnemyKilled(string enemyID)
    {
        if (string.IsNullOrEmpty(enemyID)) return;

        // Iterar por todas las misiones ACTIVAS.
        foreach (var questPair in questLog.Where(q => q.Value.status == QuestStatus.Active))
        {
            bool questUpdated = false;
            // Iterar por los objetivos de esa misi�n
            for (int i = 0; i < questPair.Key.goals.Count; i++)
            {
                QuestGoal goal = questPair.Key.goals[i];
                if (goal.goalType == GoalType.Kill && goal.requiredID == enemyID)
                {
                    // Incrementar el progreso para este objetivo
                    if (questPair.Value.goalProgress[i] < goal.requiredAmount)
                    {
                        questPair.Value.goalProgress[i]++;
                        questUpdated = true;
                        Debug.Log($"Progreso de Misi�n '{questPair.Key.title}': Enemigo '{enemyID}' derrotado ({questPair.Value.goalProgress[i]}/{goal.requiredAmount}).");
                    }
                }
            }

            if (questUpdated)
            {
                OnQuestObjectiveUpdated?.Invoke(questPair.Key); // Notificar que un objetivo ha avanzado
                CheckQuestCompletion(questPair.Key); // Comprobar si la misi�n se ha completado con este progreso
            }
        }
    }

    /// <summary>
    /// Comprueba si todos los objetivos de una misi�n activa se han cumplido.
    /// Si es as�, cambia su estado a 'Completed'.
    /// </summary>
    private void CheckQuestCompletion(QuestData quest)
    {
        if (!questLog.ContainsKey(quest) || questLog[quest].status != QuestStatus.Active) return;

        bool allGoalsMet = true;
        for (int i = 0; i < quest.goals.Count; i++)
        {
            if (questLog[quest].goalProgress[i] < quest.goals[i].requiredAmount)
            {
                allGoalsMet = false;
                break; // Un objetivo no se cumple, no hace falta seguir comprobando
            }
        }

        if (allGoalsMet)
        {
            questLog[quest].status = QuestStatus.Completed;
            Debug.Log($"�Misi�n COMPLETADA: '{quest.title}'! (Pendiente de entrega)");
            OnQuestCompleted?.Invoke(quest); // Notificar a la UI, etc.
        }
    }


    /// <summary>
    /// El jugador entrega una misi�n completada y recibe las recompensas.
    /// </summary>
    public bool ClaimQuestRewards(QuestData quest, Character character)
    {
        if (!questLog.ContainsKey(quest) || questLog[quest].status != QuestStatus.Completed || character == null)
        {
            Debug.LogWarning($"No se pueden reclamar las recompensas de la misi�n '{quest.title}'. Estado actual: {GetQuestStatus(quest)}");
            return false;
        }

        // Dar recompensas
        Debug.Log($"Entregando misi�n '{quest.title}'. Recompensas recibidas:");
        if (quest.experienceReward > 0)
        {
            character.GainXP(quest.experienceReward);
            Debug.Log($"- XP: {quest.experienceReward}");
        }
        if (quest.itemRewards.Count > 0 && PlayerInventory.Instance != null)
        {
            foreach (ItemData itemReward in quest.itemRewards)
            {
                PlayerInventory.Instance.AddItem(itemReward, 1); // Asume cantidad 1 por ahora
                Debug.Log($"- Objeto: {itemReward.itemName}");
            }
        }

        // Marcar la misi�n como entregada
        questLog[quest].status = QuestStatus.Claimed;
        OnQuestClaimed?.Invoke(quest);
        return true;
    }
    public QuestData GetActiveQuest()
    {
        // Usamos LINQ para encontrar el primer par en el diccionario cuyo estado sea 'Active'.
        var activeQuestPair = questLog.FirstOrDefault(q => q.Value.status == QuestStatus.Active);
        // Devolvemos la clave (que es el QuestData) de ese par. Si no encuentra ninguna, devuelve null.
        return activeQuestPair.Key;
    }
    public int GetQuestProgress(QuestData quest, int goalIndex)
    {
        if (questLog.TryGetValue(quest, out QuestStatusInfo statusInfo))
        {
            if (goalIndex < statusInfo.goalProgress.Count)
            {
                return statusInfo.goalProgress[goalIndex];
            }
        }
        return 0; // Devuelve 0 si no se encuentra la misi�n o el objetivo.
    }
}


/// <summary>
/// Clase auxiliar para almacenar el estado y progreso de una misi�n en el diario del jugador.
/// </summary>
public class QuestStatusInfo
{
    public QuestStatus status;
    public List<int> goalProgress; // Una lista de enteros, uno por cada objetivo en QuestData.goals

    public QuestStatusInfo(QuestData questData)
    {
        status = QuestStatus.Active;
        goalProgress = new List<int>();
        // Inicializar el progreso de cada objetivo a 0
        foreach (var goal in questData.goals)
        {
            goalProgress.Add(0);
        }
    }
}
