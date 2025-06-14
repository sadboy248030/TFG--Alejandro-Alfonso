using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Necesario para Action (eventos)

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("Configuraci�n Inicial de la Party")]
    [SerializeField] private List<GameObject> initialPartyMemberGameObjects = new List<GameObject>();

    private List<Character> _currentPartyMembers = new List<Character>();
    public List<Character> CurrentPartyMembers => new List<Character>(_currentPartyMembers);

    private Character _selectedMenuCharacter;
    public Character SelectedMenuCharacter => _selectedMenuCharacter;

    public static event Action OnPartyRosterChanged;
    public static event Action<Character> OnSelectedMenuCharacterChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeParty();
    }

    private void InitializeParty()
    {
        _currentPartyMembers.Clear();
        foreach (GameObject memberGO in initialPartyMemberGameObjects)
        {
            if (memberGO != null)
            {
                Character characterComponent = memberGO.GetComponent<Character>();
                if (characterComponent != null)
                {
                    // Usar el m�todo AddCharacterToParty para asegurar que la l�gica sea consistente
                    // y que se aplique DontDestroyOnLoad.
                    AddCharacterToParty(characterComponent);
                }
            }
        }
        Debug.Log("PartyManager: Party inicializada con " + _currentPartyMembers.Count + " miembros.");

        if (_currentPartyMembers.Count > 0)
        {
            SetSelectedMenuCharacter(_currentPartyMembers[0]);
        }
    }

    /// <summary>
    /// A�ade un nuevo personaje a la party desde un Prefab. Ideal para reclutamiento.
    /// </summary>
    public bool AddCharacterToPartyFromPrefab(GameObject characterPrefab)
    {
        if (characterPrefab == null)
        {
            Debug.LogError("PartyManager: Se intent� a�adir un personaje desde un Prefab nulo.");
            return false;
        }
        GameObject newMemberInstance = Instantiate(characterPrefab);
        Character newMemberCharacter = newMemberInstance.GetComponent<Character>();

        if (newMemberCharacter == null)
        {
            Debug.LogError($"PartyManager: El prefab '{characterPrefab.name}' no tiene un componente Character.cs.", characterPrefab);
            Destroy(newMemberInstance);
            return false;
        }
        return AddCharacterToParty(newMemberCharacter);
    }

    /// <summary>
    /// A�ade un nuevo personaje a la party si a�n no est� presente y lo hace persistente.
    /// </summary>
    public bool AddCharacterToParty(Character newMember)
    {
        if (newMember == null) return false;
        if (_currentPartyMembers.Contains(newMember)) return false;

        _currentPartyMembers.Add(newMember);

        Debug.Log($"PartyManager: {newMember.characterName} a�adido a la party y marcado como persistente.");
        OnPartyRosterChanged?.Invoke();


        if (_selectedMenuCharacter == null && _currentPartyMembers.Count == 1)
        {
            SetSelectedMenuCharacter(newMember);
        }
        return true;
    }

    public void SetSelectedMenuCharacter(Character character)
    {
        if (character == null) return;
        if (_currentPartyMembers.Contains(character))
        {
            if (_selectedMenuCharacter != character)
            {
                _selectedMenuCharacter = character;
                OnSelectedMenuCharacterChanged?.Invoke(_selectedMenuCharacter);
            }
        }
    }

    public Character GetFirstPartyMember()
    {
        if (_currentPartyMembers.Count > 0)
        {
            return _currentPartyMembers[0];
        }
        return null;
    }
}
