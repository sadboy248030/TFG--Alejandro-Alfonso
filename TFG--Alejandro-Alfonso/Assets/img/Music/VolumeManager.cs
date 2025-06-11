using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    public Slider musicSlider;
    public AudioSource musicSource;

    void Start()
    {
        // Cargar volumen guardado o establecer por defecto
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicSlider.value = savedVolume;
        musicSource.volume = savedVolume;

        // Asociar cambio de valor al m�todo
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
}
