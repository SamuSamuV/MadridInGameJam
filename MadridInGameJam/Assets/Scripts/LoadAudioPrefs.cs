using UnityEngine;
using UnityEngine.Audio;

public class LoadAudioPrefs : MonoBehaviour
{
    public AudioMixer myMixer;

    void Start()
    {
        float savedMusicVol = PlayerPrefs.GetFloat("MusicVol", 1f);
        float savedSFXVol = PlayerPrefs.GetFloat("SFXVol", 1f);

        myMixer.SetFloat("MusicVolParam", Mathf.Log10(savedMusicVol) * 20);
        myMixer.SetFloat("SFXVolParam", Mathf.Log10(savedSFXVol) * 20);
    }
}