using UnityEngine;
using TMPro;
using System.Linq;

// Este script debe ir en un GameObject que est� SIEMPRE ACTIVO.
// Controlar� un panel hijo que contiene los elementos visuales.
public class QuestTrackerUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("El objeto Panel que contiene todos los elementos visuales del tracker. Este es el que se mostrar�/ocultar�.")]
    [SerializeField] private GameObject questTrackerPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;

    [Header("Objetivos")]
    [Tooltip("El Transform padre donde se instanciar�n los objetivos de la misi�n.")]
    [SerializeField] private Transform goalsContainer;
    [Tooltip("El prefab para mostrar una l�nea de objetivo (debe tener el script QuestGoalUI).")]
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
        // Al empezar, nos aseguramos de que el panel visual est� oculto.
        // Este script, en su propio GameObject, debe permanecer ACTIVO para que esta l�gica funcione.
        if (questTrackerPanel != null)
        {
            questTrackerPanel.SetActive(false);
        }

        // --- L�GICA DE COMPROBACI�N INICIAL ---
        // Comprobamos si ya hay una misi�n activa cuando la UI se inicia.
        // Esto soluciona el problema de que la misi�n se acepte ANTES de que la UI est� lista para escuchar.
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
        // Evitar refrescar si ya se est� mostrando la misma misi�n
        if (questTrackerPanel.activeSelf && currentTrackedQuest == newQuest) return;

        currentTrackedQuest = newQuest;
        UpdateUI();

        // Solo activamos el panel si tiene una misi�n que mostrar.
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
