using UnityEngine;

// Este script se encarga de configurar el estado inicial del juego, como asignar la primera misi�n.
public class GameStartManager : MonoBehaviour
{
    [Header("Configuraci�n de la Misi�n Inicial")]
    [Tooltip("La primera misi�n que se le dar� al jugador al empezar una nueva partida. Arrastra aqu� el asset QuestData correspondiente.")]
    [SerializeField] private QuestData firstQuest;

    // Usamos una variable est�tica para asegurarnos de que esto solo se ejecute una vez por sesi�n de juego.
    private static bool hasStarted = false;

    void Start()
    {
        // Si ya se ha ejecutado, o si no hay misi�n asignada, no hacemos nada.
        if (hasStarted || firstQuest == null)
        {
            Destroy(gameObject); // Destruimos este objeto para que no se ejecute de nuevo.
            return;
        }

        // Comprobamos si la misi�n ya est� en el diario (por si se carga una partida guardada).
        if (QuestManager.Instance != null && QuestManager.Instance.GetQuestStatus(firstQuest) == QuestStatus.Inactive)
        {
            Debug.Log($"Iniciando la primera misi�n del juego: '{firstQuest.title}'");
            QuestManager.Instance.AcceptQuest(firstQuest);
        }

        // Marcamos que ya se ha ejecutado y hacemos que este objeto no se destruya (por si acaso).
        hasStarted = true;
        DontDestroyOnLoad(gameObject);
    }
}
