using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
 * Description: Title screen GUI behaviour.
 * Author: Nic Van Zuylen
*/

public class MainMenu : MonoBehaviour
{
    private ScreenFade m_fadeScript;

    private void Awake()
    {
        m_fadeScript = GetComponentInChildren<ScreenFade>();   
    }

    private void Update()
    {
        
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene(1); // Scene at build index 1 should be the game scene.
    }

    public void PlayButtonCallback()
    {
        m_fadeScript.SetCallback(LoadGameScene);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);
    }

    public void QuitButtonCallback()
    {
        m_fadeScript.SetFadeRate(1.5f);
        m_fadeScript.SetCallback(Application.Quit);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);
    }
}
