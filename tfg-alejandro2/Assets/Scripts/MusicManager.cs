using UnityEngine;
using System.Collections;

// Este script gestionará la música de fondo del juego.
// Debe estar en un GameObject persistente (ej: MusicManager_Handler).
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Pistas de Música")]
    [Tooltip("La música que sonará por defecto en el modo de exploración.")]
    [SerializeField] private AudioClip explorationMusic;
    [Tooltip("La música que sonará durante los combates.")]
    [SerializeField] private AudioClip combatMusic;

    [Header("Configuración de Fundido")]
    [Tooltip("Volumen máximo de la música (0.0 a 1.0).")]
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.5f;
    [Tooltip("Duración del fundido al cambiar de canción en segundos.")]
    [SerializeField] private float fadeDuration = 1.0f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Configurar el Singleton persistente
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Obtener y configurar el AudioSource
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; // Queremos que la música se repita
        audioSource.volume = 0f; // Empezar con el volumen a cero para hacer un fade in
    }

    void Start()
    {
        // Empezar a reproducir la música de exploración al inicio del juego
        PlayExplorationMusic();
    }

    /// <summary>
    /// Inicia la reproducción de la música de exploración.
    /// </summary>
    public void PlayExplorationMusic()
    {
        PlayMusic(explorationMusic);
    }

    /// <summary>
    /// Inicia la reproducción de la música de combate.
    /// </summary>
    public void PlayCombatMusic()
    {
        PlayMusic(combatMusic);
    }

    /// <summary>
    /// Lógica principal para cambiar de pista de música con un fundido suave.
    /// </summary>
    private void PlayMusic(AudioClip newClip)
    {
        if (newClip == null || (audioSource.clip == newClip && audioSource.isPlaying))
        {
            return; // No hacer nada si el clip es nulo o si ya se está reproduciendo
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeTrack(newClip));
    }

    private IEnumerator FadeTrack(AudioClip newClip)
    {
        // 1. Fade Out (Bajar volumen de la canción actual si hay una)
        if (audioSource.isPlaying)
        {
            yield return StartCoroutine(FadeVolume(0f));
            audioSource.Stop();
        }

        // 2. Cambiar la canción y hacer Fade In
        audioSource.clip = newClip;
        audioSource.Play();
        yield return StartCoroutine(FadeVolume(maxVolume));

        fadeCoroutine = null;
    }

    private IEnumerator FadeVolume(float targetVolume)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}
