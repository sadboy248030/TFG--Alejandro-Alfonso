using UnityEngine;
using Cinemachine; // Aseg�rate de tener el paquete Cinemachine instalado y este 'using'

/// <summary>
/// Este script se a�ade a cualquier CinemachineVirtualCamera que necesite
/// seguir al jugador. Se encarga de encontrar al jugador persistente
/// en la escena y asignarlo como el objetivo "Follow".
/// Esta versi�n es m�s robusta y reintenta la asignaci�n si falla al inicio.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))] // Asegura que este script solo se pueda a�adir a objetos con una VCam
public class CinemachineTargetSetter : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;

    void Awake()
    {
        // Obtener la referencia a la c�mara virtual en este mismo GameObject.
        vcam = GetComponent<CinemachineVirtualCamera>();
        Debug.Log($"CinemachineTargetSetter en '{this.gameObject.name}' se ha despertado.");
    }

    void Start()
    {
        // Intentar asignar el objetivo inmediatamente al iniciar.
        AssignFollowTarget();
    }

    // --- NUEVO: L�gica de Update para mayor robustez ---
    // Si la c�mara pierde su objetivo por cualquier raz�n (como un cambio de escena),
    // este m�todo intentar� reasignarlo en cada frame hasta que lo consiga.
    void Update()
    {
        // Si la c�mara no tiene un objetivo "Follow", intentar asignarlo.
        if (vcam.Follow == null)
        {
            AssignFollowTarget();
        }
    }

    /// <summary>
    /// Busca el GameObject del jugador por su tag y lo asigna al campo "Follow" de la c�mara.
    /// </summary>
    private void AssignFollowTarget()
    {
        // El check 'if (vcam.Follow != null)' ya no es necesario aqu� porque se hace en Update.

        // Buscar el jugador usando el tag "Player".
        // PlayerPersistence.Instance.transform es una forma a�n m�s segura si ya tienes ese script.
        GameObject playerTarget = GameObject.FindGameObjectWithTag("Player");

        if (playerTarget != null)
        {
            // Si se encuentra al jugador, asignar su Transform al campo "Follow".
            vcam.Follow = playerTarget.transform;
            Debug.Log($"CinemachineTargetSetter: ��xito! Objetivo '{playerTarget.name}' asignado a la c�mara '{this.gameObject.name}'.");
        }
        else
        {
            // Este mensaje ahora es menos preocupante, ya que el Update() lo intentar� de nuevo.
            // Debug.LogWarning("CinemachineTargetSetter: Intentando encontrar al jugador... (Todav�a no se ha encontrado un objeto con el tag 'Player')");
        }
    }
}
