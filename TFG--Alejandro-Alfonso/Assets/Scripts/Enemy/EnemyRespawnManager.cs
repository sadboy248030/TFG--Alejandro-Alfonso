using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyRespawnManager : MonoBehaviour
{
    public static EnemyRespawnManager Instance { get; private set; }

    [Header("Configuración de Reaparición")]
    [Tooltip("El tiempo en segundos que tardará un enemigo normal en reaparecer.")]
    [SerializeField] private float defaultRespawnTime = 60f; // 1 minuto por defecto

    [Tooltip("La distancia mínima que debe haber entre el jugador y el punto de reaparición para que el enemigo aparezca. Evita que aparezcan encima del jugador.")]
    [SerializeField] private float safeRespawnDistance = 15f;

    private Transform playerTransform;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Buscamos al jugador al iniciar para poder comprobar la distancia de seguridad.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("EnemyRespawnManager: No se pudo encontrar al jugador con el tag 'Player'.", this);
        }
    }

    /// <summary>
    /// Este método es llamado por un EnemyEncounter cuando es derrotado.
    /// </summary>
    /// <param name="encounter">El encuentro que debe reaparecer.</param>
    public void ScheduleRespawn(EnemyEncounter encounter)
    {
        StartCoroutine(RespawnCoroutine(encounter));
    }

    private IEnumerator RespawnCoroutine(EnemyEncounter encounterToRespawn)
    {
        // 1. Esperamos el tiempo de reaparición.
        yield return new WaitForSeconds(defaultRespawnTime);

        // 2. Antes de reaparecer, nos aseguramos de que el jugador no esté demasiado cerca.
        if (playerTransform != null)
        {
            // Mientras el jugador esté dentro de la distancia de seguridad, esperamos.
            while (Vector2.Distance(encounterToRespawn.transform.position, playerTransform.position) < safeRespawnDistance)
            {
                // Esperamos al siguiente frame y volvemos a comprobar.
                yield return null;
            }
        }

        // 3. Una vez es seguro, ¡reaparecemos al enemigo!
        Debug.Log($"Reapareciendo a {encounterToRespawn.gameObject.name} en su posición original.");
        encounterToRespawn.Respawn();
    }
}
