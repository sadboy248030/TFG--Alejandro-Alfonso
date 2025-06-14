using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // Aseg�rate de tener TextMeshPro importado

/// <summary>
/// Gestiona una secuencia de introducci�n con texto que aparece y desaparece
/// y luego carga la siguiente escena.
/// </summary>
public class IntroManager : MonoBehaviour
{
    [Header("Configuraci�n de la Introducci�n")]
    [Tooltip("El componente de texto donde se mostrar�n las frases.")]
    [SerializeField] private TextMeshProUGUI introText;
    [Tooltip("El CanvasGroup del texto para controlar su fundido (fade).")]
    [SerializeField] private CanvasGroup textCanvasGroup;
    [Tooltip("El nombre exacto de la escena principal del juego a cargar despu�s de la intro.")]
    [SerializeField] private string sceneToLoadAfterIntro = "NombreDeTuEscenaPrincipal";

    [Header("Frases de la Historia")]
    [Tooltip("Escribe aqu� cada frase de la introducci�n. Cada elemento es una nueva 'diapositiva'.")]
    [TextArea(3, 5)]
    [SerializeField] private string[] introPhrases;

    [Header("Tiempos y Velocidades")]
    [Tooltip("Cu�nto tiempo (en segundos) tarda el texto en aparecer y desaparecer.")]
    [SerializeField] private float fadeDuration = 1.5f;
    [Tooltip("Cu�nto tiempo (en segundos) permanece cada frase en pantalla.")]
    [SerializeField] private float displayDuration = 4.0f;
    [Tooltip("Peque�a pausa entre el final de una frase y el inicio de la siguiente.")]
    [SerializeField] private float pauseBetweenPhrases = 0.5f;

    void Start()
    {
        // Asegurarse de que el texto est� invisible al empezar
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0;
        }

        // Iniciar la secuencia de la introducci�n
        StartCoroutine(ShowIntroSequence());
    }

    private IEnumerator ShowIntroSequence()
    {
        yield return new WaitForSeconds(1f); // Una peque�a pausa inicial

        // Recorrer cada frase que hemos escrito
        foreach (string phrase in introPhrases)
        {
            // Actualizar el texto
            if (introText != null)
            {
                introText.text = phrase;
            }

            // Hacer que el texto aparezca (Fade In)
            yield return StartCoroutine(FadeCanvasGroup(textCanvasGroup, 0f, 1f, fadeDuration));

            // Esperar mientras la frase est� en pantalla
            yield return new WaitForSeconds(displayDuration);

            // Hacer que el texto desaparezca (Fade Out)
            yield return StartCoroutine(FadeCanvasGroup(textCanvasGroup, 1f, 0f, fadeDuration));

            // Pausa antes de la siguiente frase
            yield return new WaitForSeconds(pauseBetweenPhrases);
        }

        // Cuando todas las frases han terminado, cargar la escena principal del juego
        SceneManager.LoadScene(sceneToLoadAfterIntro);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        if (cg == null) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }
}
