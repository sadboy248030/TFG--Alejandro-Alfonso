using UnityEngine;

// Este script desactiva su propio GameObject cuando se completa una misión específica.
public class QuestLockedBarrier : MonoBehaviour
{
    [Header("Configuración de la Barrera")]
    [Tooltip("La misión que debe completarse para que esta barrera desaparezca. Arrastra aquí el asset QuestData correspondiente.")]
    [SerializeField] private QuestData requiredQuest;

    void Start()
    {
        if (requiredQuest == null)
        {
            Debug.LogError($"QuestLockedBarrier en '{gameObject.name}' no tiene una misión asignada.", this);
            return;
        }

        // Suscribirse al evento de que una misión ha sido completada.
        // Esto es mucho más eficiente que comprobar el estado en cada frame.
        QuestManager.OnQuestCompleted += OnQuestStateChanged;
        QuestManager.OnQuestClaimed += OnQuestStateChanged; // También funciona si la entregas

        // Comprobar el estado inicial. Si la misión ya estuviera completada, desactivar la barrera.
        CheckQuestStatus();
    }

    private void OnDestroy()
    {
        // Muy importante: desuscribirse de los eventos para evitar errores de memoria.
        QuestManager.OnQuestCompleted -= OnQuestStateChanged;
        QuestManager.OnQuestClaimed -= OnQuestStateChanged;
    }

    /// <summary>
    /// Este método se llama automáticamente cuando cualquier misión cambia de estado a 'Completed' o 'Claimed'.
    /// </summary>
    private void OnQuestStateChanged(QuestData questData)
    {
        // Comprobamos si la misión que ha cambiado es la que nos interesa.
        if (questData == requiredQuest)
        {
            Debug.Log($"La misión '{requiredQuest.title}' ha sido completada. Desactivando la barrera '{gameObject.name}'.");
            gameObject.SetActive(false); // ¡La barrera desaparece!
        }
    }

    /// <summary>
    /// Comprueba el estado actual de la misión y desactiva la barrera si ya está completada.
    /// </summary>
    private void CheckQuestStatus()
    {
        if (QuestManager.Instance != null)
        {
            QuestStatus status = QuestManager.Instance.GetQuestStatus(requiredQuest);
            if (status == QuestStatus.Completed || status == QuestStatus.Claimed)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
