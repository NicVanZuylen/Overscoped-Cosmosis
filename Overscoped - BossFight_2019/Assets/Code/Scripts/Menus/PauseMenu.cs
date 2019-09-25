using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Tooltip("Parent object of all elements in the pause menu.")]
    [SerializeField]
    private GameObject m_pauseMenu = null;

    [Tooltip("Parent object of all elements in credits screen.")]
    [SerializeField]
    private GameObject m_creditsScreen = null;

    private PlayerController m_playerController;
    private ScreenFade m_fadeScript;
    private static bool m_bPaused;

    private void Awake()
    {
        m_playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        m_fadeScript = GetComponentInChildren<ScreenFade>();

        Time.timeScale = 1.0f;
        m_bPaused = false;

        m_pauseMenu.SetActive(false);
    }

    private void Update()
    {
        // Pause or unpause on escape.
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (!m_bPaused)
                PauseGame();
            else
                ResumeGame();
        }
    }

    /*
    Description: Get whether or not the game is paused.
    Return Type: bool
    */
    public static bool IsPaused()
    {
        return m_bPaused;
    }

    /*
    Description: Pause the game.
    */
    public void PauseGame()
    {
        // Show pause menu.
        m_pauseMenu.SetActive(true);

        // Unlock cursor.
        m_playerController.SetPaused(true);

        m_bPaused = true;
        Time.timeScale = 0.0f; // Freeze time.
    }

    /*
    Description: Resume the game.
    */
    public void ResumeGame()
    {
        // Hide pause menu.
        m_pauseMenu.SetActive(false);

        // Lock player cursor.
        m_playerController.SetPaused(false);

        m_bPaused = false;
        Time.timeScale = 1.0f; // Resume time.
    }

    /*
    Description: Quit to title screen.
    */
    public void QuitTitleScreen()
    {
        m_bPaused = false;
        Time.timeScale = 1.0f;
        m_playerController.SetFocus(false);

        // Loads title screen scene.
        SceneManager.LoadScene(0);
    }

    /*
    Description: Show the credits screen.
    */
    public void ShowCreditsScreen()
    {
        PauseGame();

        // Show credits UI.
        m_creditsScreen.SetActive(true);

        // Reverse fade.
        m_fadeScript.SetFadeRate(0.3f);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_OUT);
    }

    /*
    Description: Perform a screen fade to black, then call QuitTitleScreen()
    */
    public void FadeQuitTitleScreen()
    {
        m_fadeScript.SetFadeRate(1.0f);
        m_fadeScript.SetCallback(QuitTitleScreen);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);
    }

    /*
    Description: Quit to desktop.
    */
    public void QuitDesktop()
    {
        // Quit game.
        Application.Quit();
    }
}
