using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("Configuración")]
    public AudioMixer myMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        float savedMusicVol = PlayerPrefs.GetFloat("MusicVol", 1f);
        float savedSFXVol = PlayerPrefs.GetFloat("SFXVol", 1f);

        musicSlider.value = savedMusicVol;
        sfxSlider.value = savedSFXVol;

        SetMusicVolume(savedMusicVol);
        SetSFXVolume(savedSFXVol);
    }

    public void SetMusicVolume(float volume)
    {
        myMixer.SetFloat("MusicVolParam", Mathf.Log10(volume) * 20);

        PlayerPrefs.SetFloat("MusicVol", volume);
    }

    public void SetSFXVolume(float volume)
    {
        myMixer.SetFloat("SFXVolParam", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVol", volume);
    }
}