using UnityEngine;

// Este script desactiva su propio GameObject cuando se completa una misi�n espec�fica.
public class QuestLockedBarrier : MonoBehaviour
{
    [Header("Configuraci�n de la Barrera")]
    [Tooltip("La misi�n que debe completarse para que esta barrera desaparezca. Arrastra aqu� el asset QuestData correspondiente.")]
    [SerializeField] private QuestData requiredQuest;

    void Start()
    {
        if (requiredQuest == null)
        {
            Debug.LogError($"QuestLockedBarrier en '{gameObject.name}' no tiene una misi�n asignada.", this);
            return;
        }

        // Suscribirse al evento de que una misi�n ha sido completada.
        // Esto es mucho m�s eficiente que comprobar el estado en cada frame.
        QuestManager.OnQuestCompleted += OnQuestStateChanged;
        QuestManager.OnQuestClaimed += OnQuestStateChanged; // Tambi�n funciona si la entregas

        // Comprobar el estado inicial. Si la misi�n ya estuviera completada, desactivar la barrera.
        CheckQuestStatus();
    }

    private void OnDestroy()
    {
        // Muy importante: desuscribirse de los eventos para evitar errores de memoria.
        QuestManager.OnQuestCompleted -= OnQuestStateChanged;
        QuestManager.OnQuestClaimed -= OnQuestStateChanged;
    }

    /// <summary>
    /// Este m�todo se llama autom�ticamente cuando cualquier misi�n cambia de estado a 'Completed' o 'Claimed'.
    /// </summary>
    private void OnQuestStateChanged(QuestData questData)
    {
        // Comprobamos si la misi�n que ha cambiado es la que nos interesa.
        if (questData == requiredQuest)
        {
            Debug.Log($"La misi�n '{requiredQuest.title}' ha sido completada. Desactivando la barrera '{gameObject.name}'.");
            gameObject.SetActive(false); // �La barrera desaparece!
        }
    }

    /// <summary>
    /// Comprueba el estado actual de la misi�n y desactiva la barrera si ya est� completada.
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
