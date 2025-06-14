using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class MainMenu : MonoBehaviour
{
    [Header("Configuraci�n de Escenas")]
    [Tooltip("El nombre exacto de la escena principal del juego que se cargar�.")]
    [SerializeField] private string mainGameSceneName = "MainGameScene"; // Cambia "MainGameScene" por el nombre de tu escena de juego

    /// <summary>
    /// Este m�todo se asigna al bot�n "Iniciar Juego".
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
            Debug.LogError("MainMenu: El nombre de la escena principal del juego no est� asignado en el Inspector.");
        }
    }

    /// <summary>
    /// Este m�todo se asigna al bot�n "Salir".
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

        // La siguiente l�nea cierra la aplicaci�n. No funciona en el Editor de Unity,
        // pero s� funcionar� cuando construyas (build) el juego.
        Application.Quit();

        // Esto es para que la salida tambi�n funcione en el Editor de Unity.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- Opcional: M�todos para un panel de opciones ---

    // public void OpenOptionsPanel()
    // {
    //     // Aqu� ir�a la l�gica para activar tu panel de opciones
    // }

    // public void CloseOptionsPanel()
    // {
    //     // Aqu� ir�a la l�gica para desactivar tu panel de opciones
    // }
}
