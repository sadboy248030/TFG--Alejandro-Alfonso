using UnityEngine;

// Este script se encarga de configurar el estado inicial del juego, como asignar la primera misión.
public class GameStartManager : MonoBehaviour
{
    [Header("Configuración de la Misión Inicial")]
    [Tooltip("La primera misión que se le dará al jugador al empezar una nueva partida. Arrastra aquí el asset QuestData correspondiente.")]
    [SerializeField] private QuestData firstQuest;

    // Usamos una variable estática para asegurarnos de que esto solo se ejecute una vez por sesión de juego.
    private static bool hasStarted = false;

    void Start()
    {
        // Si ya se ha ejecutado, o si no hay misión asignada, no hacemos nada.
        if (hasStarted || firstQuest == null)
        {
            Destroy(gameObject); // Destruimos este objeto para que no se ejecute de nuevo.
            return;
        }

        // Comprobamos si la misión ya está en el diario (por si se carga una partida guardada).
        if (QuestManager.Instance != null && QuestManager.Instance.GetQuestStatus(firstQuest) == QuestStatus.Inactive)
        {
            Debug.Log($"Iniciando la primera misión del juego: '{firstQuest.title}'");
            QuestManager.Instance.AcceptQuest(firstQuest);
        }

        // Marcamos que ya se ha ejecutado y hacemos que este objeto no se destruya (por si acaso).
        hasStarted = true;
        DontDestroyOnLoad(gameObject);
    }
}
