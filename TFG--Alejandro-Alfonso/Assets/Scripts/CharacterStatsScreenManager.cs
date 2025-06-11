using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using TopDown; // Asumiendo que Character.cs está aquí
using System;    // Necesario para System.Action

public class CharacterStatsScreenManager : MonoBehaviour
{
    public static CharacterStatsScreenManager Instance { get; private set; }

    [Header("Referencias a UI")]
    [SerializeField] private GameObject characterStatsScreenPanel;
    [SerializeField] private TextMeshProUGUI screenTitleText_Stats;
    [SerializeField] private Image characterBigSpriteImage_StatsScreen;
    [SerializeField] private TextMeshProUGUI characterNameText_StatsScreen;
    [SerializeField] private TextMeshProUGUI characterLevelText_StatsScreen;
    [SerializeField] private TextMeshProUGUI currentXPText_StatsScreen;
    [SerializeField] private TextMeshProUGUI nextLevelXPText_StatsScreen;
    [SerializeField] private Slider xpProgressBar_StatsScreen;
    [SerializeField] private GameObject detailedStatsContainer_StatsScreen;
    [SerializeField] private TextMeshProUGUI hpValueText;
    [SerializeField] private TextMeshProUGUI mpValueText;
    [SerializeField] private TextMeshProUGUI attackValueText;
    [SerializeField] private TextMeshProUGUI defenseValueText;
    [SerializeField] private TextMeshProUGUI magicAttackValueText;
    [SerializeField] private TextMeshProUGUI magicDefenseValueText;
    [SerializeField] private TextMeshProUGUI speedValueText;
    [SerializeField] private GameObject abilitiesPanel_StatsScreen;
    [SerializeField] private Transform abilitiesListContainer_StatsScreen;
    [SerializeField] private GameObject abilityListItemUIPrefab;
    [SerializeField] private TextMeshProUGUI abilityDescriptionText_StatsScreen;
    [SerializeField] private Button closeButton_StatsScreen;

    private Character _currentlyDisplayedCharacter;
    private List<AbilityListItem_UI> _abilityListItemUIs = new List<AbilityListItem_UI>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (characterStatsScreenPanel == null) Debug.LogError("CSSM: 'characterStatsScreenPanel' no asignado.", this);
        if (abilitiesListContainer_StatsScreen == null) Debug.LogWarning("CSSM: 'abilitiesListContainer_StatsScreen' no asignado.", this);
        if (abilityListItemUIPrefab == null) Debug.LogWarning("CSSM: 'abilityListItemUIPrefab' no asignado.", this);
        if (abilityDescriptionText_StatsScreen == null) Debug.LogWarning("CSSM: 'abilityDescriptionText_StatsScreen' no asignado.", this);
        if (characterBigSpriteImage_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'characterBigSpriteImage_StatsScreen' no asignado.", this);
        if (characterNameText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'characterNameText_StatsScreen' no asignado.", this);
        if (characterLevelText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'characterLevelText_StatsScreen' no asignado.", this);
        if (currentXPText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'currentXPText_StatsScreen' no asignado.", this);
        if (nextLevelXPText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'nextLevelXPText_StatsScreen' no asignado.", this);
        if (detailedStatsContainer_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'detailedStatsContainer_StatsScreen' no asignado.", this);
        if (hpValueText == null) Debug.LogWarning("CharacterStatsScreenManager: 'hpValueText' no asignado.", this);
        if (mpValueText == null) Debug.LogWarning("CharacterStatsScreenManager: 'mpValueText' no asignado.", this);
        if (attackValueText == null) Debug.LogWarning("CharacterStatsScreenManager: 'attackValueText' no asignado.", this);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (closeButton_StatsScreen != null)
        {
            closeButton_StatsScreen.onClick.AddListener(() => {
                if (UIManager.Instance != null) UIManager.Instance.CloseCurrentPanel();
            });
        }
    }

    public void DisplayCharacterData(Character characterToShow)
    {
        if (characterToShow == null)
        {
            Destroy(gameObject);
            return;
        }

        _currentlyDisplayedCharacter = characterToShow;
        UpdateAllCharacterInfo();
    }

    private void UpdateAllCharacterInfo()
    {
        if (_currentlyDisplayedCharacter == null) return;

        if (characterNameText_StatsScreen != null) characterNameText_StatsScreen.text = _currentlyDisplayedCharacter.characterName;
        if (characterLevelText_StatsScreen != null) characterLevelText_StatsScreen.text = "Nivel: " + _currentlyDisplayedCharacter.level.ToString();
        if (characterBigSpriteImage_StatsScreen != null)
        {
            characterBigSpriteImage_StatsScreen.sprite = _currentlyDisplayedCharacter.portraitSprite;
            characterBigSpriteImage_StatsScreen.enabled = (_currentlyDisplayedCharacter.portraitSprite != null);
        }

        if (currentXPText_StatsScreen != null) currentXPText_StatsScreen.text = "XP: " + _currentlyDisplayedCharacter.currentXP.ToString();
        if (nextLevelXPText_StatsScreen != null) nextLevelXPText_StatsScreen.text = "Siguiente: " + _currentlyDisplayedCharacter.experienceToNextLevel.ToString();
        if (xpProgressBar_StatsScreen != null)
        {
            if (_currentlyDisplayedCharacter.experienceToNextLevel > 0)
                xpProgressBar_StatsScreen.value = (float)_currentlyDisplayedCharacter.currentXP / _currentlyDisplayedCharacter.experienceToNextLevel;
            else
                xpProgressBar_StatsScreen.value = 0;
        }

        UpdateDetailedStatsDisplay();
        PopulateAbilitiesList();
    }

    private void UpdateDetailedStatsDisplay()
    {
        if (_currentlyDisplayedCharacter == null) return;
        if (hpValueText != null) hpValueText.text = _currentlyDisplayedCharacter.currentHP + " / " + _currentlyDisplayedCharacter.MaxHP;
        if (mpValueText != null) mpValueText.text = _currentlyDisplayedCharacter.currentMP + " / " + _currentlyDisplayedCharacter.MaxMP;
        if (attackValueText != null) attackValueText.text = _currentlyDisplayedCharacter.Attack.ToString();
        if (defenseValueText != null) defenseValueText.text = _currentlyDisplayedCharacter.Defense.ToString();
        if (magicAttackValueText != null) magicAttackValueText.text = _currentlyDisplayedCharacter.MagicAttack.ToString();
        if (magicDefenseValueText != null) magicDefenseValueText.text = _currentlyDisplayedCharacter.MagicDefense.ToString();
        if (speedValueText != null) speedValueText.text = _currentlyDisplayedCharacter.Speed.ToString();
    }

    private void PopulateAbilitiesList()
    {
        if (abilitiesListContainer_StatsScreen == null || abilityListItemUIPrefab == null || _currentlyDisplayedCharacter == null)
        {
            if (abilityDescriptionText_StatsScreen != null) abilityDescriptionText_StatsScreen.text = "";
            return;
        }

        foreach (Transform child in abilitiesListContainer_StatsScreen)
        {
            Destroy(child.gameObject);
        }
        _abilityListItemUIs.Clear();

        if (_currentlyDisplayedCharacter.knownAbilities == null || _currentlyDisplayedCharacter.knownAbilities.Count == 0)
        {
            if (abilityDescriptionText_StatsScreen != null) abilityDescriptionText_StatsScreen.text = "Sin habilidades conocidas.";
            return;
        }

        foreach (AbilityData ability in _currentlyDisplayedCharacter.knownAbilities)
        {
            if (ability == null) continue;

            GameObject listItemGO = Instantiate(abilityListItemUIPrefab, abilitiesListContainer_StatsScreen);
            AbilityListItem_UI listItemUI = listItemGO.GetComponent<AbilityListItem_UI>();

            if (listItemUI != null)
            {
                // --- LÍNEA CORREGIDA ---
                // Ahora llamamos al método SetupAbilityItem y le pasamos tanto la habilidad
                // como el método de este script que debe ejecutar al hacer clic.
                listItemUI.SetupAbilityItem(ability, OnAbilityListItemClicked);
                _abilityListItemUIs.Add(listItemUI);
            }
            else
            {
                Debug.LogError("[CSSM] ¡ERROR! El prefab 'abilityListItemUIPrefab' no tiene el componente AbilityListItem_UI.", this);
                Destroy(listItemGO);
            }
        }

        // Seleccionar la primera habilidad de la lista por defecto para mostrar su descripción
        if (_abilityListItemUIs.Count > 0 && _abilityListItemUIs[0].CurrentAbilityData != null)
        {
            OnAbilityListItemClicked(_abilityListItemUIs[0].CurrentAbilityData);
        }
        else if (abilityDescriptionText_StatsScreen != null)
        {
            abilityDescriptionText_StatsScreen.text = "";
        }
    }

    public void OnAbilityListItemClicked(AbilityData selectedAbility)
    {
        if (selectedAbility == null)
        {
            if (abilityDescriptionText_StatsScreen != null) abilityDescriptionText_StatsScreen.text = "";
            return;
        }

        if (abilityDescriptionText_StatsScreen != null)
        {
            StringBuilder descBuilder = new StringBuilder();
            descBuilder.AppendLine("<b>" + selectedAbility.abilityName + "</b>");

            if (selectedAbility.mpCost > 0)
            {
                descBuilder.AppendLine("Coste MP: " + selectedAbility.mpCost);
            }
            descBuilder.AppendLine("<i>" + selectedAbility.effectType.ToString() + " - " + selectedAbility.targetType.ToString() + "</i>");
            descBuilder.AppendLine("--------------------");
            descBuilder.AppendLine(selectedAbility.description);

            abilityDescriptionText_StatsScreen.text = descBuilder.ToString();
        }
    }

    private void HandleCharacterLevelUp(Character characterWhoLeveledUp)
    {
        if (characterStatsScreenPanel != null && characterStatsScreenPanel.activeSelf &&
            _currentlyDisplayedCharacter == characterWhoLeveledUp)
        {
            UpdateAllCharacterInfo();
        }
    }
}
