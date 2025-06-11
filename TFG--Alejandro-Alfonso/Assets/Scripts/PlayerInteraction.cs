using System.Collections;
using System.Collections.Generic;
using TopDown; // Aseg�rate de que PlayerMovement y NPCDialogue est�n accesibles
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuraci�n de Interacci�n")]
    [Tooltip("La tecla que el jugador presionar� para iniciar la interacci�n con NPCs.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [Tooltip("La distancia m�xima (en unidades de Unity) a la que el jugador puede interactuar con un NPC.")]
    [SerializeField] private float interactionDistance = 1.0f;
    [Tooltip("La LayerMask para filtrar qu� objetos puede detectar el raycast (ej: solo la capa 'NPC' o 'Interactable').")]
    [SerializeField] private LayerMask interactableLayer;
    [Tooltip("Peque�o desplazamiento hacia adelante para el origen del raycast, para evitar que el rayo golpee al propio jugador.")]
    [SerializeField] private float raycastOriginOffset = 0.2f;

    // Referencia al script PlayerMovement del jugador
    private PlayerMovement playerMovement;

    // Ya no necesitamos 'currentActiveDialogue' ni la l�gica de 'advanceDialogueKey' aqu�.
    // El DialogueManager se encargar� de todo eso.

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerInteraction: No se encontr� el componente PlayerMovement en el jugador.", this);
            enabled = false;
        }
    }

    void Update()
    {
        // La responsabilidad de este script ahora es mucho m�s simple.
        // Solo debe detectar la pulsaci�n de la tecla de interacci�n,
        // pero �nicamente si no hay un di�logo ya en curso.

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying)
        {
            // Si el di�logo est� activo, no hacemos nada. El DialogueManager maneja el input.
            return;
        }

        // Si no hay di�logo, escuchar la tecla para intentar iniciar una nueva interacci�n.
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
    }

    /// <summary>
    /// Intenta iniciar una interacci�n con un NPC en la direcci�n en la que mira el jugador.
    /// </summary>
    private void TryInteract()
    {
        if (playerMovement == null) return;

        Vector2 interactionDirection = playerMovement.LastFacingVector;

        if (interactionDirection == Vector2.zero)
        {
            // Fallback por si el jugador est� quieto y no tiene una �ltima direcci�n guardada
            interactionDirection = Vector2.down;
        }

        Vector2 raycastOrigin = (Vector2)transform.position + (interactionDirection * raycastOriginOffset);

        Debug.DrawRay(raycastOrigin, interactionDirection * interactionDistance, Color.blue, 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, interactionDirection, interactionDistance, interactableLayer);

        if (hit.collider != null)
        {
            if (hit.collider.gameObject == this.gameObject) return;
            // Intentar obtener el componente NPCDialogue del objeto golpeado.
            if (hit.collider.TryGetComponent<SceneTransitionDoor>(out SceneTransitionDoor door))
            {
                Debug.Log($"Interactuando con puerta: {hit.collider.name}");
                door.Transition();
            }
            else if (hit.collider.TryGetComponent<NPCDialogue>(out NPCDialogue npcDialogue))
            {
                // --- NUEVA L�GICA INTELIGENTE ---
                // Primero, comprobar si el NPC tiene un QuestGiver.
                if (npcDialogue.TryGetComponent<QuestGiver>(out QuestGiver questGiver))
                {
                    // Si lo tiene, el QuestGiver decide qu� di�logo mostrar.
                    Debug.Log($"Interactuando con QuestGiver: {hit.collider.name}");
                    questGiver.StartQuestDialogue();
                }
                else
                {
                    // Si no, iniciar el di�logo por defecto del NPC.
                    Debug.Log($"Interactuando con NPC (di�logo por defecto): {hit.collider.name}");
                    npcDialogue.StartDefaultDialogue();
                }
            }
        }
    }


}