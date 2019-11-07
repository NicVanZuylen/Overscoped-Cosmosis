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

        // Creates a instance of SettingIO
        m_settingSaver = new SettingsIO();

        // Sets the volume 
        m_settings.m_fMasterVolume = 1.0f;

        m_settings.m_fBossVolume = 1.0f;
        m_settings.m_fGrappleVolume = 1.0f;
        m_settings.m_fPlayerVolume = 1.0f;
        m_settings.m_fWindVolume = 1.0f;

        // Sets the data to the settings values
        m_settingSaver.SetData(m_settings);
        // Writes the data into the file
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
