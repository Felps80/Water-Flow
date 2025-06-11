using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu Panel")]
    public static bool GameIsPaused = false;
    public GameObject pauseMenuUI;
    public GameObject pauseButtonUI;
    private static PauseMenu instance;

    void Awake()
    {
        // If an instance exists and it’s not this, destroy this duplicate.
        if (instance != null && instance != this)
        {
           
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Remove a inscrição para evitar erros
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
       
        // Reatribua as referências se necessário. Por exemplo, se o painel de pause estiver na nova cena,
        // você pode buscá-lo por nome ou tag:
        if (pauseMenuUI == null)
        {
            // Exemplo: supondo que o painel de pause na nova cena tenha o nome "PauseMenuUI".
            pauseMenuUI = GameObject.Find("Pause Menu");
          
        }
        // Se estiver utilizando o botão de pause na UI:
        if (pauseButtonUI == null)
        {
            pauseButtonUI = GameObject.Find("Pause 1"); // ajuste para o nome correto
           
        }

        // Se for o caso, reative a visibilidade do botão de pause ao trocar de cena.
        if (pauseButtonUI != null)
        {
            pauseButtonUI.SetActive(true);
        }

        // Se o jogo estava pausado na cena anterior, garanta que ele esteja retomado na nova cena:
        Resume();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (GameIsPaused)
            {
                Resume();

            } else
            {
                Pause();
            }

        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        if (pauseButtonUI != null)
            pauseButtonUI.SetActive(true);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        if (pauseButtonUI != null)
            pauseButtonUI.SetActive(false);
        Time.timeScale = 0f;
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("Main Menu");
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Time.timeScale = 1f;
    }

    public void SkipFase()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1f;
        GameIsPaused = false;
        pauseMenuUI.SetActive(false);
        if (pauseButtonUI != null)
            pauseButtonUI.SetActive(true);
    }

    public void PauseButton()
    {
        Pause();
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        //Debug.Log("Apertou");
    }

}
