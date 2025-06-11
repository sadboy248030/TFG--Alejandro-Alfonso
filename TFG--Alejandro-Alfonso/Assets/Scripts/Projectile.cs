using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour
{
    [Header("Configuraci�n del Proyectil")]
    [Tooltip("Velocidad a la que se mueve el proyectil.")]
    [SerializeField] private float speed = 10f;
    [Tooltip("Prefab del efecto de impacto a instanciar al colisionar (opcional).")]
    [SerializeField] private GameObject impactVFXPrefab;
    [Tooltip("Tiempo de vida del proyectil en segundos antes de autodestruirse si no golpea nada.")]
    [SerializeField] private float lifetime = 5f;
    [Tooltip("Duraci�n de la animaci�n de impacto antes de destruir el proyectil (si la hay).")]
    [SerializeField] private float impactAnimationDuration = 0.5f;

    private Transform _targetTransform;
    private Combatant _attacker;
    private Combatant _targetCombatant;

    // Para distinguir el origen y c�mo calcular el da�o/efecto
    private AbilityData _originatingAbility;
    private ItemData _originatingWeapon; // Para proyectiles de ataque b�sico (aunque no lo usemos para el da�o si viene precalculado)
    private int _basicAttackDamageValue; // Da�o espec�fico para este proyectil de ataque b�sico

    private Animator _animator;
    private bool _hitOccurred = false;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogWarning("Projectile: No se encontr� el componente Animator en " + gameObject.name, this);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
        // Si tienes una animaci�n de "Volar", aseg�rate de que sea el estado por defecto
        // en el Animator Controller del proyectil, o act�vala aqu�.
    }

    /// <summary>
    /// Inicializa el proyectil lanzado por una HABILIDAD.
    /// </summary>
    public void InitializeSkillProjectile(Combatant target, Combatant attackerCombatant, AbilityData ability)
    {
        if (target != null && target.combatSpriteGO != null)
        {
            _targetTransform = target.combatSpriteGO.transform;
        }
        _targetCombatant = target;
        _attacker = attackerCombatant;
        _originatingAbility = ability;
        _originatingWeapon = null;
        _basicAttackDamageValue = 0; // No es un ataque b�sico

        if (_targetTransform == null)
        {
            Debug.LogWarning($"Projectile (Skill '{ability?.abilityName}'): Objetivo nulo o sprite del objetivo nulo. Proyectil se autodestruir�.");
            Destroy(gameObject, 0.1f);
        }
    }

    /// <summary>
    /// NUEVO: Inicializa el proyectil lanzado por un ATAQUE B�SICO.
    /// </summary>
    public void InitializeBasicAttackProjectile(Combatant target, Combatant attackerCombatant, ItemData weapon, int attackDamage)
    {
        if (target != null && target.combatSpriteGO != null)
        {
            _targetTransform = target.combatSpriteGO.transform;
        }
        _targetCombatant = target;
        _attacker = attackerCombatant;
        _originatingWeapon = weapon; // Guardamos el arma, aunque el da�o ya venga calculado
        _basicAttackDamageValue = attackDamage; // Guardamos el da�o precalculado
        _originatingAbility = null;

        if (_targetTransform == null)
        {
            Debug.LogWarning("Projectile (Basic Attack): Objetivo nulo o sprite del objetivo nulo. Proyectil se autodestruir�.");
            Destroy(gameObject, 0.1f);
        }
    }


    void Update()
    {
        if (_hitOccurred) return;
        if (isTargetDefeatedOrNull()) { Destroy(gameObject); return; }

        if (_targetTransform != null)
        {
            Vector3 direction = (_targetTransform.position - transform.position).normalized;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            if (direction != Vector3.zero)
            {
                // L�gica de rotaci�n (opcional, si tu sprite de proyectil necesita orientarse)
                // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    private bool isTargetDefeatedOrNull()
    {
        if (_targetCombatant != null && _targetCombatant.isDefeated) return true;
        if (_targetTransform == null && _targetCombatant == null) return true;
        // Si el transform del objetivo se vuelve null (ej: por desactivaci�n del GO antes de marcar isDefeated)
        if (_targetTransform == null && _targetCombatant != null && !_targetCombatant.isDefeated)
        {
            Debug.LogWarning($"Projectile: _targetTransform es null para {_targetCombatant.GetName()} aunque no est� derrotado. Destruyendo proyectil.");
            return true;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitOccurred || isTargetDefeatedOrNull()) return;

        if (_targetCombatant != null && _targetCombatant.combatSpriteGO == other.gameObject)
        {
            _hitOccurred = true;
            speed = 0;
            Debug.Log($"Projectile: �Impacto en el objetivo {_targetCombatant.GetName()}!");
            // --- REPRODUCIR SONIDO DE IMPACTO ---
            if (CombatManager.Instance != null && _originatingAbility != null)
            {
                CombatManager.Instance.PlaySoundEffect(_originatingAbility.impactSound);
            }
            // Si el ataque b�sico tambi�n tuviera un sonido de impacto, se a�adir�a una l�gica similar aqu�
            // else if (CombatManager.Instance != null && _originatingWeapon != null) { /* ... */ }
            // --- FIN SONIDO ---

            // Aplicar efecto/da�o
            if (_originatingAbility != null && _attacker != null) // Si fue lanzado por una HABILIDAD
            {
                if (_originatingAbility.effectType == AbilityEffectType.Damage)
                {
                    int calculatedDamage = Mathf.Max(1, (int)_originatingAbility.power + (_attacker.GetAttack() / 2) - _targetCombatant.GetDefense());
                    _targetCombatant.TakeDamage(calculatedDamage);
                }
                // (Aqu� ir�an otros efectos de habilidad si el proyectil los aplica directamente)
            }
            else if (_attacker != null) // Si fue lanzado por un ATAQUE B�SICO (originatingWeapon puede ser null)
            {
                // El da�o ya fue calculado por CombatManager y pasado como _basicAttackDamageValue
                Debug.Log($"Proyectil de ataque b�sico aplicando {_basicAttackDamageValue} de da�o precalculado a {_targetCombatant.GetName()}");
                _targetCombatant.TakeDamage(_basicAttackDamageValue);
            }

            StartCoroutine(HandleImpactSequence());
        }
    }

    private IEnumerator HandleImpactSequence()
    {
        if (impactVFXPrefab != null) Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
        if (_animator != null)
        {
            _animator.SetTrigger("ImpactTrigger");
            yield return new WaitForSeconds(impactAnimationDuration);
        }
        else if (impactVFXPrefab != null) { yield return new WaitForSeconds(0.1f); } // Peque�a pausa para el VFX si no hay anim de impacto

        Destroy(gameObject);
    }
}
