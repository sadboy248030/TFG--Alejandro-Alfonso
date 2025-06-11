using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TopDown;
using TMPro; // Necesario para TextMeshProUGUI en ShowFloatingText


// Clase Combatant (CON LA CORRECCI�N PARA LA SINCRONIZACI�N DE HP)
public class Combatant
{
    public Character characterData;
    public EnemyData enemyData;
    public GameObject combatSpriteGO;
    public int speed;
    public bool isPlayerCharacter;
    public Animator animator; // Referencia al Animator del sprite de combate
    public int currentHP;
    public int maxHP;
    public bool isDefeated = false;
    public EnemyCombatStatusUI enemyStatusUI;
    public bool isDefending = false;
    private int defenseBonusWhileDefending = 5;
    private SpriteRenderer _spriteRenderer; // Guardar referencia para cambiar color
    private Color _originalSpriteColor;    // Para restaurar el color original

    public Combatant(Character character, GameObject spriteGO)
    {
        characterData = character;
        enemyData = null;
        combatSpriteGO = spriteGO;
        if (spriteGO != null)
        {
            animator = spriteGO.GetComponent<Animator>();
            _spriteRenderer = spriteGO.GetComponent<SpriteRenderer>(); // Obtener SpriteRenderer
            if (_spriteRenderer != null) _originalSpriteColor = _spriteRenderer.color; // Guardar color original
            else Debug.LogWarning($"Combatant {character.characterName} no tiene SpriteRenderer en su combatSpriteGO.");
        }
        speed = character.Speed;
        isPlayerCharacter = true;
        // Inicializar desde el Character. Sus valores son la fuente de verdad.
        this.currentHP = character.currentHP;
        this.maxHP = character.MaxHP;
        isDefeated = (this.currentHP <= 0);
        if (isDefeated && combatSpriteGO != null) combatSpriteGO.SetActive(false); // Desactivar si empieza derrotado
        isDefending = false;
        Debug.Log($"Combatant CREADO para JUGADOR: {GetName()}, HP Inicial: {this.currentHP}/{this.maxHP} (Desde Character: {character.currentHP}/{character.MaxHP})");
    }

    public Combatant(EnemyData enemy, GameObject spriteGO, EnemyCombatStatusUI statusUIScript = null)
    {
        characterData = null;
        enemyData = enemy;
        combatSpriteGO = spriteGO;
        if (spriteGO != null)
        {
            animator = spriteGO.GetComponent<Animator>();
            _spriteRenderer = spriteGO.GetComponent<SpriteRenderer>(); // Obtener SpriteRenderer
            if (_spriteRenderer != null) _originalSpriteColor = _spriteRenderer.color; // Guardar color original
            else Debug.LogWarning($"Combatant {enemy.enemyName} no tiene SpriteRenderer en su combatSpriteGO.");
        }
        speed = enemy.baseSpeed;
        isPlayerCharacter = false;
        currentHP = enemy.maxHP;
        maxHP = enemy.maxHP;
        isDefeated = false;
        enemyStatusUI = statusUIScript;
        isDefending = false;
    }

    public string GetName() { return isPlayerCharacter ? characterData.characterName : enemyData.enemyName; }

    public int GetCurrentHP()
    {
        // Para personajes, siempre leer del characterData que es la fuente de verdad.
        // Para enemigos, leer del currentHP local del Combatant.
        return isPlayerCharacter && characterData != null ? characterData.currentHP : this.currentHP;
    }

    public int GetMaxHP()
    {
        return isPlayerCharacter && characterData != null ? characterData.MaxHP : this.maxHP;
    }

    public int GetAttack() { return isPlayerCharacter ? (characterData != null ? characterData.Attack : 0) : (enemyData != null ? enemyData.baseAttack : 0); }
    public int GetDefense()
    {
        int baseDef = isPlayerCharacter ? (characterData != null ? characterData.Defense : 0) : (enemyData != null ? enemyData.baseDefense : 0);
        if (isDefending) return baseDef + defenseBonusWhileDefending;
        return baseDef;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDefeated || damageAmount <= 0) return;
        // --- REPRODUCIR SONIDO DE RECIBIR DA�O ---
        // Se reproduce antes de cualquier espera o efecto visual para que sea inmediato
        if (CombatManager.Instance != null && damageAmount > 0)
        {
            AudioClip hitSound = isPlayerCharacter ? characterData.takeHitSound : enemyData.takeHitSound;
            CombatManager.Instance.PlaySoundEffect(hitSound);
        }

        int hpBeforeDamage = GetCurrentHP(); // Usar GetCurrentHP() para leer el valor correcto

        int newHP = hpBeforeDamage - damageAmount;
        if (newHP < 0) newHP = 0;

        // Actualizar la fuente de verdad y la copia local
        if (isPlayerCharacter && characterData != null)
        {
            characterData.currentHP = newHP; // ACTUALIZA EL CHARACTER ORIGINAL
            this.currentHP = newHP;          // Sincroniza la copia local del Combatant
        }
        else
        {
            this.currentHP = newHP; // Para enemigos, Combatant.currentHP es la fuente
        }

        Debug.Log($"{GetName()} recibe {damageAmount} de da�o. HP antes: {hpBeforeDamage}, HP despu�s: {GetCurrentHP()}. Vida restante: {GetCurrentHP()}/{GetMaxHP()}");
        // Mostrar texto flotante para el da�o
        if (CombatManager.Instance != null && combatSpriteGO != null)
        {
            if (damageAmount > 0) CombatManager.Instance.ShowFloatingText("-" + damageAmount.ToString(), combatSpriteGO.transform.position, Color.red, this);
            if (animator != null) animator.SetTrigger("HitTrigger");
            if (_spriteRenderer != null && damageAmount > 0) CombatManager.Instance.StartCoroutine(FlashFeedbackCoroutine(_spriteRenderer, Color.red, _originalSpriteColor, 0.1f, 2));
        }
        Debug.Log($"{GetName()} recibe {damageAmount} de da�o. HP restante: {GetCurrentHP()}/{GetMaxHP()}");

        if (GetCurrentHP() <= 0)
        {
            HandleDefeat(); // Llamar al nuevo m�todo para manejar la derrota
        }

        // Actualizar HUDs
        if (isPlayerCharacter && CombatManager.Instance != null)
        {
            CombatManager.Instance.UpdatePartyStatusHUD();
        }
        else if (!isPlayerCharacter && enemyStatusUI != null)
        {
            enemyStatusUI.UpdateHPDisplay(); // Actualizar su propia barra de HP
        }
    }

    private void HandleDefeat()
    {
        if (isDefeated) return;
        isDefeated = true;
        Debug.Log($"{GetName()} ha sido derrotado!");
        // --- REPRODUCIR SONIDO DE DERROTA ---
        if (CombatManager.Instance != null)
        {
            AudioClip defeatSound = isPlayerCharacter ? null : enemyData.defeatSound; // Asumimos que solo enemigos tienen sonido de derrota
            CombatManager.Instance.PlaySoundEffect(defeatSound);
        }
        // --- FIN SONIDO ---
        // --- INICIO DE LA L�GICA A A�ADIR ---
        if (!isPlayerCharacter) // Asegurarse de que es un enemigo
        {
            // NOTIFICAR AL QUESTMANAGER QUE SE HA DERROTADO UN ENEMIGO
            if (QuestManager.Instance != null && enemyData != null && !string.IsNullOrEmpty(enemyData.enemyID))
            {
                QuestManager.Instance.NotifyEnemyKilled(enemyData.enemyID);
            }

            // La l�gica de ocultar la UI del enemigo tambi�n va aqu� dentro
            if (enemyStatusUI != null) enemyStatusUI.gameObject.SetActive(false);
        }
        // --- FIN DE LA L�GICA A A�ADIR ---
        if (combatSpriteGO != null && CombatManager.Instance != null)
        {
            float preFadeDelay = 0f;
            string defeatTrigger = "DefeatTrigger"; // Trigger por defecto para derrota

            if (isPlayerCharacter)
            {
                if (animator != null) animator.SetTrigger("HitTrigger"); // Jugador usa HitTrigger al ser derrotado
                preFadeDelay = 0.3f;
            }
            else
            {
                if (animator != null)
                {
                    animator.SetTrigger(defeatTrigger);
                    preFadeDelay = CombatManager.Instance.GetEnemyDefeatAnimDuration();
                }
            }
            CombatManager.Instance.StartCoroutine(DefeatSequenceCoroutine(preFadeDelay, 0.5f));
        }
    }

    private IEnumerator DefeatSequenceCoroutine(float preFadeDelay, float fadeDuration)
    {
        if (preFadeDelay > 0.01f) yield return new WaitForSeconds(preFadeDelay);
        if (_spriteRenderer != null)
        {
            float timer = 0f; Color startColor = _spriteRenderer.color;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, timer / fadeDuration));
                yield return null;
            }
            _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }
        if (combatSpriteGO != null) combatSpriteGO.SetActive(false);
        yield break;
    }

    public static IEnumerator FlashFeedbackCoroutine(SpriteRenderer sr, Color flashColor, Color originalColor, float flashDuration, int flashCount)
    {
        if (sr == null) yield break;

        for (int i = 0; i < flashCount; i++)
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            sr.color = originalColor;
            if (i < flashCount - 1) // No esperar despu�s del �ltimo flash si se restaura inmediatamente
            {
                yield return new WaitForSeconds(flashDuration);
            }
        }
        // Asegurarse de que el color final sea el original
        sr.color = originalColor;
    }


    public void ApplyHeal(int healAmount)
    {
        if (isDefeated || healAmount <= 0) return;

        int hpBeforeHeal = GetCurrentHP();
        int actualHealedAmount = 0;

        if (isPlayerCharacter && characterData != null)
        {
            if (characterData.Heal(healAmount)) // Character.Heal devuelve true si cur� algo
            {
                actualHealedAmount = characterData.currentHP - hpBeforeHeal;
                this.currentHP = characterData.currentHP; // Sincronizar
            }
        }
        else // Para enemigos (o si characterData fuera null)
        {
            int maxHealable = GetMaxHP() - hpBeforeHeal;
            if (maxHealable > 0)
            {
                actualHealedAmount = Mathf.Min(healAmount, maxHealable);
                this.currentHP += actualHealedAmount;
                if (this.currentHP > GetMaxHP()) this.currentHP = GetMaxHP();
            }
        }

        if (actualHealedAmount > 0)
        {
            if (CombatManager.Instance != null && combatSpriteGO != null)
            {
                CombatManager.Instance.ShowFloatingText("+" + actualHealedAmount.ToString(), combatSpriteGO.transform.position, Color.green, this);
                if (_spriteRenderer != null) // Parpadeo verde para curaci�n
                {
                    CombatManager.Instance.StartCoroutine(FlashFeedbackCoroutine(_spriteRenderer, Color.green, _originalSpriteColor, 0.1f, 2));
                }
            }
            Debug.Log($"{GetName()} se cura {actualHealedAmount}. HP actual: {GetCurrentHP()}/{GetMaxHP()}");
        }
        else
        {
            Debug.Log($"{GetName()} intent� curarse {healAmount} pero no tuvo efecto (HP ya al m�ximo o no se pudo curar).");
        }


        if (isPlayerCharacter && CombatManager.Instance != null) CombatManager.Instance.UpdatePartyStatusHUD();
        else if (!isPlayerCharacter && enemyStatusUI != null) enemyStatusUI.UpdateHPDisplay();
    }

    public void ApplyManaRestore(int manaAmount)
    {
        if (isDefeated || manaAmount <= 0) return;

        int actualRestoredAmount = 0;

        if (isPlayerCharacter && characterData != null)
        {
            int mpBeforeRestore = characterData.currentMP;
            if (characterData.RestoreMana(manaAmount)) // Character.RestoreMana devuelve true si restaur� algo
            {
                actualRestoredAmount = characterData.currentMP - mpBeforeRestore;
            }
        }
        // (Podr�as a�adir l�gica para enemigos si usan MP y pueden restaurarlo)

        if (actualRestoredAmount > 0)
        {
            if (CombatManager.Instance != null && combatSpriteGO != null)
            {
                CombatManager.Instance.ShowFloatingText("+" + actualRestoredAmount.ToString() + " MP", combatSpriteGO.transform.position, Color.blue, this);
            }
            Debug.Log($"{GetName()} restaura {actualRestoredAmount} MP. MP actual: {characterData.currentMP}/{characterData.MaxMP}");
            CombatManager.Instance.UpdatePartyStatusHUD(); // Asume que esto actualiza el MP en la UI
        }
        else
        {
            Debug.Log($"{GetName()} intent� restaurar {manaAmount} MP pero no tuvo efecto.");
        }
    }

    public void StartDefending()
    {
        isDefending = true;
        Debug.Log($"{GetName()} adopta una postura defensiva. Defensa aumentada.");
    }

    public void StopDefending()
    {
        if (isDefending) Debug.Log($"{GetName()} ya no est� en postura defensiva.");
        isDefending = false;
    }
}


public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    [Header("Audio")] // --- NUEVA SECCI�N ---
    [Tooltip("AudioSource para reproducir los efectos de sonido del combate.")]
    [SerializeField] private AudioSource sfxAudioSource;
    // ... (Variables [SerializeField] existentes) ...
    [Header("HUD de Combate - Informaci�n de Ronda/Turno")] // --- NUEVA SECCI�N ---
    [Tooltip("Elemento TextMeshProUGUI para mostrar el t�tulo de la ronda (ej: 'RONDA 1').")]
    [SerializeField] private TextMeshProUGUI roundTitleText;
    [Tooltip("Duraci�n en segundos que el t�tulo de la ronda permanecer� visible.")]
    [SerializeField] private float roundTitleDisplayDuration = 1.5f;
    // --- FIN NUEVA SECCI�N ---

    [Header("Estado del Combate")]
    [SerializeField] private bool isCombatActive = false;
    public bool IsCombatActive => isCombatActive;
    private bool isSelectingTargetForAttack = false;
    private Combatant attackerForTargetSelection;
    private bool isSelectingSkill = false; // <-- DECLARACI�N DE LA VARIABLE
    private bool isSelectingTargetForSkill = false;
    private AbilityData _selectedAbility = null;
    private bool isSelectingItem = false;
    private bool isSelectingTargetForItem = false;
    private ItemData _selectedItemData = null;


    [Header("Configuraci�n de Escena y UI")]
    [SerializeField] private GameObject explorationRootGameObject;
    [SerializeField] private GameObject currentCombatArenaGameObject;
    [SerializeField] private CinemachineVirtualCamera explorationCamera;
    [SerializeField] private CinemachineVirtualCamera combatCamera;
    [SerializeField] private GameObject combatScreenUIPanel;
    [SerializeField] private CanvasGroup fadePanelCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Posiciones de Combate")]
    [SerializeField] private List<Transform> partySpawnPoints = new List<Transform>();
    [SerializeField] private List<Transform> enemySpawnPoints = new List<Transform>();

    [Header("HUD de Combate - Estado de la Party")]
    [SerializeField] private Transform partyStatusAreaContainer;
    [SerializeField] private GameObject partyMemberStatusUIPrefab;

    [Header("HUD de Combate - Men� de Acciones")]
    [SerializeField] private GameObject actionMenuPanel;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillsButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button itemsButton;
    [SerializeField] private Button fleeButton;

    [Header("HUD de Combate - Selecci�n de Habilidades")]
    [SerializeField] private GameObject skillSelectionPanel;
    [SerializeField] private Transform skillListContainer;
    [SerializeField] private GameObject abilityListItemPrefab;
    [SerializeField] private Button closeSkillSelectionButton;

    [Header("HUD de Combate - Selecci�n de Objetos")]
    [Tooltip("El Panel que contiene la lista de objetos usables en combate.")]
    [SerializeField] private GameObject itemSelectionPanel_Combat;
    [Tooltip("El Transform 'Content' del ScrollView donde se instanciar�n los �tems de objeto.")]
    [SerializeField] private Transform itemListContainer_Combat;
    [Tooltip("Prefab para un �tem de objeto en la lista (debe tener CombatItemListItem_UI.cs).")]
    [SerializeField] private GameObject combatItemListItemPrefab;
    [Tooltip("Bot�n dentro del ItemSelectionPanel para volver al men� de acciones principal.")]
    [SerializeField] private Button closeItemSelectionButton;

    [Header("HUD de Combate - Estado de Enemigos")]
    [SerializeField] private GameObject enemyStatusUIPrefab;
    [SerializeField] private float enemyHPBarOffsetY = 0.7f;

    [Header("Capa de los Combatientes")]
    [SerializeField] private LayerMask combatantLayerMask;

    // --- NUEVA VARIABLE PARA LA PROBABILIDAD DE HUIR ---
    [Header("Mec�nicas de Huida")]
    [Tooltip("Probabilidad de �xito al intentar huir (0.0 a 1.0).")]
    [Range(0f, 1f)]
    [SerializeField] private float fleeSuccessChance = 0.7f; // 70% de probabilidad por defecto

    // --- NUEVAS VARIABLES PARA TEXTO FLOTANTE ---
    [Header("Feedback de Combate")]
    [Tooltip("Prefab para el texto flotante de da�o/curaci�n (debe tener FloatingCombatText.cs).")]
    [SerializeField] private GameObject floatingTextPrefab;
    [Tooltip("Transform padre para los textos flotantes. Si es null, se instanciar�n como hijos del combatiente (para World Space Canvas en el prefab del texto). Si se asigna un Canvas ScreenSpace, se intentar� convertir la posici�n.")]
    [SerializeField] private Transform floatingTextCanvasTransform;
    [SerializeField] private float floatingTextDefaultFontSize = 20f;
    [SerializeField] private float floatingTextYOffset = 0.6f;
    // --- NUEVO: Prefab para el VFX de golpe directo ---
    [Tooltip("Prefab del efecto visual (slash/hit) para ataques directos y habilidades sin proyectil.")]
    [SerializeField] private GameObject directHitVFXPrefab;
    [Tooltip("Offset Y para el VFX de golpe directo sobre el pivote del objetivo.")]
    [SerializeField] private float directHitVFX_Y_Offset = 0.3f; // Ajusta seg�n el tama�o de tus sprites y el pivote del VFX
                                                                 // --- FIN NUEVO ---

    [Header("Animaciones de Combate")]
    // --- ELIMINADAS LAS REFERENCIAS GEN�RICAS DE ANIMATOR CONTROLLER ---
    // [SerializeField] private RuntimeAnimatorController playerCombatAnimatorController; 
    // [SerializeField] private RuntimeAnimatorController defaultEnemyCombatAnimatorController;
    // --- REINTRODUCIDAS LAS REFERENCIAS GEN�RICAS COMO FALLBACK ---
    [Tooltip("Animator Controller por defecto para los personajes de la party si su Character.cs no tiene uno asignado.")]
    [SerializeField] private RuntimeAnimatorController playerFallbackAnimatorController;
    [Tooltip("Animator Controller por defecto para los enemigos si su EnemyData.cs no tiene uno asignado.")]
    [SerializeField] private RuntimeAnimatorController defaultEnemyFallbackAnimatorController;
    // --- FIN REINTRODUCCI�N ---
    [Tooltip("Duraci�n por defecto para animaciones de ataque/habilidad del jugador si la habilidad no especifica una o el personaje no tiene controller espec�fico.")]
    [SerializeField] private float playerGenericAnimationDuration = 0.6f;
    [Tooltip("Duraci�n de la animaci�n de ataque del enemigo si su EnemyData no especifica una o no tiene controller.")]
    [SerializeField] private float enemyAttackAnimationDuration = 0.8f;
    [SerializeField] private float enemyDefeatAnimationBaseDuration = 1.0f;
    // --- NUEVA VARIABLE PARA DELAY DE IMPACTO DE PROYECTILES DE HABILIDAD ---
    [Tooltip("Tiempo de espera adicional DESPU�S de la animaci�n del lanzador para que los proyectiles de habilidad impacten y sus efectos se procesen.")]
    [SerializeField] private float skillProjectileImpactDelay = 0.75f; // Ajusta este valor seg�n sea necesario



    // --- FIN NUEVO ---

    private List<Character> currentPlayerPartyData;
    private List<EnemyData> currentEnemyGroupData;
    private PlayerMovement playerMovementController;
    private EnemyEncounter _activeEncounter;
    private List<GameObject> _partyCombatSpriteGOs = new List<GameObject>();
    private List<GameObject> _enemyCombatSpriteGOs = new List<GameObject>();
    private List<PartyMemberCombatStatusUI> _partyStatusUIs = new List<PartyMemberCombatStatusUI>();
    private List<Combatant> _combatants = new List<Combatant>();
    private int _currentCombatantIndex = -1;
    private Combatant _activeCombatant;
    private List<AbilityListItem_UI> _currentSkillListUIs = new List<AbilityListItem_UI>();
    private List<CombatItemListItem_UI> _currentCombatItemListUIs = new List<CombatItemListItem_UI>();
    private int _currentRoundNumber = 0; // --- NUEVO: Contador de rondas ---
    private Coroutine _roundTitleCoroutine; // Para manejar la corrutina del t�tulo


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CombatManager: Instancia DUPLICADA. Destruyendo este: " + gameObject.name);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("CombatManager: Awake - Instancia configurada.");
    }

    void Start()
    {
        playerMovementController = FindObjectOfType<PlayerMovement>();
        if (playerMovementController == null) Debug.LogWarning("CombatManager: No se encontr� PlayerMovement.", this);

        if (fadePanelCanvasGroup != null)
        {
            fadePanelCanvasGroup.alpha = 0f;
            fadePanelCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CombatManager: 'fadePanelCanvasGroup' no asignado.", this);
        }

        if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);
        if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);
        if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true);

        if (explorationCamera != null) explorationCamera.Priority = 10;
        if (combatCamera != null) combatCamera.Priority = 9;

        if (partyStatusAreaContainer == null) Debug.LogError("CombatManager: 'partyStatusAreaContainer' no asignado.", this);
        if (partyMemberStatusUIPrefab == null) Debug.LogError("CombatManager: 'partyMemberStatusUIPrefab' no asignado.", this);
        if (enemyStatusUIPrefab == null) Debug.LogError("CombatManager: 'enemyStatusUIPrefab' no asignado.", this);

        if (actionMenuPanel == null) Debug.LogError("CombatManager: 'actionMenuPanel' no asignado.", this);
        else actionMenuPanel.SetActive(false);

        if (skillSelectionPanel == null) Debug.LogError("CombatManager: 'skillSelectionPanel' no asignado.", this);
        else skillSelectionPanel.SetActive(false);
        if (skillListContainer == null) Debug.LogError("CombatManager: 'skillListContainer' no asignado.", this);
        if (abilityListItemPrefab == null) Debug.LogError("CombatManager: 'abilityListItemPrefab' no asignado.", this);

        // --- NUEVAS VALIDACIONES Y LISTENERS PARA OBJETOS ---
        if (itemSelectionPanel_Combat == null) Debug.LogError("CombatManager: 'itemSelectionPanel_Combat' no asignado.", this);
        else itemSelectionPanel_Combat.SetActive(false);
        if (itemListContainer_Combat == null) Debug.LogError("CombatManager: 'itemListContainer_Combat' no asignado.", this);
        if (combatItemListItemPrefab == null) Debug.LogError("CombatManager: 'combatItemListItemPrefab' no asignado.", this);
        if (closeItemSelectionButton != null) closeItemSelectionButton.onClick.AddListener(CloseItemSelectionPanel);
        else Debug.LogWarning("CombatManager: 'closeItemSelectionButton' (para panel de �tems) no asignado.", this);

        if (itemsButton != null) itemsButton.onClick.AddListener(OnItemsButtonClicked);
        else Debug.LogWarning("CombatManager: 'itemsButton' no asignado.", this);
        // --- FIN NUEVAS VALIDACIONES Y LISTENERS ---
        if (floatingTextPrefab == null) Debug.LogError("CombatManager: 'floatingTextPrefab' no asignado. No se mostrar� texto flotante.", this);
        // floatingTextCanvasTransform es opcional, as� que un LogWarning est� bien si est� vac�o.
        if (floatingTextCanvasTransform == null) Debug.LogWarning("CombatManager: 'floatingTextCanvasTransform' no asignado. Los textos flotantes se instanciar�n como hijos del combatiente (asume World Space Canvas en prefab) o en la ra�z de la escena.", this);
        if (directHitVFXPrefab == null) Debug.LogWarning("CombatManager: 'directHitVFXPrefab' no asignado. No se mostrar� VFX para golpes directos.", this);
        if (roundTitleText != null)
        {
            roundTitleText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CombatManager: 'roundTitleText' no asignado. No se mostrar� el t�tulo de la ronda.");
        }
        if (sfxAudioSource == null) Debug.LogWarning("CombatManager: 'sfxAudioSource' no asignado. No se reproducir�n efectos de sonido.", this);

        if (attackButton != null) attackButton.onClick.AddListener(OnAttackButtonClicked);
        if (defendButton != null) defendButton.onClick.AddListener(OnDefendButtonClicked);
        else Debug.LogWarning("CombatManager: 'defendButton' no asignado.", this);
        if (skillsButton != null) skillsButton.onClick.AddListener(OnSkillsButtonClicked);
        else Debug.LogWarning("CombatManager: 'skillsButton' no asignado.", this);
        if (fleeButton != null) fleeButton.onClick.AddListener(OnFleeButtonClicked); // --- A�ADIR LISTENER ---
        else Debug.LogWarning("CombatManager: 'fleeButton' no asignado.", this);
        if (closeSkillSelectionButton != null)
        {
            closeSkillSelectionButton.onClick.AddListener(CloseSkillSelectionPanel);
        }
        else
        {
            Debug.LogWarning("CombatManager: 'closeSkillSelectionButton' no asignado en el panel de habilidades. No se podr� cerrar con ese bot�n.", this);
        }
    }
    // --- NUEVO: M�todo para Reproducir Sonidos ---
    public void PlaySoundEffect(AudioClip clip)
    {
        if (clip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    public void StartCombat(List<Character> playerParty, List<EnemyData> enemyGroup, EnemyEncounter encounterReference)
    {
        if (isCombatActive)
        {
            Debug.LogWarning("CombatManager: StartCombat llamado pero isCombatActive ya es true.");
            return;
        }
        // --- LLAMADA PARA CAMBIAR A M�SICA DE COMBATE ---
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayCombatMusic();
        }
        // --- FIN DE LA LLAMADA ---
        if (playerParty == null || playerParty.Count == 0) { Debug.LogError("CombatManager: Party vac�a al iniciar combate."); return; }
        if (enemyGroup == null || enemyGroup.Count == 0) { Debug.LogError("CombatManager: Grupo de enemigos vac�o al iniciar combate."); return; }
        if (encounterReference == null) { Debug.LogError("CombatManager: Referencia a EnemyEncounter nula al iniciar combate."); return; }
        _currentRoundNumber = 0; // Resetear al iniciar combate
        if (roundTitleText != null) roundTitleText.gameObject.SetActive(false);

        isCombatActive = true;
        this.currentPlayerPartyData = new List<Character>(playerParty);
        this.currentEnemyGroupData = new List<EnemyData>(enemyGroup);
        this._activeEncounter = encounterReference;
        StartCoroutine(CombatTransitionCoroutine(true));
    }

    public void EndCombat(bool playerWon)
    {
        if (!isCombatActive && !isSelectingTargetForAttack && !isSelectingTargetForSkill && _activeEncounter == null)
        {
            Debug.LogWarning("CombatManager: EndCombat llamado cuando el combate no parece estar activo o ya est� terminando.");
        }
        // --- LLAMADA PARA VOLVER A LA M�SICA DE EXPLORACI�N ---
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayExplorationMusic();
        }
        // --- FIN DE LA LLAMADA ---
        isCombatActive = false;
        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;
        isSelectingSkill = false;
        isSelectingTargetForSkill = false;
        _selectedAbility = null;
        isSelectingItem = false; // Resetear estado de selecci�n de �tem
        isSelectingTargetForItem = false;
        _selectedItemData = null;
        if (roundTitleText != null) roundTitleText.gameObject.SetActive(false);
        if (_roundTitleCoroutine != null)
        {
            StopCoroutine(_roundTitleCoroutine);
            _roundTitleCoroutine = null;
        }

        StartCoroutine(CombatTransitionCoroutine(false, playerWon));
    }

    private IEnumerator CombatTransitionCoroutine(bool startingCombat, bool playerWon = false)
    {
        if (playerMovementController != null) playerMovementController.SetCanMove(false);

        if (fadePanelCanvasGroup != null)
        {
            fadePanelCanvasGroup.gameObject.SetActive(true);
            fadePanelCanvasGroup.alpha = 0f;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                fadePanelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            fadePanelCanvasGroup.alpha = 1f;
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (startingCombat)
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
            if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false); // Asegurar que est� oculto
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(false);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(true);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer pr)) pr.enabled = false;

            SetupCombatantsAndPrepareTurnOrder();
            PopulatePartyStatusUI();

            if (combatCamera != null) combatCamera.Priority = 11;
            if (explorationCamera != null) explorationCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(true);
        }
        else
        {
            // --- �NUEVA L�GICA DE EXPERIENCIA AQU�! ---
            if (playerWon)
            {
                // 1. Calcular la XP total de los enemigos derrotados.
                int totalXpGained = 0;
                foreach (var enemyData in currentEnemyGroupData)
                {
                    if (enemyData != null)
                    {
                        totalXpGained += enemyData.xpReward;
                    }
                }

                // 2. Repartir la XP entre los personajes de la party que est�n vivos.
                if (totalXpGained > 0)
                {
                    List<Character> survivingPartyMembers = currentPlayerPartyData.Where(p => p.currentHP > 0).ToList();
                    if (survivingPartyMembers.Count > 0)
                    {
                        int xpPerMember = totalXpGained / survivingPartyMembers.Count;
                        Debug.Log($"VICTORIA! Repartiendo {xpPerMember} XP a cada superviviente.");
                        foreach (Character member in survivingPartyMembers)
                        {
                            member.GainXP(xpPerMember);
                        }
                    }
                }
                foreach (var enemyData in currentEnemyGroupData)
                {
                    if (enemyData == null || enemyData.potentialDrops.Count == 0 || PlayerInventory.Instance == null)
                    {
                        continue; // Si el enemigo no tiene drops o no hay inventario, pasamos al siguiente.
                    }

                    // Revisamos la lista de posibles drops en orden.
                    foreach (ItemDropInfo dropInfo in enemyData.potentialDrops)
                    {
                        if (dropInfo.item == null) continue;

                        // Generamos un n�mero aleatorio para este objeto espec�fico.
                        if (Random.value <= dropInfo.chance)
                        {
                            Debug.Log($"�{enemyData.enemyName} ha soltado {dropInfo.item.itemName}!");
                            PlayerInventory.Instance.AddItem(dropInfo.item, 1);

                            // �IMPORTANTE! Hemos conseguido un drop, as� que dejamos de revisar
                            // la lista para este enemigo y pasamos al siguiente.
                            break;
                        }
                    }
                }
            }
            // --- FIN DE LA NUEVA L�GICA ---
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
            if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer pr)) pr.enabled = true;

            CleanupCombatants();
            CleanupPartyStatusUI();

            if (explorationCamera != null) explorationCamera.Priority = 10;
            if (combatCamera != null) combatCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);

            if (playerWon && _activeEncounter != null) _activeEncounter.MarkAsDefeated();
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        if (fadePanelCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                fadePanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            fadePanelCanvasGroup.alpha = 0f;
            fadePanelCanvasGroup.gameObject.SetActive(false);
        }

        if (!startingCombat)
        {
            if (playerMovementController != null) playerMovementController.SetCanMove(true);
            this.currentPlayerPartyData = null;
            this.currentEnemyGroupData = null;
            this._activeEncounter = null;
        }
        else
        {
            Debug.Log("CombatManager: Combate listo. Iniciando primer turno.");
            NextTurn();
        }
    }

    private void SetupCombatantsAndPrepareTurnOrder()
    {
        CleanupCombatants();
        _combatants.Clear();

        // A�adir personajes de la party
        if (currentPlayerPartyData != null)
        {
            for (int i = 0; i < currentPlayerPartyData.Count; i++)
            {
                if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && currentPlayerPartyData[i] != null)
                {
                    Character character = currentPlayerPartyData[i];
                    GameObject partyMemberSpriteGO = new GameObject("PartyCombatSprite_" + character.characterName);
                    partyMemberSpriteGO.transform.position = partySpawnPoints[i].position;
                    if (currentCombatArenaGameObject != null) partyMemberSpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);

                    SpriteRenderer sr = partyMemberSpriteGO.AddComponent<SpriteRenderer>();
                    sr.sprite = character.portraitSprite;
                    sr.sortingLayerName = "Characters_Combat";

                    Animator pAnim = partyMemberSpriteGO.AddComponent<Animator>();
                    // --- USAR ANIMATOR CONTROLLER DEL CHARACTER.CS ---
                    // --- USAR ANIMATOR CONTROLLER DEL CHARACTER.CS O EL FALLBACK ---
                    if (character.combatAnimatorController != null)
                    {
                        pAnim.runtimeAnimatorController = character.combatAnimatorController;
                    }
                    else if (playerFallbackAnimatorController != null) // Usar el fallback si el espec�fico no est�
                    {
                        pAnim.runtimeAnimatorController = playerFallbackAnimatorController;
                        Debug.LogWarning($"Personaje {character.characterName} no tiene un Combat Animator Controller espec�fico. Usando fallback del CombatManager.");
                    }
                    else
                    {
                        Debug.LogError($"Personaje {character.characterName} no tiene Combat Animator Controller espec�fico NI hay un Fallback asignado en CombatManager.");
                    }
                    // --- FIN USO ANIMATOR ---

                    BoxCollider2D partyCol = partyMemberSpriteGO.AddComponent<BoxCollider2D>();
                    if (sr.sprite != null) partyCol.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.8f);
                    else partyCol.size = new Vector2(0.5f, 0.5f);

                    partyMemberSpriteGO.AddComponent<CombatSpriteEventHandler>();

                    _partyCombatSpriteGOs.Add(partyMemberSpriteGO);
                    _combatants.Add(new Combatant(character, partyMemberSpriteGO));
                }
            }
        }

        // A�adir enemigos
        if (currentEnemyGroupData != null)
        {
            for (int i = 0; i < currentEnemyGroupData.Count; i++)
            {
                if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null && currentEnemyGroupData[i] != null)
                {
                    EnemyData enemy = currentEnemyGroupData[i];
                    GameObject enemySpriteGO = new GameObject("EnemyCombatSprite_" + enemy.enemyName);
                    enemySpriteGO.transform.position = enemySpawnPoints[i].position;
                    if (currentCombatArenaGameObject != null) enemySpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);
                    SpriteRenderer sr = enemySpriteGO.AddComponent<SpriteRenderer>();
                    sr.sprite = enemy.battleSprite;
                    sr.sortingLayerName = "Characters_Combat";

                    Animator eAnim = enemySpriteGO.AddComponent<Animator>();
                    // --- USAR ANIMATOR CONTROLLER DEL ENEMYDATA.CS ---
                    if (enemy.combatAnimatorController != null)
                    {
                        eAnim.runtimeAnimatorController = enemy.combatAnimatorController;
                    }
                    else
                    {
                        Debug.LogWarning($"Enemigo {enemy.enemyName} no tiene un Combat Animator Controller asignado en su EnemyData asset.");
                        // Opcional: Asignar un controller por defecto si no se encontr� uno espec�fico
                        // if (defaultEnemyCombatAnimatorController_DEPRECATED != null) eAnim.runtimeAnimatorController = defaultEnemyCombatAnimatorController_DEPRECATED;
                    }
                    // --- FIN USO ANIMATOR DEL ENEMYDATA ---

                    BoxCollider2D col = enemySpriteGO.AddComponent<BoxCollider2D>();

                    col.isTrigger = true;

                    if (sr.sprite != null) col.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.8f);

                    else col.size = new Vector2(0.5f, 0.5f);

                    enemySpriteGO.layer = LayerMask.NameToLayer("EnemiesInCombat");

                    _enemyCombatSpriteGOs.Add(enemySpriteGO);
� � � � � � � � � � // --- A�ADIR CombatSpriteEventHandler A ENEMIGOS (si fueran a lanzar proyectiles por evento) --
� � � � � � � � � � enemySpriteGO.AddComponent<CombatSpriteEventHandler>();

� � � � � � � � � � // --- FIN ---

� � � � � � � � � � EnemyCombatStatusUI statusUIInstance = null;

                    if (enemyStatusUIPrefab != null)
                    {
                        GameObject enemyStatusUIGO = Instantiate(enemyStatusUIPrefab, enemySpriteGO.transform);
                        enemyStatusUIGO.transform.localPosition = new Vector3(0, enemyHPBarOffsetY, 0);
                        statusUIInstance = enemyStatusUIGO.GetComponent<EnemyCombatStatusUI>();
                        if (statusUIInstance == null) Debug.LogError("El prefab enemyStatusUIPrefab no tiene el script EnemyCombatStatusUI.", enemyStatusUIPrefab);

                    }
                    Combatant enemyCombatant = new Combatant(currentEnemyGroupData[i], enemySpriteGO, statusUIInstance);
                    _combatants.Add(enemyCombatant);
                    if (statusUIInstance != null) statusUIInstance.SetupEnemyStatus(enemyCombatant);
                }
            }
        }
        _combatants = _combatants.OrderByDescending(c => c.speed).ToList();
        _currentCombatantIndex = -1;
    }

    private void CleanupCombatants()
    {
        foreach (GameObject go in _partyCombatSpriteGOs) if (go != null) Destroy(go);
        _partyCombatSpriteGOs.Clear();
        foreach (GameObject go in _enemyCombatSpriteGOs) if (go != null) Destroy(go);
        _enemyCombatSpriteGOs.Clear();
    }

    private void PopulatePartyStatusUI()
    {
        if (partyStatusAreaContainer == null || partyMemberStatusUIPrefab == null) return;
        foreach (PartyMemberCombatStatusUI ui in _partyStatusUIs) if (ui != null) Destroy(ui.gameObject);
        _partyStatusUIs.Clear();
        if (currentPlayerPartyData == null || currentPlayerPartyData.Count == 0) return;

        foreach (Character partyMember in currentPlayerPartyData)
        {
            if (partyMember == null) continue;
            GameObject statusGO = Instantiate(partyMemberStatusUIPrefab, partyStatusAreaContainer);
            PartyMemberCombatStatusUI statusUI = statusGO.GetComponent<PartyMemberCombatStatusUI>();
            if (statusUI != null)
            {
                statusUI.SetupStatus(partyMember);
                _partyStatusUIs.Add(statusUI);
            }
            else Destroy(statusGO);
        }
    }

    private void CleanupPartyStatusUI()
    {
        foreach (PartyMemberCombatStatusUI ui in _partyStatusUIs) if (ui != null) Destroy(ui.gameObject);
        _partyStatusUIs.Clear();
    }

    public void UpdatePartyStatusHUD()
    {
        if (_partyStatusUIs == null) return;
        foreach (PartyMemberCombatStatusUI statusUI in _partyStatusUIs)
        {
            if (statusUI != null && statusUI.gameObject != null && statusUI.gameObject.activeInHierarchy)
            {
                statusUI.UpdateUIElements();
            }
        }
    }

    private void NextTurn()
    {
        Debug.Log($"[{Time.frameCount}] NextTurn: INICIO. isCombatActive: {isCombatActive}"); // Log de tu versi�n
        if (!isCombatActive)
        {
            Debug.LogWarning($"[{Time.frameCount}] NextTurn: Combate NO activo. Retornando.");
            return;
        }

        if (CheckCombatEndConditions()) // CheckCombatEndConditions llama a EndCombat si se cumplen
        {
            Debug.Log($"[{Time.frameCount}] NextTurn: CheckCombatEndConditions() devolvi� true (combate terminado). Retornando.");
            return; // El combate ha terminado
        }

        Debug.Log($"[{Time.frameCount}] NextTurn: El combate contin�a. Buscando siguiente combatiente.");

        _currentCombatantIndex++;
        if (_currentCombatantIndex >= _combatants.Count)
        {
            _currentCombatantIndex = 0;
            _currentRoundNumber++; // Incrementar el n�mero de ronda
            Debug.Log($"[{Time.frameCount}] NextTurn: ----- NUEVA RONDA ({_currentRoundNumber}) -----");

            // --- MOSTRAR T�TULO DE RONDA ---
            if (roundTitleText != null)
            {
                if (_roundTitleCoroutine != null) StopCoroutine(_roundTitleCoroutine); // Detener corrutina anterior si existe
                _roundTitleCoroutine = StartCoroutine(ShowRoundTitleCoroutine($"RONDA {_currentRoundNumber}"));
            }
            // --- FIN MOSTRAR T�TULO ---

            foreach (Combatant combatant in _combatants.Where(c => c != null)) // A�adido check de null por seguridad
            {
                if (combatant.isDefending) combatant.StopDefending();
            }
            UpdatePartyStatusHUD();
        }
        else if (_currentCombatantIndex == 0 && _currentRoundNumber == 0) // Condici�n para el primer turno del combate
        {
            _currentRoundNumber = 1; // La primera ronda es la 1
            Debug.Log($"[{Time.frameCount}] NextTurn: ----- INICIO COMBATE - RONDA {_currentRoundNumber} -----");
            if (roundTitleText != null)
            {
                if (_roundTitleCoroutine != null) StopCoroutine(_roundTitleCoroutine);
                _roundTitleCoroutine = StartCoroutine(ShowRoundTitleCoroutine($"RONDA {_currentRoundNumber}"));
            }
        }

        // Asegurarse de que _combatants no sea nulo o vac�o despu�s de la l�gica de ronda
        if (_combatants == null || _combatants.Count == 0)
        {
            Debug.LogError($"[{Time.frameCount}] NextTurn: Lista de combatientes vac�a o nula inesperadamente DESPU�S de procesar ronda. Terminando combate.");
            EndCombat(false);
            return;
        }
        // Asegurarse de que el �ndice es v�lido despu�s de potencialmente resetearlo
        if (_currentCombatantIndex < 0 || _currentCombatantIndex >= _combatants.Count)
        {
            Debug.LogError($"[{Time.frameCount}] NextTurn: �ndice de combatiente inv�lido ({_currentCombatantIndex}) DESPU�S de procesar ronda. Reseteando a 0.");
            _currentCombatantIndex = 0;
            if (_combatants.Count == 0) { EndCombat(false); return; } // Doble check por si acaso
        }

        _activeCombatant = _combatants[_currentCombatantIndex];

        if (_activeCombatant == null)
        { // Comprobar si _activeCombatant es nulo
            Debug.LogError($"[{Time.frameCount}] NextTurn: _activeCombatant es null en el �ndice {_currentCombatantIndex} ANTES de comprobar isDefeated. Saltando turno e investigando.");
            NextTurn(); // Intentar con el siguiente, pero esto podr�a indicar un problema m�s profundo
            return;
        }

        if (_activeCombatant.isDefeated)
        {
            Debug.Log($"[{Time.frameCount}] NextTurn: {_activeCombatant.GetName()} est� derrotado. Saltando turno.");
            NextTurn();
            return;
        }

        Debug.Log($"[{Time.frameCount}] NextTurn: FIN. Llamando a StartTurnForActiveCombatant() para {_activeCombatant.GetName()}.");
        StartTurnForActiveCombatant();
    }

    private void StartTurnForActiveCombatant()
    {
        if (_activeCombatant == null || _activeCombatant.isDefeated) { NextTurn(); return; }
        Debug.Log($"CombatManager: Iniciando turno para {_activeCombatant.GetName()} (HP: {_activeCombatant.GetCurrentHP()}/{_activeCombatant.GetMaxHP()}, Defensa: {_activeCombatant.GetDefense()}, Defendiendo: {_activeCombatant.isDefending})");
        // Doble check para asegurar que el combate no termin� justo antes
        if (!isCombatActive)
        {
            Debug.LogWarning($"[{Time.frameCount}] StartTurnForActiveCombatant: El combate termin� antes de iniciar el turno para {_activeCombatant.GetName()}. No se mostrar� el men� de acci�n.");
            return;
        }
        if (_activeCombatant.isPlayerCharacter)
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(true);
        }
        else
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            StartCoroutine(EnemyTurnCoroutine(_activeCombatant));
        }
    }

    private IEnumerator EnemyTurnCoroutine(Combatant enemy)
    {
        Debug.Log($"[{Time.frameCount}] EnemyTurnCoroutine: {enemy.GetName()} est� pensando...");
        yield return new WaitForSeconds(1.0f);

        if (enemy.isDefeated) { NextTurn(); yield break; }
        if (enemy.isDefending) enemy.StopDefending();

        List<Combatant> livingPlayerCombatants = _combatants.Where(c => c.isPlayerCharacter && !c.isDefeated).ToList();
        if (livingPlayerCombatants.Count == 0)
        {
            Debug.Log($"[{Time.frameCount}] EnemyTurnCoroutine: {enemy.GetName()} no tiene objetivos vivos.");
            NextTurn();
            yield break;
        }

        // --- L�GICA DE DECISI�N DE LA IA ---
        bool useAbility = false;
        if (enemy.enemyData.isBoss && enemy.enemyData.abilities.Count > 0)
        {
            // Decisi�n simple: 50% de probabilidad de usar habilidad si es un jefe con habilidades
            if (Random.value > 0.5f)
            {
                useAbility = true;
            }
        }

        if (useAbility)
        {
            // --- USAR UNA HABILIDAD ---
            // 1. Elegir una habilidad al azar de la lista
            AbilityData chosenAbility = enemy.enemyData.abilities[Random.Range(0, enemy.enemyData.abilities.Count)];
            Debug.Log($"[{Time.frameCount}] IA Enemiga: {enemy.GetName()} ha decidido usar la habilidad '{chosenAbility.abilityName}'.");

            // 2. Determinar el objetivo para la habilidad
            Combatant abilityTarget = null;
            if (chosenAbility.targetType == AbilityTargetType.SingleEnemy)
            {
                abilityTarget = livingPlayerCombatants[Random.Range(0, livingPlayerCombatants.Count)];
            }
            // (Si tuviera habilidades de curaci�n de aliados enemigos, aqu� ir�a la l�gica para SingleAlly)

            // 3. Ejecutar la habilidad (la l�gica de la corrutina maneja animaci�n, espera y efectos)
            // NOTA: Como la ejecuci�n de la habilidad del jugador, esto es "dispara y olvida".
            // EnemyTurnCoroutine terminar�, pero PerformSkillSequence se seguir� ejecutando.
            StartCoroutine(PerformSkillSequenceForEnemy(enemy, abilityTarget, chosenAbility));

        }
        else
        {
            // --- USAR UN ATAQUE B�SICO ---
            Debug.Log($"[{Time.frameCount}] IA Enemiga: {enemy.GetName()} ha decidido usar un ataque b�sico.");
            Combatant target = livingPlayerCombatants[Random.Range(0, livingPlayerCombatants.Count)];

            // Iniciar corrutina para el ataque b�sico del enemigo
            StartCoroutine(PerformBasicAttackForEnemy(enemy, target));
        }
    }

    // Corrutina espec�fica para el ataque b�sico de un enemigo
    private IEnumerator PerformBasicAttackForEnemy(Combatant attacker, Combatant target)
    {
        PlaySoundEffect(attacker.enemyData.basicAttackSound);
        if (attacker.animator != null)
        {
            attacker.animator.SetTrigger("AttackTrigger");
            yield return new WaitForSeconds(enemyAttackAnimationDuration);
        }

        if (directHitVFXPrefab != null && target.combatSpriteGO != null)
        {
            Instantiate(directHitVFXPrefab, target.combatSpriteGO.transform.position + new Vector3(0, directHitVFX_Y_Offset, 0), Quaternion.identity);
        }

        int damage = Mathf.Max(1, attacker.GetAttack() - target.GetDefense());
        target.TakeDamage(damage);

        yield return new WaitForSeconds(0.5f);
        NextTurn();
    }

    // Corrutina espec�fica para la habilidad de un enemigo
    private IEnumerator PerformSkillSequenceForEnemy(Combatant attacker, Combatant directTarget, AbilityData skill)
    {
        // Esta corrutina es casi id�ntica a PerformSkillSequence del jugador, pero simplificada
        // ya que los enemigos no usan el sistema de eventos de animaci�n del jugador ni tienen coste de MP.

        // 1. Reproducir sonido y animaci�n del lanzador
        PlaySoundEffect(skill.launchSound);
        float casterAnimationDuration = enemyAttackAnimationDuration; // Usar una duraci�n gen�rica de enemigo
        if (attacker.animator != null)
        {
            string triggerName = "AttackTrigger";
            if (!string.IsNullOrEmpty(skill.animationTriggerName)) triggerName = skill.animationTriggerName;
            attacker.animator.SetTrigger(triggerName);
            if (skill.animationDuration > 0.01f) casterAnimationDuration = skill.animationDuration;
        }
        yield return new WaitForSeconds(casterAnimationDuration);

        // 2. Determinar objetivos reales
        List<Combatant> actualTargets = DetermineActualTargets(skill.targetType, attacker, directTarget);

        // 3. Aplicar efecto (asumimos que las habilidades de enemigos no usan proyectiles por ahora, o son efectos directos)
        foreach (Combatant t in actualTargets)
        {
            if (t.isDefeated) continue;

            // --- MODIFICADO: L�gica de VFX de impacto para enemigos ---
            GameObject vfxToInstantiate = null;
            // Priorizar el VFX espec�fico de la habilidad (si no es un proyectil)
            if (skill.vfxPrefab != null && skill.vfxPrefab.GetComponent<Projectile>() == null)
            {
                vfxToInstantiate = skill.vfxPrefab;
                Debug.Log($"[{Time.frameCount}] Jefe usando VFX espec�fico de habilidad '{skill.abilityName}': {vfxToInstantiate.name}");
            }
            else // Si no hay VFX espec�fico en la habilidad, usar el gen�rico de golpe directo
            {
                vfxToInstantiate = directHitVFXPrefab;
                if (vfxToInstantiate != null) Debug.Log($"[{Time.frameCount}] Jefe usando VFX gen�rico de golpe directo: {vfxToInstantiate.name}");
            }

            // Instanciar el VFX elegido sobre el objetivo
            if (vfxToInstantiate != null && t.combatSpriteGO != null && skill.effectType == AbilityEffectType.Damage)
            {
                Vector3 vfxPosition = t.combatSpriteGO.transform.position + new Vector3(0, directHitVFX_Y_Offset, 0);
                Instantiate(vfxToInstantiate, vfxPosition, Quaternion.identity);
            }
            // --- FIN MODIFICACI�N ---
            PlaySoundEffect(skill.impactSound);

            if (skill.effectType == AbilityEffectType.Damage)
            {
                int damage = Mathf.Max(1, (int)skill.power + (attacker.enemyData.baseMagicAttack / 2) - t.GetDefense());
                t.TakeDamage(damage);
            }
            // (A�adir l�gica para otros efectos de habilidad del enemigo)
        }

        // 4. Pasar al siguiente turno
        yield return new WaitForSeconds(0.5f);
        NextTurn();
    }

    public void OnAttackButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter || isSelectingTargetForAttack) return;
        Debug.Log($"{_activeCombatant.GetName()} seleccion� ATACAR. Por favor, selecciona un objetivo enemigo.");
        isSelectingTargetForAttack = true;
        attackerForTargetSelection = _activeCombatant;
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
    }

    public void OnDefendButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter || isSelectingTargetForAttack) return;
        _activeCombatant.StartDefending();
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.3f));
    }

    public void OnSkillsButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter ||
            isSelectingTargetForAttack || isSelectingSkill || isSelectingTargetForSkill) return;
        Debug.Log($"{_activeCombatant.GetName()} seleccion� HABILIDADES.");
        isSelectingSkill = true;
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        PopulateSkillListForActiveCombatant();
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(true);
    }

    private void PopulateSkillListForActiveCombatant()
    {
        if (skillListContainer == null || abilityListItemPrefab == null || _activeCombatant == null || _activeCombatant.characterData == null)
        {
            if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
            return;
        }
        foreach (Transform child in skillListContainer) Destroy(child.gameObject);
        _currentSkillListUIs.Clear();
        List<AbilityData> knownAbilities = _activeCombatant.characterData.knownAbilities;
        if (knownAbilities == null || knownAbilities.Count == 0) return;

        foreach (AbilityData ability in knownAbilities)
        {
            if (ability == null) continue;
            GameObject listItemGO = Instantiate(abilityListItemPrefab, skillListContainer);
            AbilityListItem_UI listItemUI = listItemGO.GetComponent<AbilityListItem_UI>();
            if (listItemUI != null)
            {
                listItemUI.SetupAbilityItem(ability, this.OnSkillSelectedFromList);
                _currentSkillListUIs.Add(listItemUI);
            }
            else Destroy(listItemGO);
        }
    }

    public void OnSkillSelectedFromList(AbilityData ability)
    {
        if (!isSelectingSkill || ability == null || _activeCombatant == null) return;
        _selectedAbility = ability;
        if (_activeCombatant.characterData.currentMP < _selectedAbility.mpCost)
        {
            CloseSkillSelectionPanel();
            return;
        }
        isSelectingSkill = false;
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        switch (_selectedAbility.targetType)
        {
            case AbilityTargetType.Self:
            case AbilityTargetType.AllAllies:
            case AbilityTargetType.AllEnemies:
                ExecuteSkill(_activeCombatant, null, _selectedAbility);
                break;
            case AbilityTargetType.SingleAlly:
            case AbilityTargetType.SingleEnemy:
                isSelectingTargetForSkill = true;
                attackerForTargetSelection = _activeCombatant;
                Debug.Log($"Por favor, selecciona un objetivo para la habilidad: {_selectedAbility.abilityName} ({_selectedAbility.targetType})");
                break;
            default:
                NextTurn();
                break;
        }
    }

    public void CloseSkillSelectionPanel()
    {
        isSelectingSkill = false;
        isSelectingTargetForSkill = false;
        _selectedAbility = null;
        attackerForTargetSelection = null; // Asegurarse de resetear esto tambi�n por si acaso
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        if (actionMenuPanel != null && _activeCombatant != null && _activeCombatant.isPlayerCharacter)
        {
            actionMenuPanel.SetActive(true);
        }
    }
    private void ExecuteAttack(Combatant attacker, Combatant target)
    {
        if (attacker == null || target == null || target.isDefeated)
        {
            isSelectingTargetForAttack = false;
            if (attacker != null && attacker.isPlayerCharacter && actionMenuPanel != null) actionMenuPanel.SetActive(true);
            return;
        }

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        StartCoroutine(PerformAttackSequence(attacker, target));
    }

    private IEnumerator PerformAttackSequence(Combatant attacker, Combatant target)
    {
        Debug.Log($"[{Time.frameCount}] {attacker.GetName()} inicia secuencia de ATAQUE B�SICO sobre {target.GetName()}!");

        float animationDuration = playerGenericAnimationDuration;
        if (attacker.isPlayerCharacter && attacker.characterData != null && attacker.characterData.combatAnimatorController != null)
        {
            // Aqu� podr�as obtener una duraci�n espec�fica del ataque b�sico si la tuvieras en Character.cs o su AnimatorController
            // Por ahora, se usa playerGenericAnimationDuration para todos los ataques b�sicos del jugador.
        }

        // Preparar el EventHandler para el posible lanzamiento del proyectil por evento de animaci�n
        CombatSpriteEventHandler eventHandler = null;
        if (attacker.combatSpriteGO != null)
        {
            eventHandler = attacker.combatSpriteGO.GetComponent<CombatSpriteEventHandler>();
        }

        GameObject projectilePrefabFromCharacter = null;
        if (attacker.isPlayerCharacter && attacker.characterData != null)
        {
            projectilePrefabFromCharacter = attacker.characterData.basicAttackProjectilePrefab; // Obtener de Character.cs
        }

        bool isBasicAttackWithProjectile = projectilePrefabFromCharacter != null && projectilePrefabFromCharacter.GetComponent<Projectile>() != null;

        if (isBasicAttackWithProjectile && eventHandler != null)
        {
            int basicAttackDamage = Mathf.Max(1, attacker.GetAttack() - target.GetDefense());
            eventHandler.SetupForBasicAttackProjectileLaunch(attacker, target, projectilePrefabFromCharacter, basicAttackDamage);
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()}: EventHandler configurado para proyectil de ataque b�sico '{projectilePrefabFromCharacter.name}'.");
        }
        else if (isBasicAttackWithProjectile && eventHandler == null)
        {
            Debug.LogWarning($"[{Time.frameCount}] {attacker.GetName()}: CombatSpriteEventHandler no encontrado. Proyectil de ataque b�sico no se lanzar� por evento. Aplicando da�o directo.");
            isBasicAttackWithProjectile = false; // Forzar da�o directo si el handler es nulo
        }
        else if (attacker.isPlayerCharacter && projectilePrefabFromCharacter != null && projectilePrefabFromCharacter.GetComponent<Projectile>() == null)
        {
            Debug.LogWarning($"[{Time.frameCount}] {attacker.GetName()}: El 'basicAttackProjectilePrefab' ({projectilePrefabFromCharacter.name}) no tiene script Projectile. Aplicando da�o directo.");
            isBasicAttackWithProjectile = false; // Forzar da�o directo
        }
        // --- REPRODUCIR SONIDO DE ATAQUE B�SICO DEL JUGADOR ---
        if (attacker.isPlayerCharacter && attacker.characterData != null)
        {
            PlaySoundEffect(attacker.characterData.basicAttackSound);
        }
        // --- FIN SONIDO ---


        // Disparar Animaci�n de Ataque del Atacante
        if (attacker.animator != null)
        {
            attacker.animator.SetTrigger("AttackTrigger");
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()} activando AttackTrigger para ataque b�sico. Esperando {animationDuration}s.");
            yield return new WaitForSeconds(animationDuration);
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()} termin� espera de animaci�n de ataque b�sico.");
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Si NO fue un proyectil lanzado por evento (o no se pudo configurar/no existe), aplicar da�o directo.
        // Esto cubrir� al aventurero (si su characterData.basicAttackProjectilePrefab es null)
        // y a los enemigos (que no usan esta l�gica de proyectil para ataque b�sico por ahora).
        if (!isBasicAttackWithProjectile)
        {
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()}: No se lanz� proyectil (o no era aplicable/v�lido), llamando a ApplyDirectDamage.");
            ApplyDirectDamage(attacker, target);
        }
        else
        {
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()}: Ataque b�sico es de tipo proyectil. El da�o se aplicar� al impacto del proyectil (lanzado por evento de animaci�n).");
        }

        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;
        StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.2f));
    }

    private void ApplyDirectDamage(Combatant attacker, Combatant target)
    {
        if (target.isDefeated) return;
        Debug.Log($"[{Time.frameCount}] ApplyDirectDamage: Aplicando da�o de {attacker.GetName()} a {target.GetName()}. Defensa: {target.GetDefense()}, Ataque: {attacker.GetAttack()}");
        // --- INSTANCIAR VFX DE GOLPE DIRECTO EN EL OBJETIVO ---
        if (directHitVFXPrefab != null && target.combatSpriteGO != null)
        {
            Vector3 vfxPosition = target.combatSpriteGO.transform.position + new Vector3(0, directHitVFX_Y_Offset, 0);
            Instantiate(directHitVFXPrefab, vfxPosition, Quaternion.identity);
            Debug.Log($"[{Time.frameCount}] Instanciado directHitVFXPrefab en {target.GetName()}");
        }
        // --- FIN INSTANCIAR VFX ---

        int damage = Mathf.Max(1, attacker.GetAttack() - target.GetDefense());
        target.TakeDamage(damage);
    }

    private void ExecuteSkill(Combatant attacker, Combatant directTarget, AbilityData skill)
    {
        if (attacker == null || skill == null) { ResetSelectionStatesAndPassTurn(true); return; }
        if (attacker.isPlayerCharacter == false || attacker.characterData == null) { ResetSelectionStatesAndPassTurn(true); return; }

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);

        StartCoroutine(PerformSkillSequence(attacker, directTarget, skill));
    }

    private IEnumerator PerformSkillSequence(Combatant attacker, Combatant directTarget, AbilityData skill)
    {
        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Iniciando para '{skill.abilityName}' por '{attacker.GetName()}'.");

        if (skill.mpCost > 0)
        {
            if (!attacker.characterData.SpendMana(skill.mpCost))
            {
                Debug.LogWarning($"[{Time.frameCount}] PerformSkillSequence: MP insuficiente para '{skill.abilityName}'.");
                ResetSelectionStatesAndPassTurn(true);
                yield break;
            }
            UpdatePartyStatusHUD();
        }
        // --- REPRODUCIR SONIDO DE LANZAMIENTO DE HABILIDAD ---
        PlaySoundEffect(skill.launchSound);
        // --- FIN SONIDO ---

        float casterAnimationDuration = playerGenericAnimationDuration;
        CombatSpriteEventHandler eventHandler = null;
        if (attacker.combatSpriteGO != null) eventHandler = attacker.combatSpriteGO.GetComponent<CombatSpriteEventHandler>();

        List<Combatant> actualTargets = DetermineActualTargets(skill.targetType, attacker, directTarget);

        // Determinar si la habilidad va a lanzar un proyectil a trav�s de un evento de animaci�n
        bool isSkillAProjectileLaunchedByEvent = skill.vfxPrefab != null && skill.vfxPrefab.GetComponent<Projectile>() != null && eventHandler != null;

        if (isSkillAProjectileLaunchedByEvent)
        {
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Configurando EventHandler para proyectil de '{skill.abilityName}'.");
            eventHandler.SetupForSkillProjectileLaunch(attacker, actualTargets, skill, skill.vfxPrefab);
        }
        else if (skill.vfxPrefab != null && skill.vfxPrefab.GetComponent<Projectile>() != null && eventHandler == null)
        {
            Debug.LogWarning($"[{Time.frameCount}] PerformSkillSequence: CombatSpriteEventHandler no encontrado en {attacker.GetName()} para lanzar proyectil de {skill.abilityName}. Se intentar� aplicar efecto directo si es posible.");
        }

        // Disparar Animaci�n de Lanzamiento del Personaje
        if (attacker.animator != null)
        {
            string triggerName = "AttackTrigger";
            if (!string.IsNullOrEmpty(skill.animationTriggerName)) triggerName = skill.animationTriggerName;
            attacker.animator.SetTrigger(triggerName);
            if (skill.animationDuration > 0.01f) casterAnimationDuration = skill.animationDuration;
        }
        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Animaci�n del lanzador '{attacker.animator?.GetCurrentAnimatorClipInfo(0)[0].clip.name}' activada, esperando {casterAnimationDuration}s.");
        yield return new WaitForSeconds(casterAnimationDuration);
        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Fin de espera de animaci�n del lanzador para '{skill.abilityName}'.");

        // --- A�ADIR ESPERA ADICIONAL SI FUE UN PROYECTIL LANZADO POR EVENTO ---
        if (isSkillAProjectileLaunchedByEvent)
        {
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Habilidad '{skill.abilityName}' es de tipo proyectil. Esperando {skillProjectileImpactDelay}s adicionales para impacto de proyectil(es).");
            yield return new WaitForSeconds(skillProjectileImpactDelay); // Esperar que los proyectiles impacten
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Fin de espera adicional para impacto de proyectil de habilidad.");
        }
        else // Si NO es un proyectil lanzado por evento, aplicar efectos directos ahora
        {
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Aplicando efecto directo para '{skill.abilityName}'.");
            if (actualTargets.Count == 0 && skill.targetType != AbilityTargetType.None && skill.targetType != AbilityTargetType.Self)
            {
                Debug.LogWarning($"[{Time.frameCount}] PerformSkillSequence: No se encontraron objetivos v�lidos para efecto directo de {skill.abilityName}.");
            }
            else
            {
                if (skill.targetType == AbilityTargetType.Self && actualTargets.Count == 0 && attacker != null && !attacker.isDefeated) actualTargets.Add(attacker);

                Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Aplicando efecto directo a {actualTargets.Count} objetivo(s).");
                foreach (Combatant t in actualTargets)
                {
                    if (t.isDefeated && skill.effectType != AbilityEffectType.Special) continue;

                    // --- L�GICA DE VFX DE IMPACTO MODIFICADA ---
                    GameObject vfxToInstantiate = null;
                    // Priorizar el VFX espec�fico de la habilidad (si no es un proyectil)
                    if (skill.vfxPrefab != null && skill.vfxPrefab.GetComponent<Projectile>() == null)
                    {
                        vfxToInstantiate = skill.vfxPrefab;
                        Debug.Log($"[{Time.frameCount}] Usando VFX espec�fico de habilidad '{skill.abilityName}': {vfxToInstantiate.name}");
                    }
                    else // Si no hay VFX espec�fico en la habilidad, usar el gen�rico de golpe directo
                    {
                        vfxToInstantiate = directHitVFXPrefab;
                        if (vfxToInstantiate != null) Debug.Log($"[{Time.frameCount}] Usando VFX gen�rico de golpe directo: {vfxToInstantiate.name}");
                    }

                    // Instanciar el VFX elegido sobre el objetivo
                    if (vfxToInstantiate != null && t.combatSpriteGO != null)
                    {
                        Vector3 vfxPosition = t.combatSpriteGO.transform.position + new Vector3(0, directHitVFX_Y_Offset, 0);
                        Instantiate(vfxToInstantiate, vfxPosition, Quaternion.identity);
                    }
                    // --- FIN L�GICA VFX ---
                    PlaySoundEffect(skill.impactSound);
                    // --- FIN SONIDO ---

                    if (skill.effectType == AbilityEffectType.Damage)
                    {
                        int damage = Mathf.Max(1, (int)skill.power + (attacker.characterData.MagicAttack / 2) - t.GetDefense());
                        t.TakeDamage(damage);
                    }
                    else if (skill.effectType == AbilityEffectType.Heal) { t.ApplyHeal((int)skill.power); }
                    else if (skill.effectType == AbilityEffectType.RestoreMP) { if (t.isPlayerCharacter && t.characterData != null) t.ApplyManaRestore((int)skill.power); }
                }
            }
        }

        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Esperando fin de frame ANTES de ResetSelectionStatesAndPassTurn para '{skill.abilityName}'.");
        yield return null;

        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Fin de la secuencia para '{skill.abilityName}'. Llamando a ResetSelectionStatesAndPassTurn.");
        ResetSelectionStatesAndPassTurn();
    }

    private IEnumerator EndPlayerActionAndProceedToNextTurn(float delay)
    {
        Debug.Log($"[{Time.frameCount}] CombatManager: EndPlayerActionAndProceedToNextTurn - Esperando {delay}s.");
        if (delay > 0) yield return new WaitForSeconds(delay);
        else yield return null; // Esperar al menos un frame si el delay es 0

        Debug.Log($"[{Time.frameCount}] CombatManager: EndPlayerActionAndProceedToNextTurn - Fin de espera. Llamando a NextTurn(). isCombatActive: {isCombatActive}");
        if (isCombatActive) // Solo llamar a NextTurn si el combate sigue activo (ej: no termin� por CheckCombatEndConditions)
        {
            NextTurn();
        }
        else
        {
            Debug.LogWarning($"[{Time.frameCount}] CombatManager: EndPlayerActionAndProceedToNextTurn - Combate NO est� activo, no se llama a NextTurn().");
        }
    }

    private bool CheckCombatEndConditions()
    {
        Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: Verificando si el combate ha terminado.");
        if (_combatants == null || _combatants.Count == 0) { EndCombat(false); return true; }

        bool allEnemiesDefeated = _combatants.Where(c => c != null && !c.isPlayerCharacter).All(e => e.isDefeated);
        int enemyCount = _combatants.Count(c => c != null && !c.isPlayerCharacter);
        // foreach (Combatant enemy in _combatants.Where(c => !c.isPlayerCharacter && c != null))
        // { Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions - Enemigo: {enemy.GetName()}, HP: {enemy.GetCurrentHP()}, Derrotado: {enemy.isDefeated}"); }
        // Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: Enemigos totales: {enemyCount}. allEnemiesDefeated: {allEnemiesDefeated}");

        if (enemyCount > 0 && allEnemiesDefeated)
        {
            Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: �Todos los enemigos derrotados! Jugador gana.");
            EndCombat(true);
            return true;
        }

        bool allPlayersDefeated = _combatants.Where(c => c != null && c.isPlayerCharacter).All(p => p.isDefeated);
        int playerCount = _combatants.Count(c => c != null && c.isPlayerCharacter);
        // Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: Jugadores totales: {playerCount}. allPlayersDefeated: {allPlayersDefeated}");

        if (playerCount > 0 && allPlayersDefeated)
        {
            Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: �Todos los jugadores derrotados! Jugador pierde.");
            EndCombat(false);
            return true;
        }

        Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: El combate contin�a.");
        return false;
    }

    // --- MANEJADORES Y L�GICA PARA LA ACCI�N "OBJETOS" ---
    public void OnItemsButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter ||
            isSelectingTargetForAttack || isSelectingSkill || isSelectingTargetForSkill ||
            isSelectingItem || isSelectingTargetForItem) // Comprobar todos los estados de selecci�n
        {
            return;
        }

        Debug.Log($"{_activeCombatant.GetName()} seleccion� OBJETOS.");
        isSelectingItem = true;

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        PopulateCombatItemList();
        if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(true);
    }

    private void PopulateCombatItemList()
    {
        if (itemListContainer_Combat == null || combatItemListItemPrefab == null || PlayerInventory.Instance == null)
        {
            Debug.LogError("CombatManager: No se puede poblar la lista de objetos. Faltan referencias.");
            if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);
            return;
        }

        foreach (Transform child in itemListContainer_Combat) Destroy(child.gameObject);
        _currentCombatItemListUIs.Clear();

        // Filtrar por objetos que sean consumibles y tengan un efecto de uso definido
        var usableItems = PlayerInventory.Instance.inventorySlots
            .Where(slot => slot.item != null && slot.item.isConsumable &&
                           (slot.item.hpToRestore > 0 || slot.item.mpToRestore > 0 /*|| otros efectos de itemData.Use()*/ ))
            .ToList();

        if (usableItems.Count == 0)
        {
            Debug.Log("No hay objetos usables en combate en el inventario.");
            // (FUTURO: Mostrar mensaje en la UI de objetos "Sin objetos usables")
            // Considerar llamar a CloseItemSelectionPanel() para volver al men� de acci�n si est� vac�o
            // CloseItemSelectionPanel(); 
            return;
        }

        foreach (InventorySlot invSlot in usableItems)
        {
            GameObject listItemGO = Instantiate(combatItemListItemPrefab, itemListContainer_Combat);
            CombatItemListItem_UI listItemUI = listItemGO.GetComponent<CombatItemListItem_UI>();
            if (listItemUI != null)
            {
                listItemUI.SetupItem(invSlot.item, invSlot.quantity, this.OnCombatItemSelected);
                _currentCombatItemListUIs.Add(listItemUI);
            }
            else
            {
                Debug.LogError("CombatManager: El prefab 'combatItemListItemPrefab' no tiene el componente CombatItemListItem_UI.", this);
                Destroy(listItemGO);
            }
        }
    }

    public void OnCombatItemSelected(ItemData selectedItem)
    {
        if (!isSelectingItem || selectedItem == null || _activeCombatant == null || _activeCombatant.characterData == null) return;

        Debug.Log($"{_activeCombatant.GetName()} seleccion� el objeto: {selectedItem.itemName}");
        _selectedItemData = selectedItem;
        isSelectingItem = false;
        if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);

        // Determinar si el objeto necesita selecci�n de objetivo
        // Por ahora, asumimos que los consumibles de HP/MP se pueden usar en aliados/self
        if (selectedItem.itemType == ItemType.Consumable && (selectedItem.hpToRestore > 0 || selectedItem.mpToRestore > 0))
        {
            isSelectingTargetForItem = true;
            attackerForTargetSelection = _activeCombatant; // El personaje que usa el objeto
            Debug.Log($"Por favor, selecciona un objetivo para el objeto: {selectedItem.itemName} (Aliado).");
        }
        // (A�adir 'else if' para objetos de ataque a enemigos si los tienes, ej: bombas)
        // else if (selectedItem.itemType == ItemType.Bomb_Damage_Enemy) { 
        //     isSelectingTargetForItem = true; 
        //     attackerForTargetSelection = _activeCombatant;
        //     Debug.Log($"Por favor, selecciona un objetivo para el objeto: {selectedItem.itemName} (Enemigo).");
        // }
        else
        {
            // Si el objeto no necesita objetivo expl�cito o se usa sobre s� mismo por defecto
            ExecuteItem(_activeCombatant, _activeCombatant, _selectedItemData);
        }
    }

    public void CloseItemSelectionPanel()
    {
        isSelectingItem = false;
        isSelectingTargetForItem = false;
        _selectedItemData = null;
        // attackerForTargetSelection no se resetea aqu� necesariamente, podr�a ser �til si se cancela la selecci�n de objetivo

        if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);
        if (actionMenuPanel != null && _activeCombatant != null && _activeCombatant.isPlayerCharacter)
        {
            actionMenuPanel.SetActive(true);
        }
    }

    private void ExecuteItem(Combatant caster, Combatant directTarget, ItemData item)
    {
        if (caster == null || item == null) { ResetSelectionStatesAndPassTurn(); return; }
        if (caster.isPlayerCharacter == false || caster.characterData == null) { ResetSelectionStatesAndPassTurn(); return; }

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false); // Ocultar men�

        StartCoroutine(PerformItemUseSequence(caster, directTarget, item));
    }

    private IEnumerator PerformItemUseSequence(Combatant caster, Combatant directTarget, ItemData item)
    {
        Combatant actualTarget = DetermineItemTarget(item, caster, directTarget);
        if (actualTarget == null || (actualTarget.isPlayerCharacter && actualTarget.characterData == null))
        {
            Debug.LogWarning($"No se pudo aplicar {item.itemName}, objetivo no v�lido.");
            ResetSelectionStatesAndPassTurn(true);
            yield break;
        }

        Debug.Log($"{caster.GetName()} usa el objeto '{item.itemName}' sobre {actualTarget.GetName()}");
        // (FUTURO: Animaci�n de usar objeto para el caster)
        // if (caster.animator != null) caster.animator.SetTrigger("UseItemTrigger");
        // yield return new WaitForSeconds(playerItemAnimationDuration); // Necesitar�as esta variable

        bool itemUsedSuccessfully = false;
        if (item.Use(actualTarget.characterData))
        {
            itemUsedSuccessfully = true;
            if (item.hpToRestore > 0 && actualTarget.isPlayerCharacter) { ShowFloatingText("+" + item.hpToRestore, actualTarget.combatSpriteGO.transform.position, Color.green, actualTarget); }
            if (item.mpToRestore > 0 && actualTarget.isPlayerCharacter) { ShowFloatingText("+" + item.mpToRestore + " MP", actualTarget.combatSpriteGO.transform.position, Color.blue, actualTarget); }
        }

        if (itemUsedSuccessfully)
        {
            if (PlayerInventory.Instance != null) PlayerInventory.Instance.RemoveItem(item, 1);
            UpdatePartyStatusHUD();
        }
        else Debug.LogWarning($"{item.itemName} no pudo ser usado sobre {actualTarget.GetName()}.");

        ResetSelectionStatesAndPassTurn(!itemUsedSuccessfully);
    }

    public void ShowFloatingText(string text, Vector3 worldPosition, Color textColor, Combatant targetCombatant = null)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("CombatManager: floatingTextPrefab no asignado. No se puede mostrar texto flotante.");
            return;
        }

        GameObject textGO;
        Vector3 targetPosition = worldPosition; // Posici�n base del sprite del objetivo

        // Ajustar la posici�n Y para que el texto aparezca encima del pivote del sprite del objetivo
        if (targetCombatant != null && targetCombatant.combatSpriteGO != null)
        {
            SpriteRenderer sr = targetCombatant.combatSpriteGO.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // El offset se aplica sobre el centro del sprite si el pivote es central,
                // o sobre la base si el pivote est� en los pies.
                // floatingTextYOffset deber�a ser en unidades del mundo.
                targetPosition = targetCombatant.combatSpriteGO.transform.position + new Vector3(0, floatingTextYOffset, 0);
            }
        }
        else
        { // Si no hay combatiente espec�fico, usar la worldPosition directamente con el offset
            targetPosition = worldPosition + new Vector3(0, floatingTextYOffset, 0);
        }


        // Decidir el padre y la posici�n del texto flotante
        if (floatingTextCanvasTransform != null) // Si se asign� un Canvas padre (Screen Space)
        {
            textGO = Instantiate(floatingTextPrefab, floatingTextCanvasTransform);
            Canvas canvas = floatingTextCanvasTransform.GetComponent<Canvas>();
            if (canvas != null && Camera.main != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Vector2 screenPoint = Camera.main.WorldToScreenPoint(targetPosition);
                    textGO.transform.position = screenPoint;
                }
                else // ScreenSpaceCamera o WorldSpace (si el canvas padre es WorldSpace)
                {
                    textGO.transform.position = targetPosition;
                }
            }
            else
            { // Fallback si no hay canvas o c�mara
                textGO.transform.position = targetPosition;
            }
        }
        else if (targetCombatant != null && targetCombatant.combatSpriteGO != null &&
                 floatingTextPrefab.GetComponent<Canvas>() != null &&
                 floatingTextPrefab.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace)
        {
            // Si el PREFAB de texto flotante tiene su PROPIO Canvas en World Space, hacerlo hijo del target
            textGO = Instantiate(floatingTextPrefab, targetCombatant.combatSpriteGO.transform);
            // El localPosition se ajusta para que el pivote del texto quede en el offset Y deseado
            // Asumiendo que el pivote del texto flotante est� en su centro.
            textGO.transform.localPosition = new Vector3(0, floatingTextYOffset, 0);
        }
        else // Fallback: instanciar en el mundo sin padre espec�fico (requiere que el prefab tenga su propio Canvas WorldSpace)
        {
            textGO = Instantiate(floatingTextPrefab, targetPosition, Quaternion.identity);
        }


        FloatingCombatText floatingTextScript = textGO.GetComponent<FloatingCombatText>();
        if (floatingTextScript != null)
        {
            floatingTextScript.Init(text, textColor, floatingTextDefaultFontSize);
        }
        else
        {
            Debug.LogError("El prefab 'floatingTextPrefab' no tiene el script FloatingCombatText.", this);
            Destroy(textGO);
        }
    }

    private void ResetSelectionStatesAndPassTurn(bool reOpenActionMenu = false)
    {
        Debug.Log($"[{Time.frameCount}] CombatManager: ResetSelectionStatesAndPassTurn INICIO. Reabrir Men�: {reOpenActionMenu}");
        isSelectingItem = false;
        isSelectingTargetForItem = false;
        isSelectingSkill = false;
        isSelectingTargetForSkill = false;
        isSelectingTargetForAttack = false;
        _selectedItemData = null;
        _selectedAbility = null;
        attackerForTargetSelection = null;

        if (reOpenActionMenu && actionMenuPanel != null && _activeCombatant != null && _activeCombatant.isPlayerCharacter)
        {
            actionMenuPanel.SetActive(true);
            Debug.Log($"[{Time.frameCount}] CombatManager: ResetSelectionStatesAndPassTurn - Men� de acci�n reabierto.");
        }
        else
        {
            // El delay aqu� debe ser corto, solo para permitir que el frame actual termine si es necesario.
            // Si la acci�n ya incluy� una espera para animaci�n, este puede ser muy corto.
            StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.1f));
        }
        Debug.Log($"[{Time.frameCount}] CombatManager: ResetSelectionStatesAndPassTurn FIN.");
    }



    void Update()
    {
        if (!isCombatActive) return;

        // --- L�gica de Selecci�n de Objetivo para ATAQUE ---
        if (isSelectingTargetForAttack && Input.GetMouseButtonDown(0))
        {
            // Debug.Log("CombatManager: Clic detectado mientras isSelectingTargetForAttack es true.");
            if (Camera.main == null)
            {
                Debug.LogError("CombatManager: Camera.main es NULL. Aseg�rate de que tu c�mara de combate est� tageada como 'MainCamera'.");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("EnemiesInCombat"));

            if (hit.collider != null)
            {
                // Debug.Log($"CombatManager: Raycast (ataque) golpe� a '{hit.collider.gameObject.name}' en la capa '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject && !c.isPlayerCharacter);

                if (targetCombatant != null)
                {
                    Debug.Log("CombatManager: Objetivo enemigo v�lido encontrado para ATAQUE: " + targetCombatant.GetName());
                    // --- CORRECCI�N: Desactivar la selecci�n ANTES de ejecutar la acci�n ---
                    isSelectingTargetForAttack = false;
                    ExecuteAttack(attackerForTargetSelection, targetCombatant);
                    // attackerForTargetSelection se resetea dentro de PerformAttackSequence o su corolario
                }
                else
                {
                    Debug.LogWarning("CombatManager: Clic en un objeto con collider en la capa de enemigos, pero no es un combatiente enemigo v�lido/vivo.");
                    // No resetear isSelectingTargetForAttack aqu� para permitir otro intento si el jugador quiere
                    // Opcional: Podr�as querer que el jugador pueda cancelar la selecci�n aqu�
                    // ResetSelectionStatesAndPassTurn(true); // Esto volver�a al men� de acci�n
                }
            }
            // else { Debug.Log("Ataque: Clic en el vac�o."); } // Opcional: cancelar si se hace clic en el vac�o
        }
        // --- L�gica de Selecci�n de Objetivo para HABILIDAD ---
        else if (isSelectingTargetForSkill && Input.GetMouseButtonDown(0))
        {
            // Debug.Log("CombatManager: Clic detectado mientras isSelectingTargetForSkill es true.");
            if (Camera.main == null) { /* ... */ return; }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, combatantLayerMask);

            if (hit.collider != null)
            {
                // Debug.Log($"CombatManager: Raycast (habilidad) golpe� a '{hit.collider.gameObject.name}' en la capa '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject);

                if (targetCombatant != null && _selectedAbility != null)
                {
                    bool isValidTargetType = false;
                    switch (_selectedAbility.targetType)
                    {
                        case AbilityTargetType.SingleEnemy:
                            if (!targetCombatant.isPlayerCharacter) isValidTargetType = true;
                            break;
                        case AbilityTargetType.SingleAlly:
                            if (targetCombatant.isPlayerCharacter) isValidTargetType = true;
                            break;
                    }

                    if (isValidTargetType)
                    {
                        Debug.Log($"CombatManager: Objetivo '{targetCombatant.GetName()}' v�lido para la habilidad '{_selectedAbility.abilityName}'.");
                        // --- CORRECCI�N: Desactivar la selecci�n ANTES de ejecutar la habilidad ---
                        isSelectingTargetForSkill = false;
                        ExecuteSkill(attackerForTargetSelection, targetCombatant, _selectedAbility);
                        // attackerForTargetSelection y _selectedAbility se resetean en ResetSelectionStatesAndPassTurn
                    }
                    else
                    {
                        Debug.LogWarning($"CombatManager: Objetivo '{targetCombatant.GetName()}' NO es v�lido para la habilidad '{_selectedAbility.abilityName}'.");
                        // Considerar no cerrar el panel de skills inmediatamente, sino dar feedback y permitir reintentar o cancelar.
                        // Por ahora, la l�gica de ResetSelectionStatesAndPassTurn(true) se llamar� si ExecuteSkill no procede.
                        // Si se quiere cancelar expl�citamente aqu�:
                        // ResetSelectionStatesAndPassTurn(true); // Vuelve al men� de acci�n
                    }
                }
                // else { Debug.LogWarning("CombatManager: Clic en un objeto con collider, pero no es un combatiente v�lido/vivo o no hay habilidad seleccionada."); }
            }
            // else { Debug.Log("Habilidad: Clic en el vac�o."); } // Opcional
        }
        // --- L�gica de Selecci�n de Objetivo para OBJETOS ---
        else if (isSelectingTargetForItem && Input.GetMouseButtonDown(0))
        {
            // ... (l�gica similar, poner isSelectingTargetForItem = false; antes de ExecuteItem) ...
            if (Camera.main == null) { return; }
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, combatantLayerMask);

            if (hit.collider != null)
            {
                Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject);

                if (targetCombatant != null && _selectedItemData != null)
                {
                    bool isValidTarget = false;
                    if (_selectedItemData.itemType == ItemType.Consumable && (_selectedItemData.hpToRestore > 0 || _selectedItemData.mpToRestore > 0))
                    {
                        if (targetCombatant.isPlayerCharacter) isValidTarget = true;
                    }
                    // (A�adir validaci�n para otros tipos de �tems)

                    if (isValidTarget)
                    {
                        // --- CORRECCI�N: Desactivar la selecci�n ANTES de ejecutar el �tem ---
                        isSelectingTargetForItem = false;
                        ExecuteItem(attackerForTargetSelection, targetCombatant, _selectedItemData);
                    }
                    else
                    {
                        Debug.LogWarning($"Objetivo '{targetCombatant.GetName()}' NO es v�lido para el objeto '{_selectedItemData.itemName}'.");
                        ResetSelectionStatesAndPassTurn(true);
                    }
                }
            }
        }
        // Teclas de depuraci�n para terminar combate (solo si no estamos seleccionando nada)
        else if (!isSelectingTargetForAttack && !isSelectingTargetForSkill && !isSelectingTargetForItem)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) EndCombat(true);
            else if (Input.GetKeyDown(KeyCode.Alpha9)) EndCombat(false);
        }
    }
    public void OnFleeButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter ||
            isSelectingTargetForAttack || isSelectingSkill || isSelectingTargetForSkill ||
            isSelectingItem || isSelectingTargetForItem)
        {
            return; // No hacer nada si no es el momento adecuado
        }

        Debug.Log($"{_activeCombatant.GetName()} intenta HUIR.");

        if (actionMenuPanel != null)
        {
            actionMenuPanel.SetActive(false); // Ocultar men� inmediatamente
        }

        // Comprobar si se puede huir de este encuentro espec�fico
        if (_activeEncounter != null && !_activeEncounter.canFleeFromThisEncounter)
        {
            Debug.Log("�No se puede huir de este combate (Jefe)!");
            // (FUTURO: Mostrar mensaje en UI "�No puedes huir de este enemigo!")
            // El personaje pierde el turno
            StartCoroutine(ShowMessageAndEndPlayerTurn("�No puedes huir!", 1.5f));
            return;
        }

        // Calcular si la huida tiene �xito
        if (Random.value < fleeSuccessChance) // Random.value devuelve un float entre 0.0 (inclusive) y 1.0 (inclusive)
        {
            Debug.Log("�Huida exitosa!");
            // (FUTURO: Mostrar mensaje en UI "�Escapaste con �xito!")
            // Podr�as a�adir una peque�a pausa antes de llamar a EndCombat
            // StartCoroutine(DelayedEndCombat(false, 0.5f)); 
            EndCombat(false); // Terminar el combate, el jugador no "gana"
        }
        else
        {
            Debug.Log("�La huida fall�!");
            // (FUTURO: Mostrar mensaje en UI "La huida fall�...")
            // El personaje pierde el turno
            StartCoroutine(ShowMessageAndEndPlayerTurn("�La huida fall�!", 1.5f));
        }
    }
    private IEnumerator ShowMessageAndEndPlayerTurn(string message, float delay)
    {
        // (FUTURO: Aqu� mostrar�as 'message' en una UI temporal de feedback)
        Debug.Log("Mensaje de Combate: " + message);
        yield return new WaitForSeconds(delay);
        NextTurn();
    }
    // (Opcional) Corrutina para un peque�o delay antes de terminar el combate al huir
    // private IEnumerator DelayedEndCombat(bool playerWon, float delay)
    // {
    //    yield return new WaitForSeconds(delay);
    //    EndCombat(playerWon);
    // }
    private Combatant DetermineItemTarget(ItemData item, Combatant caster, Combatant directTarget)
    {
        // L�gica simplificada: si es consumible de HP/MP, el objetivo es un aliado o el lanzador.
        if (item.itemType == ItemType.Consumable && (item.hpToRestore > 0 || item.mpToRestore > 0))
        {
            if (directTarget != null && directTarget.isPlayerCharacter && !directTarget.isDefeated) return directTarget;
            if (caster != null && !caster.isDefeated && caster.isPlayerCharacter) return caster;
        }
        // (A�adir l�gica para otros tipos de �tems, ej: bombas que apuntan a enemigos)
        // else if (item.itemType == ItemType.Bomb_Damage_Enemy_Etc) {
        //    if (directTarget != null && !directTarget.isPlayerCharacter && !directTarget.isDefeated) return directTarget;
        // }
        return null;
    }

    private List<Combatant> DetermineActualTargets(AbilityTargetType targetType, Combatant attacker, Combatant directTarget)
    {
        List<Combatant> actualTargets = new List<Combatant>();
        if (attacker == null) return actualTargets;

        // Filtrar las listas de posibles objetivos al inicio
        List<Combatant> livingEnemies = _combatants.Where(c => c != null && !c.isPlayerCharacter && !c.isDefeated).ToList();
        List<Combatant> livingAllies = _combatants.Where(c => c != null && c.isPlayerCharacter && !c.isDefeated).ToList();

        switch (targetType)
        {
            case AbilityTargetType.Self:
                if (!attacker.isDefeated) actualTargets.Add(attacker);
                break;

            case AbilityTargetType.SingleAlly:
                if (attacker.isPlayerCharacter) // Si un jugador lanza a un aliado
                {
                    if (directTarget != null && directTarget.isPlayerCharacter && !directTarget.isDefeated) actualTargets.Add(directTarget);
                }
                else // Si un enemigo lanza a un aliado (otro enemigo)
                {
                    if (directTarget != null && !directTarget.isPlayerCharacter && !directTarget.isDefeated) actualTargets.Add(directTarget);
                }
                break;

            case AbilityTargetType.AllAllies: // Afecta a todos los del mismo bando que el lanzador
                actualTargets.AddRange(attacker.isPlayerCharacter ? livingAllies : livingEnemies);
                break;

            case AbilityTargetType.SingleEnemy: // Afecta a un enemigo del bando contrario
                if (attacker.isPlayerCharacter) // Si un jugador lanza a un enemigo
                {
                    if (directTarget != null && !directTarget.isPlayerCharacter && !directTarget.isDefeated) actualTargets.Add(directTarget);
                }
                else // Si un enemigo lanza a un enemigo (un jugador)
                {
                    if (directTarget != null && directTarget.isPlayerCharacter && !directTarget.isDefeated) actualTargets.Add(directTarget);
                }
                break;

            case AbilityTargetType.AllEnemies: // Afecta a todos los del bando contrario
                actualTargets.AddRange(attacker.isPlayerCharacter ? livingEnemies : livingAllies);
                break;
        }
        return actualTargets;
    }
    public float GetEnemyDefeatAnimDuration()
    {
        // Podr�as hacer esto m�s din�mico si los enemigos tienen diferentes duraciones de animaci�n de derrota
        // almacenadas en su EnemyData, por ejemplo.
        return enemyAttackAnimationDuration; // Reutilizando la duraci�n del ataque por ahora, o usa enemyDefeatAnimationBaseDuration
    }
    private IEnumerator ShowRoundTitleCoroutine(string title)
    {
        if (roundTitleText == null) yield break;

        roundTitleText.text = title;
        roundTitleText.gameObject.SetActive(true);

        // Opcional: Podr�as a�adir un peque�o efecto de fade in aqu� si quieres
        // CanvasGroup titleCG = roundTitleText.GetComponent<CanvasGroup>(); // Si le a�ades un CanvasGroup
        // if (titleCG != null) { /* ... l�gica de fade in ... */ }

        yield return new WaitForSeconds(roundTitleDisplayDuration);

        // Opcional: Efecto de Fade Out para el t�tulo
        // if (titleCG != null) { /* ... l�gica de fade out ... */ }
        // else roundTitleText.gameObject.SetActive(false); 
        roundTitleText.gameObject.SetActive(false); // Simple ocultaci�n por ahora

        _roundTitleCoroutine = null; // Limpiar referencia
    }

}

