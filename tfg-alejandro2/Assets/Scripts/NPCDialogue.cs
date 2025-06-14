using UnityEngine;
using Ink.Runtime;

// Este script ahora act�a como el intermediario entre el di�logo y los scripts de comportamiento.
public class NPCDialogue : MonoBehaviour
{
    [Header("Configuraci�n de Di�logo")]
    [Tooltip("El archivo de di�logo (.ink.json) por defecto para este NPC si no tiene misiones.")]
    [SerializeField] private TextAsset inkJSON;

    // --- Referencias a los scripts de comportamiento ---
    private NPCMovement npcMovement;
    private DynamicIdleBehavior dynamicIdleBehavior;
    // No necesitamos una referencia a SetInitialAnimationState, ya que solo act�a una vez en Start.
    // --- NUEVO: Referencia al script de curaci�n ---
    private NPCHealer npcHealer;

    private Transform playerTransform;

    private void Awake()
    {
        // Obtener referencias a TODOS los posibles scripts de comportamiento en este NPC.
        // Si un script no existe en el NPC, su referencia ser� null y no dar� error.
        npcMovement = GetComponent<NPCMovement>();
        dynamicIdleBehavior = GetComponent<DynamicIdleBehavior>();
        npcHealer = GetComponent<NPCHealer>(); // Buscamos si este NPC es un sanador.

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    /// <summary>
    /// Inicia el di�logo por defecto. Llamado por PlayerInteraction si no hay QuestGiver.
    /// </summary>
    public void StartDefaultDialogue()
    {
        StartDialogue(this.inkJSON);
    }

    /// <summary>
    /// Inicia un di�logo espec�fico. Llamado por QuestGiver.
    /// </summary>
    public void StartDialogue(TextAsset dialogueToPlay)
    {
        if (dialogueToPlay == null || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying))
        {
            return;
        }

        // --- PAUSAR COMPORTAMIENTOS ---
        // Pausar el script de patrulla (si existe).
        if (npcMovement != null)
        {
            npcMovement.SetMovementPaused(true);
            npcMovement.SetIdleAndFaceTarget(playerTransform); // Le decimos que mire al jugador.
        }
        // Pausar el script de idle din�mico (si existe), usando su propio m�todo.
        if (dynamicIdleBehavior != null)
        {
            dynamicIdleBehavior.FocusOnTarget(playerTransform);
        }

        // Iniciar el di�logo a trav�s del DialogueManager.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EnterDialogueMode(dialogueToPlay, this, npcHealer);
        }
    }

    /// <summary>
    /// Este m�todo es llamado por DialogueManager cuando el di�logo termina.
    /// </summary>
    public void OnDialogueEnd()
    {
        // --- REANUDAR COMPORTAMIENTOS ---
        // Reanudar el script de patrulla (si existe).
        if (npcMovement != null)
        {
            npcMovement.SetMovementPaused(false);
        }
        // Reanudar el script de idle din�mico (si existe).
        if (dynamicIdleBehavior != null)
        {
            dynamicIdleBehavior.ResumeDynamicIdle();
        }
    }
}
