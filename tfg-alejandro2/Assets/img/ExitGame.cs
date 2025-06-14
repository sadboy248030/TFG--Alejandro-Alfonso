using UnityEngine;

public class ExitGame : MonoBehaviour
{
    // Este m�todo se asignar� al OnClick() de tu bot�n �Exit�
    public void QuitGame()
    {
        Debug.Log("Quit button pressed. Exiting game.");

        // Si estamos en el Editor de Unity, detenemos el Play Mode
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Si estamos en la build final, cerramos la aplicaci�n
        Application.Quit();
#endif
    }
}
