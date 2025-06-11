using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script gen�rico para convertir un GameObject en un Singleton que persiste
/// entre escenas y se asegura de que solo exista una instancia de s� mismo.
/// Esencial para objetos como el EventSystem y el Canvas principal de la UI.
/// </summary>
public class PersistentSingleton : MonoBehaviour
{
    [Tooltip("Etiqueta �nica para este tipo de objeto persistente (ej: 'MainEventSystem', 'MainCanvas'). Esto permite tener varios singletons de este tipo, cada uno con su propio tag.")]
    [SerializeField] private string singletonTag;

    // Una lista est�tica para llevar un registro de los tags de los singletons que ya han sido creados.
    private static List<string> createdTags = new List<string>();

    void Awake()
    {
        // Si no se ha asignado un tag en el Inspector, usar el nombre del GameObject como fallback.
        if (string.IsNullOrEmpty(singletonTag))
        {
            singletonTag = this.gameObject.name;
        }

        // Comprobar si ya existe un objeto persistente con este mismo tag.
        if (createdTags.Contains(singletonTag))
        {
            // Si ya existe, significa que este es un duplicado (ej: al volver a la escena principal).
            // Destruimos este GameObject duplicado para evitar problemas y detenemos la ejecuci�n del script.
            Debug.LogWarning($"Singleton duplicado detectado para el tag '{singletonTag}'. Destruyendo el nuevo objeto '{this.gameObject.name}'.");
            Destroy(this.gameObject);
            return;
        }

        // Si es el primero con este tag, lo registramos y lo hacemos persistente.
        createdTags.Add(singletonTag);
        DontDestroyOnLoad(this.gameObject);
        Debug.Log($"Singleton persistente creado para el tag '{singletonTag}'.");
    }

    /// <summary>
    /// Opcional: Este m�todo se llama cuando la aplicaci�n se cierra.
    /// Limpia la lista de tags creados, lo cual es �til si est�s trabajando
    /// en el editor de Unity para asegurar un estado limpio en cada nueva ejecuci�n.
    /// </summary>
    private void OnApplicationQuit()
    {
        createdTags.Clear();
    }
}
