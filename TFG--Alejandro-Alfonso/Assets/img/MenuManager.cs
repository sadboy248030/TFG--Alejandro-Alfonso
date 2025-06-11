using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Referencias UI")]
    // Arrastra desde el Inspector el GameObject padre que contiene Start, Options y Exit
    public GameObject mainMenuButtons;

    // Arrastra desde el Inspector el panel que quieres mostrar al pulsar Options
    public GameObject optionsPanel;

    private void Start()
    {
        if (optionsPanel == null || mainMenuButtons == null)
        {
            Debug.LogError("Las referencias UI no est�n asignadas en el Inspector.");
            return;
        }
        optionsPanel.SetActive(false);
    }

    // Este m�todo se llama desde el bot�n �Options�
    public void ShowOptions()
    {
        Debug.Log("ShowOptions() llamado.");
        if (mainMenuButtons != null)
            mainMenuButtons.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    // Este m�todo se llama desde el bot�n �Volver� dentro de OptionsPanel
    public void BackToMainMenu()
    {
        Debug.Log("BackToMainMenu() llamado.");
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (mainMenuButtons != null)
            mainMenuButtons.SetActive(true);
    }
}
