using UnityEngine;

// Este script ahora solo se encarga de iniciar el combate al ser tocado.
[RequireComponent(typeof(EnemyEncounter))] // Necesita saber qu� combate iniciar.
[RequireComponent(typeof(Collider2D))]    // Necesita un collider para ser tocado.
public class EnemyAIController : MonoBehaviour
{
    private EnemyEncounter enemyEncounter;
    private bool combatHasStarted = false; // Para evitar iniciar el combate m�ltiples veces.

    void Start()
    {
        // Obtenemos la referencia a los datos del combate.
        enemyEncounter = GetComponent<EnemyEncounter>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Si el combate ya ha empezado o si el objeto que nos toca no es el jugador, no hacemos nada.
        if (combatHasStarted || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // Marcamos que el combate ha comenzado para no volver a entrar aqu�.
        combatHasStarted = true;
        Debug.Log($"�Colisi�n con el jugador! {gameObject.name} inicia combate.");

        // Buscamos la party del jugador para pasarla al CombatManager.
        var playerParty = FindObjectOfType<PartyManager>();

        if (CombatManager.Instance != null && playerParty != null)
        {
            // Llamamos al CombatManager para que inicie la secuencia de combate.
            CombatManager.Instance.StartCombat(playerParty.CurrentPartyMembers, enemyEncounter.enemyGroup, enemyEncounter);
        }
        else
        {
            Debug.LogError("No se pudo iniciar el combate. Falta CombatManager o PartyManager en la escena.", this);
        }
    }
}