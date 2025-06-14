using UnityEngine;
using System.Collections.Generic;
using TopDown; // Aseg�rate de que tu clase Character es accesible

public class NPCHealer : MonoBehaviour
{
    /// <summary>
    /// Este m�todo p�blico puede ser llamado desde cualquier otro script (como el DialogueManager).
    /// Se encarga de curar completamente a todos los miembros de la party.
    /// </summary>
    public void HealPartyCompletely()
    {
        // Buscamos el PartyManager para acceder a la lista de personajes.
        PartyManager partyManager = FindObjectOfType<PartyManager>();
        if (partyManager == null)
        {
            Debug.LogError("NPCHealer: No se pudo encontrar el PartyManager en la escena.", this);
            return;
        }

        Debug.Log("Iniciando curaci�n completa de la party...");

        // Iteramos sobre cada personaje en la party.
        foreach (Character member in partyManager.CurrentPartyMembers)
        {
            if (member != null)
            {
                // Restauramos el HP y el MP a su valor m�ximo.
                member.currentHP = member.MaxHP;
                member.currentMP = member.MaxMP;
                Debug.Log($"Personaje '{member.characterName}' curado. HP: {member.currentHP}/{member.MaxHP}, MP: {member.currentMP}/{member.MaxMP}");
            }
        }

        Debug.Log("�La party ha sido curada completamente!");

        // Opcional: Podr�as a�adir un efecto de sonido o visual aqu�.
        // if (healingSound != null) AudioSource.PlayClipAtPoint(healingSound, transform.position);
    }
}
