using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndPortal : MonoBehaviour
{
    private Collider m_collider;
    private ScreenFade m_fadeScript;
    private PauseMenu m_pauseScript;

    void Awake()
    {
        m_collider = GetComponent<Collider>();
        m_fadeScript = FindObjectOfType<ScreenFade>();
        m_pauseScript = FindObjectOfType<PauseMenu>();
    }

    private void QuitTitleScrren()
    {
        // Quit to title scene.
        SceneManager.LoadScene(0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            // Begin fade to black before transitioning to the title screen.
            m_fadeScript.SetFadeRate(0.3f);
            m_fadeScript.SetCallback(m_pauseScript.ShowCreditsScreen);
            m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);


            // Freeze game.
            other.gameObject.GetComponent<PlayerController>().SetPaused(true);
            Time.timeScale = 0.0f;

            m_collider.enabled = false;
        }
    }
}
