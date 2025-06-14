using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TopDown; // Asegúrate de que este namespace es correcto si Character está ahí

/// <summary>
/// Gestiona las transiciones entre escenas con un efecto de fundido (fade).
/// Este script debe estar en un GameObject persistente.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Configuración de la Transición")]
    [Tooltip("Arrastra aquí el GameObject del FadePanel que tiene el CanvasGroup.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;
    private Vector2 nextPlayerSpawnPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Validar que la referencia al panel de fundido está asignada
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("SceneTransitionManager: ¡El 'Fade Canvas Group' no está asignado en el Inspector!");
        }
    }

    /// <summary>
    /// Inicia el proceso de transición a una nueva escena.
    /// </summary>
    public void LoadScene(string sceneName, Vector2 spawnPosition)
    {
        if (!isTransitioning)
        {
            nextPlayerSpawnPosition = spawnPosition;
            StartCoroutine(TransitionCoroutine(sceneName));
        }
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;

        // Desactivar el control del jugador durante la transición
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null) playerMovement.SetCanMove(false);

        // 1. Fade Out (fundido a negro)
        yield return StartCoroutine(Fade(1f));

        // 2. Cargar la nueva escena
        AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!sceneLoadOperation.isDone)
        {
            yield return null; // Esperar a que la nueva escena se cargue completamente
        }

        // 3. Mover al jugador a la posición de spawn
        // La referencia a playerMovement puede haberse perdido, la buscamos de nuevo.
        playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.transform.position = nextPlayerSpawnPosition;
            Debug.Log($"Jugador movido a la posición de spawn: {nextPlayerSpawnPosition}");
        }
        else
        {
            Debug.LogError("SceneTransitionManager: No se pudo encontrar al jugador en la nueva escena para posicionarlo.");
        }

        // Reactivar el control del jugador ANTES del fundido de entrada
        if (playerMovement != null) playerMovement.SetCanMove(true);

        // 4. Fade In (fundido para mostrar la nueva escena)
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("Fade Canvas Group no está asignado en SceneTransitionManager. No se puede mostrar el efecto de fundido.");
            yield break;
        }

        fadeCanvasGroup.gameObject.SetActive(true); // Asegurarse de que el panel esté activo
        fadeCanvasGroup.blocksRaycasts = true; // Bloquear input durante el fundido
        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        // Si el fundido ha terminado (es transparente), podemos desactivar el panel
        if (targetAlpha == 0)
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }
}
