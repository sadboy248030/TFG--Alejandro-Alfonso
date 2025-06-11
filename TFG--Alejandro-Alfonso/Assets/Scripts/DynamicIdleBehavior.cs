using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicIdleBehavior : MonoBehaviour
{
    [Header("Animation Settings")] 
    [Tooltip("El componente Animator del NPC.")] 
    [SerializeField] private Animator anim;
    [Tooltip("Tiempo m�nimo que el NPC pasar� mirando en una direcci�n antes de cambiar.")]
    [SerializeField] private float minLookTime = 2f;
    [Tooltip("Tiempo m�ximo que el NPC pasar� mirando en una direcci�n antes de cambiar.")]
    [SerializeField] private float maxLookTime = 5f;


    [System.Serializable] 
    public struct AllowedDirections
    {
        public bool lookUp;
        public bool lookDown;
        public bool lookLeft;
        public bool lookRight;
    }
    [Tooltip("Define en qu� direcciones puede mirar este NPC cuando est� en idle din�mico.")]
    [SerializeField]
    private AllowedDirections allowedLookDirections =
        new AllowedDirections { lookUp = true, lookDown = true, lookLeft = true, lookRight = true }; // Por defecto, todas las direcciones est�n permitidas.

    [Tooltip("Direcci�n inicial en la que mirar� el NPC al empezar. Debe ser 'Up', 'Down', 'Left', o 'Right'.")]
    [SerializeField] private string initialDirection = "Down"; // Direcci�n por defecto al iniciar.


    private List<string> _possibleDirectionKeys = new List<string>(); // Lista para almacenar las claves de direcci�n permitidas (ej: "Up", "Left").
    private string _currentDirectionKey; // Almacena la clave de la direcci�n actual en la que mira el NPC (ej: "Down").
    private Coroutine _idleCycleCoroutine; // Referencia a la corrutina que maneja el ciclo de cambio de direcci�n.
    private bool _isExternalControlActive = false; // Bandera para saber si un sistema externo (como el di�logo) ha tomado el control.




    void Awake()
    {
        // Si el Animator no fue asignado en el Inspector, intenta encontrarlo.
        if (anim == null)
        {
            // Busca un GameObject hijo llamado "charactersprite" (ajusta el nombre si es diferente en tu prefab).
            Transform characterSprite = transform.Find("charactersprite");
            if (characterSprite != null)
            {
                anim = characterSprite.GetComponent<Animator>(); // Obtiene el Animator del hijo.
            }
            else
            {
                anim = GetComponent<Animator>(); // Si no hay hijo, busca en el mismo GameObject.
            }
        }

        // Si sigue sin encontrar el Animator, muestra un error y deshabilita el script.
        if (anim == null)
        {
            Debug.LogError("DynamicIdleBehavior: No se encontr� un componente Animator en " + gameObject.name + " o en su hijo 'charactersprite'.", this);
            enabled = false; // Deshabilitar este script para evitar errores continuos.
            return;
        }
        _currentDirectionKey = initialDirection; // Establece la direcci�n actual a la inicial configurada.
    }


    void Start()
    {
        BuildPossibleDirectionsList(); // Construye la lista de direcciones permitidas.

        if (_possibleDirectionKeys.Count > 0) // Si hay al menos una direcci�n permitida.
        {
            // Asegura que la direcci�n inicial sea v�lida o elige una de las permitidas como fallback.
            if (!_possibleDirectionKeys.Contains(_currentDirectionKey))
            {
                _currentDirectionKey = _possibleDirectionKeys[0]; // Usa la primera permitida.
                Debug.LogWarning($"DynamicIdleBehavior: initialDirection '{initialDirection}' no est� en las permitidas para {gameObject.name}. Usando '{_currentDirectionKey}'.", this);
            }

            PlayIdleAnimation(_currentDirectionKey); // Reproduce la animaci�n idle inicial.

            // Inicia el ciclo de cambio de direcci�n solo si no est� controlado externamente desde el inicio.
            if (!_isExternalControlActive)
            {
                StartIdleCycle();
            }
        }
        else // Si no se configur� ninguna direcci�n permitida (aunque BuildPossibleDirectionsList tiene un fallback).
        {
            Debug.LogWarning("DynamicIdleBehavior: No hay direcciones permitidas configuradas para " + gameObject.name + ". El NPC permanecer� en su pose inicial.", this);
            PlayIdleAnimation(_currentDirectionKey); // Intenta reproducir la pose inicial.
        }
    }


    void OnEnable()
    {
        // Si el script se reactiva (y no es la primera vez, que lo maneja Start),
        // y no est� bajo control externo, y el Animator est� listo, reanudar el ciclo.
        if (!_isExternalControlActive && anim != null && anim.isInitialized && _possibleDirectionKeys.Count > 0)
        {
            PlayIdleAnimation(_currentDirectionKey); // Restaura la �ltima pose conocida.
            StartIdleCycle(); // Reanuda el ciclo de idle din�mico.
        }
    }


    void OnDisable()
    {
        // Detiene la corrutina si el objeto se desactiva para evitar errores.
        StopIdleCycle();
    }



    // Construye la lista _possibleDirectionKeys a partir de las configuraciones de allowedLookDirections.
    void BuildPossibleDirectionsList()
    {
        _possibleDirectionKeys.Clear(); // Limpia la lista por si se llama varias veces.
        if (allowedLookDirections.lookUp) _possibleDirectionKeys.Add("Up");
        if (allowedLookDirections.lookDown) _possibleDirectionKeys.Add("Down");
        if (allowedLookDirections.lookLeft) _possibleDirectionKeys.Add("Left");
        if (allowedLookDirections.lookRight) _possibleDirectionKeys.Add("Right");

        // Si el usuario no marc� ninguna direcci�n en el Inspector,
        // por defecto se usa la initialDirection (si es v�lida) o "Down" como fallback.
        if (_possibleDirectionKeys.Count == 0)
        {
            if (!string.IsNullOrEmpty(initialDirection) &&
                (initialDirection == "Up" || initialDirection == "Down" || initialDirection == "Left" || initialDirection == "Right"))
            {
                _possibleDirectionKeys.Add(initialDirection);
            }
            else
            {
                _possibleDirectionKeys.Add("Down"); // Fallback absoluto si initialDirection tampoco es v�lida.
            }
            Debug.LogWarning("DynamicIdleBehavior: No se especificaron direcciones en allowedLookDirections para " + gameObject.name + ". Usando: " + _possibleDirectionKeys[0], this);
        }
    }

    // Reproduce la animaci�n "Idle" correspondiente a la directionKey dada.
    void PlayIdleAnimation(string directionKey)
    {
        if (anim == null || string.IsNullOrEmpty(directionKey)) return; // Salir si no hay Animator o la clave es inv�lida.

        _currentDirectionKey = directionKey; // Actualiza la direcci�n actual.
        string animationName = "Idle" + _currentDirectionKey; // Construye el nombre de la animaci�n (ej: "IdleUp").

        // Evita reiniciar la animaci�n si ya se est� reproduciendo la misma.
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            anim.Play(animationName); // Reproduce la animaci�n.
        }
    }

    // Corrutina que maneja el ciclo de cambiar la direcci�n de "idle" aleatoriamente.
    IEnumerator IdleCycleRoutine()
    {
        while (true) // Bucle infinito para que el ciclo contin�e.
        {
            // Espera un tiempo aleatorio entre minLookTime y maxLookTime.
            float waitTime = Random.Range(minLookTime, maxLookTime);
            yield return new WaitForSeconds(waitTime);

            // Si est� bajo control externo (ej: durante un di�logo), espera aqu� hasta que se libere.
            while (_isExternalControlActive)
            {
                yield return null; // Espera un frame y vuelve a comprobar la bandera _isExternalControlActive.
            }

            // Si, despu�s de la espera y de que ya no est� bajo control externo, a�n hay direcciones posibles...
            if (_possibleDirectionKeys.Count > 0) // (esta comprobaci�n ya se hace en StartIdleCycle, pero es una doble seguridad)
            {
                string newDirectionKey = _currentDirectionKey; // Empieza con la direcci�n actual.

                // Intenta elegir una direcci�n diferente a la actual, si hay m�s de una opci�n permitida.
                if (_possibleDirectionKeys.Count > 1)
                {
                    int attempts = 0; // Contador para evitar un bucle infinito si algo va mal (poco probable aqu�).
                    // Sigue eligiendo una nueva direcci�n aleatoria hasta que sea diferente de la actual.
                    while (newDirectionKey == _currentDirectionKey && attempts < _possibleDirectionKeys.Count * 2) // Limitar intentos
                    {
                        newDirectionKey = _possibleDirectionKeys[Random.Range(0, _possibleDirectionKeys.Count)];
                        attempts++;
                    }
                }
                else // Si solo hay una direcci�n permitida, usa esa.
                {
                    newDirectionKey = _possibleDirectionKeys[0];
                }
                PlayIdleAnimation(newDirectionKey); // Reproduce la animaci�n para la nueva direcci�n.
            }
        }
    }

    // Inicia (o reinicia) la corrutina del ciclo de idle.
    private void StartIdleCycle()
    {
        StopIdleCycle(); // Detiene cualquier ciclo anterior primero para evitar duplicados.
        // Solo inicia la corrutina si el componente est� habilitado, el GameObject est� activo en la jerarqu�a,
        // el Animator est� inicializado y hay direcciones posibles.
        if (this.enabled && gameObject.activeInHierarchy && anim != null && anim.isInitialized && _possibleDirectionKeys.Count > 0)
        {
            _idleCycleCoroutine = StartCoroutine(IdleCycleRoutine());
        }
    }

    // Detiene la corrutina del ciclo de idle si est� en ejecuci�n.
    private void StopIdleCycle()
    {
        if (_idleCycleCoroutine != null)
        {
            StopCoroutine(_idleCycleCoroutine);
            _idleCycleCoroutine = null; // Limpiar la referencia a la corrutina.
        }
    }

    /// <summary>
    /// Pausa el ciclo de idle din�mico y hace que el NPC mire a un objetivo espec�fico.
    /// Llamado por NPCDialogue cuando comienza una conversaci�n.
    /// </summary>
    /// <param name="targetToFace">El Transform del objeto (jugador) hacia el que el NPC debe mirar.</param>
    public void FocusOnTarget(Transform targetToFace)
    {
        _isExternalControlActive = true; // Activa la bandera de control externo.

    

        if (anim != null && targetToFace != null) // Si hay Animator y un objetivo v�lido.
        {
            // Calcula la direcci�n hacia el objetivo.
            Vector2 directionToTarget = (targetToFace.position - transform.position).normalized;
            string newDirectionKey = _currentDirectionKey; // Por defecto, mantener la direcci�n actual si no se puede calcular una nueva.

            if (directionToTarget.sqrMagnitude > 0.01f) // Solo calcular si hay una direcci�n v�lida.
            {
                if (Mathf.Abs(directionToTarget.x) > Mathf.Abs(directionToTarget.y))
                {
                    newDirectionKey = directionToTarget.x > 0 ? "Right" : "Left";
                }
                else
                {
                    newDirectionKey = directionToTarget.y > 0 ? "Up" : "Down";
                }
            }
            PlayIdleAnimation(newDirectionKey); // Actualiza _currentDirectionKey y reproduce la animaci�n "Idle" + nueva direcci�n.
        }
        else if (anim != null) // Si no hay target, al menos asegurar que est� en su pose Idle actual.
        {
            PlayIdleAnimation(_currentDirectionKey);
        }
    }

    /// <summary>
    /// Reanuda el ciclo de idle din�mico.
    /// Llamado por NPCDialogue cuando termina una conversaci�n.
    /// </summary>
    public void ResumeDynamicIdle()
    {
        _isExternalControlActive = false; // Desactiva la bandera de control externo.

        // Si la corrutina se detuvo expl�citamente en FocusOnTarget
        // o para asegurar que el temporizador del ciclo se reinicie correctamente:
        if (this.enabled && gameObject.activeInHierarchy) // Solo si el componente est� activo.
        {
            // Asegura que el NPC est� en la pose correcta (la que ten�a al mirar al jugador o su �ltima pose aleatoria).
            PlayIdleAnimation(_currentDirectionKey);
            // Reinicia el ciclo de idle din�mico. Esto har� que espere un nuevo tiempo aleatorio
            // antes de elegir una nueva direcci�n.
            StartIdleCycle();
        }
    }
}
