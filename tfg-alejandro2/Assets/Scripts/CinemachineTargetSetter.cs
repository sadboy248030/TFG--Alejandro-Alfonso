using UnityEngine;
using Cinemachine; // Asegúrate de tener el paquete Cinemachine instalado y este 'using'

/// <summary>
/// Este script se añade a cualquier CinemachineVirtualCamera que necesite
/// seguir al jugador. Se encarga de encontrar al jugador persistente
/// en la escena y asignarlo como el objetivo "Follow".
/// Esta versión es más robusta y reintenta la asignación si falla al inicio.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))] // Asegura que este script solo se pueda añadir a objetos con una VCam
public class CinemachineTargetSetter : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;

    void Awake()
    {
        // Obtener la referencia a la cámara virtual en este mismo GameObject.
        vcam = GetComponent<CinemachineVirtualCamera>();
        Debug.Log($"CinemachineTargetSetter en '{this.gameObject.name}' se ha despertado.");
    }

    void Start()
    {
        // Intentar asignar el objetivo inmediatamente al iniciar.
        AssignFollowTarget();
    }

    // --- NUEVO: Lógica de Update para mayor robustez ---
    // Si la cámara pierde su objetivo por cualquier razón (como un cambio de escena),
    // este método intentará reasignarlo en cada frame hasta que lo consiga.
    void Update()
    {
        // Si la cámara no tiene un objetivo "Follow", intentar asignarlo.
        if (vcam.Follow == null)
        {
            AssignFollowTarget();
        }
    }

    /// <summary>
    /// Busca el GameObject del jugador por su tag y lo asigna al campo "Follow" de la cámara.
    /// </summary>
    private void AssignFollowTarget()
    {
        // El check 'if (vcam.Follow != null)' ya no es necesario aquí porque se hace en Update.

        // Buscar el jugador usando el tag "Player".
        // PlayerPersistence.Instance.transform es una forma aún más segura si ya tienes ese script.
        GameObject playerTarget = GameObject.FindGameObjectWithTag("Player");

        if (playerTarget != null)
        {
            // Si se encuentra al jugador, asignar su Transform al campo "Follow".
            vcam.Follow = playerTarget.transform;
            Debug.Log($"CinemachineTargetSetter: ¡Éxito! Objetivo '{playerTarget.name}' asignado a la cámara '{this.gameObject.name}'.");
        }
        else
        {
            // Este mensaje ahora es menos preocupante, ya que el Update() lo intentará de nuevo.
            // Debug.LogWarning("CinemachineTargetSetter: Intentando encontrar al jugador... (Todavía no se ha encontrado un objeto con el tag 'Player')");
        }
    }
}
