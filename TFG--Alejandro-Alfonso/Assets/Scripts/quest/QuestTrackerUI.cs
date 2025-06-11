using UnityEngine;
using TMPro;
using System.Linq;

// Este script debe ir en un GameObject que esté SIEMPRE ACTIVO.
// Controlará un panel hijo que contiene los elementos visuales.
public class QuestTrackerUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("El objeto Panel que contiene todos los elementos visuales del tracker. Este es el que se mostrará/ocultará.")]
    [SerializeField] private GameObject questTrackerPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;

    [Header("Objetivos")]
    [Tooltip("El Transform padre donde se instanciarán los objetivos de la misión.")]
    [SerializeField] private Transform goalsContainer;
    [Tooltip("El prefab para mostrar una línea de objetivo (debe tener el script QuestGoalUI).")]
    [SerializeField] private GameObject questGoalPrefab;

    private QuestData currentTrackedQuest;

    void OnEnable()
    {
        QuestManager.OnQuestAccepted += TrackNewQuest;
        QuestManager.OnQuestObjectiveUpdated += UpdateQuestObjectives;
        QuestManager.OnQuestClaimed += StopTrackingQuest;
    }

    void OnDisable()
    {
        QuestManager.OnQuestAccepted -= TrackNewQuest;
        QuestManager.OnQuestObjectiveUpdated -= UpdateQuestObjectives;
        QuestManager.OnQuestClaimed -= StopTrackingQuest;
    }

    void Start()
    {
        // Al empezar, nos aseguramos de que el panel visual esté oculto.
        // Este script, en su propio GameObject, debe permanecer ACTIVO para que esta lógica funcione.
        if (questTrackerPanel != null)
        {
            questTrackerPanel.SetActive(false);
        }

        // --- LÓGICA DE COMPROBACIÓN INICIAL ---
        // Comprobamos si ya hay una misión activa cuando la UI se inicia.
        // Esto soluciona el problema de que la misión se acepte ANTES de que la UI esté lista para escuchar.
        if (QuestManager.Instance != null)
        {
            QuestData initialQuest = QuestManager.Instance.GetActiveQuest();
            if (initialQuest != null)
            {
                TrackNewQuest(initialQuest);
            }
        }
    }

    private void TrackNewQuest(QuestData newQuest)
    {
        // Evitar refrescar si ya se está mostrando la misma misión
        if (questTrackerPanel.activeSelf && currentTrackedQuest == newQuest) return;

        currentTrackedQuest = newQuest;
        UpdateUI();

        // Solo activamos el panel si tiene una misión que mostrar.
        if (currentTrackedQuest != null)
        {
            questTrackerPanel.SetActive(true);
        }
    }

    private void StopTrackingQuest(QuestData completedQuest)
    {
        if (currentTrackedQuest == completedQuest)
        {
            currentTrackedQuest = null;
            questTrackerPanel.SetActive(false);
        }
    }

    private void UpdateQuestObjectives(QuestData updatedQuest)
    {
        if (currentTrackedQuest == updatedQuest)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (currentTrackedQuest == null || questTrackerPanel == null)
        {
            if (questTrackerPanel != null) questTrackerPanel.SetActive(false);
            return;
        }

        questTitleText.text = currentTrackedQuest.title;
        questDescriptionText.text = currentTrackedQuest.description;

        foreach (Transform child in goalsContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentTrackedQuest.goals.Count; i++)
        {
            GameObject goalGO = Instantiate(questGoalPrefab, goalsContainer);
            QuestGoalUI goalUI = goalGO.GetComponent<QuestGoalUI>();
            if (goalUI != null)
            {
                int currentProgress = QuestManager.Instance.GetQuestProgress(currentTrackedQuest, i);
                goalUI.Setup(currentTrackedQuest.goals[i], currentProgress);
            }
        }
    }
}
