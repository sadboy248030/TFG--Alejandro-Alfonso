using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicIdleTester : MonoBehaviour
{
    [Tooltip("Arrastra aqu� desde el Inspector el NPC que tiene DynamicIdleBehavior.cs")]
    [SerializeField] private DynamicIdleBehavior npcToTest;

    [Tooltip("Opcional: Arrastra aqu� al jugador u otro objeto para que el NPC lo mire")]
    [SerializeField] private Transform targetToFace;

    void Update()
    {
        if (npcToTest == null)
        {
            if (Input.anyKeyDown) // Solo para que no spamee el log si no hay NPC asignado
            {
                Debug.LogWarning("DynamicIdleTester: No se ha asignado un NPC para probar en el Inspector.");
            }
            return;
        }

        // Probar hacer que el NPC se enfoque en un objetivo (simula inicio de di�logo)
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Tecla '1'
        {
            if (targetToFace != null)
            {
                Debug.Log("Tester: Haciendo que NPC se enfoque en " + targetToFace.name);
                npcToTest.FocusOnTarget(targetToFace);
            }
            else
            {
                Debug.LogWarning("Tester: No se asign� un targetToFace para la prueba de FocusOnTarget. El NPC se quedar� en su pose actual pero detendr� el ciclo din�mico.");
                npcToTest.FocusOnTarget(null); // Probar c�mo reacciona sin un objetivo espec�fico
            }
        }

        // Probar reanudar el comportamiento de idle din�mico (simula fin de di�logo)
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Tecla '2'
        {
            Debug.Log("Tester: Reanudando el idle din�mico del NPC.");
            npcToTest.ResumeDynamicIdle();
        }
    }
}
