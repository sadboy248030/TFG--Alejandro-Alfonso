using UnityEngine;
using TMPro;

public class QuestGoalUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goalDescriptionText;
    [SerializeField] private TextMeshProUGUI goalProgressText;

    /// <summary>
    /// Configura esta línea de UI con los datos de un objetivo de misión.
    /// </summary>
    public void Setup(QuestGoal goal, int currentAmount)
    {
        // --- Comprobaciones de depuración ---
        if (goal == null)
        {
            Debug.LogError("QuestGoalUI: El 'goal' pasado a Setup() es nulo.");
            return;
        }
        if (goalDescriptionText == null || goalProgressText == null)
        {
            Debug.LogError("QuestGoalUI: Las referencias a los TextMeshPro no están asignadas en el Inspector.", this.gameObject);
            return;
        }
        // --- Fin de las comprobaciones ---

        string description = "Objetivo desconocido";
        if (goal.goalType == GoalType.Kill)
        {
            // Usamos el ID requerido ya que no tenemos un sistema para buscar nombres de enemigos.
            description = $"Derrota: {goal.requiredID}";
        }
        else if (goal.goalType == GoalType.Collect)
        {
            description = $"Consigue: {goal.requiredID}";
        }

        string progress = $"{currentAmount} / {goal.requiredAmount}";

        // Asignamos el texto.
        goalDescriptionText.text = description;
        goalProgressText.text = progress;

        Debug.Log($"QuestGoalUI Actualizado -> Descripción: '{description}', Progreso: '{progress}'");

        // Si el objetivo está completo, cambiamos el color del texto.
        if (currentAmount >= goal.requiredAmount)
        {
            goalDescriptionText.color = Color.gray;
            goalProgressText.color = Color.green;
        }
        else
        {
            // Asegurarnos de que el color es el normal si no está completo.
            goalDescriptionText.color = Color.white;
            goalProgressText.color = Color.white;
        }
    }
}
