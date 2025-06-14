using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinueIndicatorAnimator : MonoBehaviour
{
    [Header("Configuraci�n del Movimiento")]
    [Tooltip("La cantidad (en unidades de UI) que el indicador subir� y bajar� desde su posici�n original.")]
    [SerializeField] private float bobAmount = 5f; // Por ejemplo, 5 p�xeles/unidades de UI

    [Tooltip("La velocidad del movimiento de subida y bajada.")]
    [SerializeField] private float bobSpeed = 2f; // Ajusta esto para cambiar la velocidad

    private RectTransform rectTransform; // Referencia al RectTransform de este GameObject
    private Vector2 initialAnchoredPosition; // Posici�n Y original del indicador

    void Awake()
    {
        // Obtener el componente RectTransform de este GameObject
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("ContinueIndicatorAnimator: No se encontr� un RectTransform en " + gameObject.name + ". El script no funcionar�.", this);
            enabled = false; // Deshabilitar el script si no hay RectTransform
            return;
        }
    }

    void OnEnable()
    {
        // Cuando el objeto se activa (por ejemplo, cuando NPCDialogue lo hace visible),
        // guardar su posici�n Y inicial para que el "bobbing" sea relativo a ella.
        // Es importante hacerlo en OnEnable en lugar de Start o Awake si el objeto
        // puede activarse y desactivarse m�ltiples veces.
        if (rectTransform != null)
        {
            initialAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        // Calcular el desplazamiento vertical usando una funci�n seno para un movimiento suave
        // Time.time * bobSpeed controla la frecuencia del seno (velocidad del bobbing)
        // bobAmount controla la amplitud del seno (cu�nto sube y baja)
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

        // Aplicar el desplazamiento a la posici�n Y anclada original
        rectTransform.anchoredPosition = new Vector2(initialAnchoredPosition.x, initialAnchoredPosition.y + yOffset);
    }
}
