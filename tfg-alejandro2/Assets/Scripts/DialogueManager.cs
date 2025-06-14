using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Necesario para gestionar eventos de escena
using TMPro;
using Ink.Runtime;
using System.Collections.Generic;
using TopDown;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Configuración de Input")]
    [SerializeField] private KeyCode advanceDialogueKey = KeyCode.Space;

    [Header("UI de Opciones")]
    [SerializeField] private GameObject choiceButtonPrefab;

    private GameObject dialoguePanel;
    private TextMeshProUGUI dialogueText;
    private GameObject choicesContainer;

    public bool IsDialoguePlaying { get; private set; }

    private Story currentStory;
    private NPCDialogue currentNpcDialogue;
    private PlayerMovement playerMovement;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Forzar la búsqueda del PlayerMovement porque puede cambiar en cada escena.
        playerMovement = FindObjectOfType<PlayerMovement>();

        // --- CAMBIO CLAVE ---
        // Si ya tenemos una referencia al panel de diálogo (porque nuestro Canvas es persistente),
        // no necesitamos buscarlo de nuevo. Solo lo buscamos si es nulo.
        if (dialoguePanel == null)
        {
            Debug.Log("DialogueManager: No se encontraron referencias de UI, buscando en la nueva escena...");
            FindUIReferences();
        }
    }

    void Update()
    {
        // Esta comprobación evita errores si el diálogo se intenta iniciar antes de que la UI esté lista.
        if (!IsDialoguePlaying || dialoguePanel == null) return;

        if (currentStory.currentChoices.Count == 0 && Input.GetKeyDown(advanceDialogueKey))
        {
            ContinueStory();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON, NPCDialogue npc, NPCHealer healer = null)
    {
        if (inkJSON == null) { Debug.LogError("El archivo Ink JSON es nulo.", npc.gameObject); return; }

        // Si por alguna razón no se encontraron las referencias al cargar la escena, intentar de nuevo.
        if (dialoguePanel == null)
        {
            if (!FindUIReferences())
            {
                Debug.LogError("No se pudo iniciar el diálogo porque no se encontraron las referencias de UI en la escena.");
                return;
            }
        }

        currentStory = new Story(inkJSON.text);
        currentNpcDialogue = npc;
        IsDialoguePlaying = true;
        dialoguePanel.SetActive(true);

        // La referencia al PlayerMovement se obtiene en OnSceneLoaded
        if (playerMovement != null) playerMovement.SetCanMove(false);

        if (npc.TryGetComponent<QuestGiver>(out QuestGiver questGiver))
        {
            currentStory.BindExternalFunction("AcceptQuest", () => questGiver.AcceptThisQuest());
            currentStory.BindExternalFunction("ClaimRewards", () => questGiver.ClaimRewardsForThisQuest());
        }
        // 2. Si nos han pasado un componente Healer, enlazamos su función.
        if (healer != null)
        {
            currentStory.BindExternalFunction("heal_party", () => healer.HealPartyCompletely());
        }

        ContinueStory();
    }

    private void ExitDialogueMode()
    {
        IsDialoguePlaying = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";

        if (currentNpcDialogue != null)
        {
            currentNpcDialogue.OnDialogueEnd();
            currentNpcDialogue = null;
        }

        if (playerMovement != null) playerMovement.SetCanMove(true);
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            dialogueText.text = currentStory.Continue();
            DisplayChoices();
        }
        else
        {
            ExitDialogueMode();
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        foreach (Transform child in choicesContainer.transform)
        {
            Destroy(child.gameObject);
        }

        if (currentChoices.Count > 0)
        {
            choicesContainer.SetActive(true);
            foreach (Choice choice in currentChoices)
            {
                GameObject choiceButtonGO = Instantiate(choiceButtonPrefab, choicesContainer.transform);
                choiceButtonGO.GetComponentInChildren<TextMeshProUGUI>().text = choice.text;
                choiceButtonGO.GetComponent<Button>().onClick.AddListener(() => MakeChoice(choice));
            }
        }
        else
        {
            choicesContainer.SetActive(false);
        }
    }

    public void MakeChoice(Choice choice)
    {
        currentStory.ChooseChoiceIndex(choice.index);
        ContinueStory();
    }

    private bool FindUIReferences()
    {
        // Buscar por tag es más robusto que buscar por nombre
        dialoguePanel = GameObject.FindGameObjectWithTag("DialoguePanel");
        if (dialoguePanel != null)
        {
            dialogueText = GameObject.FindGameObjectWithTag("DialogueText")?.GetComponent<TextMeshProUGUI>();
            choicesContainer = GameObject.FindGameObjectWithTag("DialogueChoices");

            if (dialogueText == null || choicesContainer == null)
            {
                Debug.LogError("DialogueManager: No se pudieron encontrar los componentes hijos 'DialogueText' o 'DialogueChoices' con los tags correctos en la escena actual.");
                dialoguePanel = null;
                return false;
            }

            // --- ESTE ES EL CAMBIO CLAVE ---
            // Una vez que lo encontramos, lo desactivamos para que no se vea hasta que sea necesario.
            dialoguePanel.SetActive(false);

            Debug.Log("DialogueManager: Referencias de UI encontradas y asignadas en la escena actual.");
            return true;
        }

        return false;
    }
}
