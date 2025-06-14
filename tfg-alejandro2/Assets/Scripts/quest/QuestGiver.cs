using UnityEngine;
using Ink.Runtime; // Necesario para interactuar con la historia de Ink

// Aseg�rate de que los scripts QuestData y NPCDialogue est�n accesibles
// using TopDown; // Si est�n en este namespace

[RequireComponent(typeof(NPCDialogue))] // Asegura que este GameObject tambi�n tenga NPCDialogue.cs
public class QuestGiver : MonoBehaviour
{
    [Header("Misi�n")]
    [Tooltip("La misi�n que este NPC ofrece. Arrastra aqu� el asset QuestData correspondiente.")]
    [SerializeField] private QuestData questToGive;

    [Header("Di�logos de Ink")]
    [Tooltip("Di�logo que se muestra cuando la misi�n est� disponible para ser aceptada.")]
    [SerializeField] private TextAsset dialogueQuestAvailable;
    [Tooltip("Di�logo que se muestra si el jugador habla con el NPC mientras la misi�n est� activa.")]
    [SerializeField] private TextAsset dialogueQuestActive;
    [Tooltip("Di�logo que se muestra cuando el jugador ha cumplido los objetivos y vuelve a hablar con el NPC.")]
    [SerializeField] private TextAsset dialogueQuestCompleted;
    [Tooltip("Di�logo que se muestra despu�s de que la misi�n ha sido entregada y las recompensas reclamadas.")]
    [SerializeField] private TextAsset dialogueQuestClaimed;

    // Referencia al gestor de di�logo en el mismo GameObject
    private NPCDialogue npcDialogue;

    void Awake()
    {
        // Obtener la referencia al componente NPCDialogue
        npcDialogue = GetComponent<NPCDialogue>();
        if (npcDialogue == null)
        {
            Debug.LogError($"QuestGiver en '{gameObject.name}' necesita un componente NPCDialogue en el mismo GameObject para funcionar.", this);
            this.enabled = false; // Deshabilitar si falta el componente de di�logo
        }
    }

    /// <summary>
    /// Este es el m�todo principal que se llamar� cuando el jugador interact�e con este NPC.
    /// Decide qu� di�logo mostrar bas�ndose en el estado actual de la misi�n.
    /// </summary>
    public void StartQuestDialogue()
    {
        if (questToGive == null || npcDialogue == null)
        {
            Debug.LogWarning($"QuestGiver en '{gameObject.name}' no tiene una misi�n asignada o falta NPCDialogue.");
            // Si no hay misi�n, podr�a iniciar un di�logo por defecto
            // npcDialogue.StartDefaultDialogue(); // Si tuvieras un m�todo as� en NPCDialogue
            return;
        }

        // Obtener el estado actual de la misi�n desde el QuestManager
        QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(questToGive);

        // Mostrar el di�logo apropiado
        switch (currentStatus)
        {
            case QuestStatus.Inactive:
                npcDialogue.StartDialogue(dialogueQuestAvailable);
                // Despu�s de que este di�logo termine, necesitaremos aceptar la misi�n
                // Esto se har� llamando a AcceptThisQuest() desde una funci�n externa de Ink.
                break;
            case QuestStatus.Active:
                npcDialogue.StartDialogue(dialogueQuestActive);
                break;
            case QuestStatus.Completed:
                npcDialogue.StartDialogue(dialogueQuestCompleted);
                // Despu�s de que este di�logo termine, necesitaremos reclamar las recompensas
                // llamando a ClaimRewardsForThisQuest() desde Ink.
                break;
            case QuestStatus.Claimed:
                npcDialogue.StartDialogue(dialogueQuestClaimed);
                break;
        }
    }

    /// <summary>
    /// M�todo p�blico que ser� llamado por Ink para aceptar la misi�n.
    /// </summary>
    public void AcceptThisQuest()
    {
        if (QuestManager.Instance != null && questToGive != null)
        {
            QuestManager.Instance.AcceptQuest(questToGive);
        }
    }

    /// <summary>
    /// M�todo p�blico que ser� llamado por Ink para reclamar las recompensas de la misi�n.
    /// </summary>
    public void ClaimRewardsForThisQuest()
    {
        // Asumimos que el primer miembro de la party es quien recibe las recompensas
        // Necesitar�s una referencia a tu PartyManager o una forma de obtener el personaje jugador.
        if (QuestManager.Instance != null && questToGive != null && PartyManager.Instance != null)
        {
            Character playerCharacter = PartyManager.Instance.GetFirstPartyMember();
            if (playerCharacter != null)
            {
                QuestManager.Instance.ClaimQuestRewards(questToGive, playerCharacter);
            }
            else
            {
                Debug.LogError("No se pudo encontrar un personaje jugador para darle las recompensas de la misi�n.");
            }
        }
    }
}

