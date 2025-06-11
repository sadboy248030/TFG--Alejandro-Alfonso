using UnityEngine;
using UnityEngine.UI; // Necesario para Image y Slider

public class NewBehaviourScript : MonoBehaviour
{
    public Image volumeMeterImage; // Asigna tu objeto Image del medidor aqu�
    public Sprite[] volumeSprites; // Arreglo de tus Sprites de volumen (05_0, 05_1, etc.)
    public Slider volumeSlider;   // Asigna tu Slider aqu� (opcional)

    // Asumiendo que 0 es silencio y el m�ximo es volumeSprites.Length - 1
    // Si tu slider va de 0 a 100, habr� que mapear ese rango a los �ndices de los sprites.

    void Start()
    {
        if (volumeSlider != null)
        {
            // Aseg�rate de que el m�todo OnVolumeChanged se llame cuando el slider cambie de valor
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            // Establece el valor inicial del slider para que el medidor se actualice al inicio
            OnVolumeChanged(volumeSlider.value);
        }
        else
        {
            // Si no hay slider, puedes establecer un volumen inicial fijo o controlarlo de otra manera
            UpdateVolumeMeter(50); // Ejemplo: volumen inicial al 50%
        }
    }

    // M�todo que se llama cuando el valor del Slider cambia
    public void OnVolumeChanged(float sliderValue)
    {
        // Mapea el valor del slider (ej. 0-100) al �ndice del arreglo de sprites
        // Clamp asegura que el �ndice est� dentro de los l�mites del arreglo
        int spriteIndex = Mathf.FloorToInt(Mathf.InverseLerp(volumeSlider.minValue, volumeSlider.maxValue, sliderValue) * (volumeSprites.Length - 1));

        // Aseg�rate de que el �ndice no exceda los l�mites
        spriteIndex = Mathf.Clamp(spriteIndex, 0, volumeSprites.Length - 1);

        UpdateVolumeMeter(spriteIndex);
    }

    // M�todo para actualizar el sprite del medidor de volumen
    void UpdateVolumeMeter(int spriteIndex)
    {
        if (volumeMeterImage != null && volumeSprites != null && spriteIndex >= 0 && spriteIndex < volumeSprites.Length)
        {
            volumeMeterImage.sprite = volumeSprites[spriteIndex];
        }
        else
        {
            Debug.LogError("Configuraci�n incorrecta del VolumeController. Aseg�rate de asignar la imagen y los sprites.");
        }
    }

    // Puedes agregar m�todos p�blicos para cambiar el volumen desde otros scripts
    public void SetVolume(float newVolume)
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = newVolume; // Esto llamar� a OnVolumeChanged autom�ticamente
        }
        else
        {
            // Si no hay slider, mapea newVolume (ej. 0-100) directamente al �ndice del sprite
            int spriteIndex = Mathf.FloorToInt(Mathf.InverseLerp(0, 100, newVolume) * (volumeSprites.Length - 1));
            spriteIndex = Mathf.Clamp(spriteIndex, 0, volumeSprites.Length - 1);
            UpdateVolumeMeter(spriteIndex);
        }
    }
}