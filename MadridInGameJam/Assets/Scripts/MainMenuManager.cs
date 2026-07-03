using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance; // Lo convertimos en Singleton para que el cubo lo encuentre

    [Header("Paneles de Interfaz")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject settingsPanel;

    [Header("Paradas del Menú (Rail Nodes)")]
    [Tooltip("Arrastra aquí la parada que arranca el juego")]
    public RailNode playNode;
    [Tooltip("Arrastra aquí la parada que abre las opciones")]
    public RailNode optionsNode;
    [Tooltip("Arrastra aquí la parada que cierra el juego")]
    public RailNode quitNode;

    [Header("Configuración")]
    public string firstLevelSceneName = "Level1"; // Pon aquí el nombre de la escena de tu primer nivel

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
    }

    // --- NUEVA FUNCIÓN: Es llamada por el cubo cuando llega a una parada ---
    public void OnNodeReached(RailNode reachedNode)
    {
        if (reachedNode == playNode)
        {
            LoadScene(firstLevelSceneName);
        }
        else if (reachedNode == optionsNode)
        {
            OpenSettings();
        }
        else if (reachedNode == quitNode)
        {
            QuitGame();
        }
    }

    public void LoadScene(string SceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadLevel(SceneName);
        else
            SceneManager.LoadScene(SceneName);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        // Opcional: mainMenuPanel.SetActive(false); si quieres ocultar los raíles mientras estás en opciones
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        // --- LA MAGIA: Al cerrar las opciones, devolvemos el cubo al centro del menú ---
        if (MazeRailHandler.Instance != null)
        {
            MazeRailHandler.Instance.ResetToStart();
        }
    }

    public void QuitGame()
    {
        Debug.Log("¡Saliendo del juego!");
        Application.Quit();
    }
}