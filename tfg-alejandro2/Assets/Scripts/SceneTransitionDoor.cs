using UnityEngine;

// Aseg�rate de que este script est� en un GameObject que tenga un Collider2D
// y que est� en la capa que tu PlayerInteraction puede detectar.
public class SceneTransitionDoor : MonoBehaviour
{
    [Header("Configuraci�n de la Transici�n")]
    [Tooltip("El nombre exacto de la escena a la que esta puerta lleva. Debe estar a�adida en Build Settings.")]
    [SerializeField] private string sceneToLoad;

    [Tooltip("La posici�n (coordenadas X, Y) donde aparecer� el jugador en la nueva escena.")]
    [SerializeField] private Vector2 playerSpawnPosition;

    /// <summary>
    /// Este m�todo p�blico ser� llamado por el script de interacci�n del jugador.
    /// </summary>
    public void Transition()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError($"La puerta '{gameObject.name}' no tiene una 'Scene To Load' asignada.", this);
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log($"Transici�n iniciada: Cargando escena '{sceneToLoad}' y moviendo al jugador a {playerSpawnPosition}");
            SceneTransitionManager.Instance.LoadScene(sceneToLoad, playerSpawnPosition);
        }
        else
        {
            Debug.LogError("No se encontr� una instancia de SceneTransitionManager en la escena. Aseg�rate de que el GameObject que lo contiene est� en tu escena de inicio.");
        }
    }
}
