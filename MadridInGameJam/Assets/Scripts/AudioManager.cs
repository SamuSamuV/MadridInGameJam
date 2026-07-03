using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources (Arrastra aquí los 3 componentes)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("Música")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;

    [Header("SFX - UI & Chica")]
    public AudioClip[] dialogueClickClips;
    public AudioClip[] ladySpeakClips;
    public AudioClip missionCompleteClip;

    [Header("SFX - Gameplay")]
    public AudioClip[] playerClickClips;
    public AudioClip[] playerReleaseClips;
    public AudioClip[] passStationClips;
    public AudioClip[] hoverStationClips;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Evita que la música se corte al cambiar de nivel
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMenuMusic() { PlayMusic(menuMusic); }
    public void PlayGameplayMusic() { PlayMusic(gameplayMusic); }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying)) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayDialogueClick() { PlayRandomSFX(dialogueClickClips, sfxSource); }
    public void PlayPlayerClick() { PlayRandomSFX(playerClickClips, sfxSource); }
    public void PlayPlayerRelease() { PlayRandomSFX(playerReleaseClips, sfxSource); }
    public void PlayPassStation() { PlayRandomSFX(passStationClips, sfxSource); }
    public void PlayHoverStation() { PlayRandomSFX(hoverStationClips, sfxSource); }

    public void PlayMissionComplete()
    {
        if (missionCompleteClip != null) sfxSource.PlayOneShot(missionCompleteClip);
    }

    // El sonido de la chica tiene ligeras variaciones de tono para sonar más orgánico
    public void PlayLadySpeak()
    {
        if (ladySpeakClips == null || ladySpeakClips.Length == 0) return;
        AudioClip clip = ladySpeakClips[Random.Range(0, ladySpeakClips.Length)];
        voiceSource.pitch = Random.Range(0.95f, 1.05f); // Variación de tono
        voiceSource.PlayOneShot(clip);
    }

    private void PlayRandomSFX(AudioClip[] clips, AudioSource source)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        source.pitch = Random.Range(0.95f, 1.05f); // Pequeña variación para que no suene repetitivo
        source.PlayOneShot(clip);
    }
}