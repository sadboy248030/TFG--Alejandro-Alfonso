using UnityEngine;
using System.Collections.Generic;

public class EnemyEncounter : MonoBehaviour
{
    [Header("Configuraci�n del Encuentro")]
    [Tooltip("Lista de los datos de los enemigos que participar�n en este encuentro.")]
    public List<EnemyData> enemyGroup = new List<EnemyData>();

    [Tooltip("Si est� marcado, este grupo de enemigos NO reaparecer� tras ser derrotado (ideal para jefes o enemigos �nicos).")]
    public bool defeatPermanently = false;

    [Tooltip("Si est� marcado, el jugador podr� intentar huir de este encuentro.")]
    public bool canFleeFromThisEncounter = true;

    [HideInInspector]
    public bool isDefeated = false;

    /// <summary>
    /// Este m�todo se llama desde el CombatManager cuando el jugador gana la batalla.
    /// </summary>
    public void MarkAsDefeated()
    {
        isDefeated = true;
        gameObject.SetActive(false); // Siempre lo desactivamos visualmente.

        // Si NO es una derrota permanente, le pedimos al Respawn Manager que lo ponga en la cola.
        if (!defeatPermanently)
        {
            if (EnemyRespawnManager.Instance != null)
            {
                Debug.Log($"{gameObject.name} derrotado. Programando su reaparici�n.");
                EnemyRespawnManager.Instance.ScheduleRespawn(this);
            }
            else
            {
                Debug.LogWarning("Se intent� programar una reaparici�n, pero no se encontr� EnemyRespawnManager en la escena.", this);
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} derrotado permanentemente.");
            // Si es permanente, simplemente se queda desactivado.
        }
    }

    /// <summary>
    /// Este m�todo es llamado por el EnemyRespawnManager para reactivar al enemigo.
    /// </summary>
    public void Respawn()
    {
        isDefeated = false;
        gameObject.SetActive(true);

        // Si tienes el script de IA del enemigo (EnemyAIController), tambi�n lo reactivamos.
        EnemyAIController aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            aiController.enabled = true;
        }
    }

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning("EnemyEncounter en '" + gameObject.name + "' no tiene un Collider2D. No podr� ser detectado por el jugador.", this);
        }
        else
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning("EnemyEncounter en '" + gameObject.name + "': Se recomienda que su Collider2D sea 'Is Trigger = true' para facilitar la detecci�n de interacci�n por el jugador.", this);
            }
        }
    }
}
