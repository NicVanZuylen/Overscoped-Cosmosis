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
    private MusicManager m_musicManager;
    private Settings m_settings;
    private SettingsIO m_settingSaver;

    private void Awake()
    {
        m_fadeScript = GetComponentInChildren<ScreenFade>();

        m_musicManager = GetComponent<MusicManager>();

        m_musicManager.PlayTrackIndex(0);

        m_settingSaver = new SettingsIO();

        m_settings.m_fBossVolume = 1.1f;
        m_settings.m_fGrappleVolume = 1.4f;
        m_settings.m_fPlayerVolume = 3.5f;
        m_settings.m_fWindVolume = 0.3f;

        m_settingSaver.SetData(m_settings);
        m_settingSaver.WriteFile();
        
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
