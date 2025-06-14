using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class MainMenu : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("El nombre exacto de la escena principal del juego que se cargará.")]
    [SerializeField] private string mainGameSceneName = "MainGameScene"; // Cambia "MainGameScene" por el nombre de tu escena de juego

    /// <summary>
    /// Este método se asigna al botón "Iniciar Juego".
    /// </summary>
    public void StartGame()
    {
        // Carga la escena principal del juego
        if (!string.IsNullOrEmpty(mainGameSceneName))
        {
            Debug.Log($"Cargando escena: {mainGameSceneName}");
            SceneManager.LoadScene(mainGameSceneName);
        }
        else
        {
            Debug.LogError("MainMenu: El nombre de la escena principal del juego no está asignado en el Inspector.");
        }
    }

    /// <summary>
    /// Este método se asigna al botón "Salir".
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

        // La siguiente línea cierra la aplicación. No funciona en el Editor de Unity,
        // pero sí funcionará cuando construyas (build) el juego.
        Application.Quit();

        // Esto es para que la salida también funcione en el Editor de Unity.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- Opcional: Métodos para un panel de opciones ---

    // public void OpenOptionsPanel()
    // {
    //     // Aquí iría la lógica para activar tu panel de opciones
    // }

    // public void CloseOptionsPanel()
    // {
    //     // Aquí iría la lógica para desactivar tu panel de opciones
    // }
}
