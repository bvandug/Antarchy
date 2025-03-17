using UnityEngine;
using UnityEngine.UI;

public class SoundLevel : MonoBehaviour
{

    [SerializeField] Slider volumeSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void ChangeVolume(){
        AudioListener.volume = volumeSlider.value;
    }
}
