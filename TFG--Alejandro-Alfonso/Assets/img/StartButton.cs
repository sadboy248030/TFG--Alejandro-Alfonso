using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    // M�todo que se llamar� al pulsar el bot�n Start
    public void LoadVillageScene()
    {
        Debug.Log("Cargando la escena 'Village'...");
        SceneManager.LoadScene("Village");
    }
}
