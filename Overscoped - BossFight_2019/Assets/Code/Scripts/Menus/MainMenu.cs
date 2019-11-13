using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
 * Description: Title screen GUI behaviour.
 * Author: Nic Van Zuylen, Lachlan Mesman
*/

public class MainMenu : MonoBehaviour
{
    [Header("Menu handle gameobjects")]

    [Tooltip("Menu handle gameobjects")]
    [SerializeField]
    private GameObject[] m_menuHandles = null;

    [Header("Title screen button references.")]
    [SerializeField]
    private Button[] m_titleScreenButtons = null;

    [Header("Volume slider references")]

    [SerializeField]
    private Slider m_masterVolume = null;

    [SerializeField]
    private Slider m_bossVolume = null;

    [SerializeField]
    private Slider m_playerVolume = null;

    [SerializeField]
    private Slider m_grappleVolume = null;

    [SerializeField]
    private Slider m_windVolume = null;

    [SerializeField]
    private Slider m_musicVolume = null;

    [Header("General options references.")]

    [SerializeField]
    private Button m_applyButton = null;

    private ScreenFade m_fadeScript; // Controls screen fade for menu transitions.
    private MusicManager m_musicManager; // Plays title screen music.
    private Settings m_settings; // Settings save data.
    private SettingsIO m_settingSaver; // Saves and loads the settings data.
    private EMenuState m_eState; // Current menu state.

    public enum EMenuState
    {
        MENU_TITLE = 0,
        MENU_OPTIONS = 1
    }

    private void Awake()
    {
        m_fadeScript = GetComponentInChildren<ScreenFade>();

        m_musicManager = GetComponent<MusicManager>();

        m_musicManager.PlayTrackIndex(0);

        // Creates a instance of SettingIO
        m_settingSaver = new SettingsIO();
        m_settingSaver.ReadFile();

        // Gets saved settings or defaults if the save was not found.
        m_settings = m_settingSaver.GetData();

        // Set option UI values.
        UpdateOptionsUI();

        // Disable apply button as changes have not been made yet.
        m_applyButton.interactable = false;

        // Set current menu state.
        m_eState = EMenuState.MENU_TITLE;

    }

    private void UpdateOptionsUI()
    {
        // Audio
        m_masterVolume.value = m_settings.m_fMasterVolume;
        m_bossVolume.value = m_settings.m_fBossVolume;
        m_playerVolume.value = m_settings.m_fPlayerVolume;
        m_grappleVolume.value = m_settings.m_fGrappleVolume;
        m_windVolume.value = m_settings.m_fWindVolume;
        m_musicVolume.value = m_settings.m_fMusicVolume;
    }

    private void SwitchMenu(EMenuState eState)
    {
        // Deactivate old menu.
        m_menuHandles[(int)m_eState].SetActive(false);

        // Switch current state to new state.
        m_eState = eState;

        // Activate new menu.
        m_menuHandles[(int)m_eState].SetActive(true);
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene(1); // Scene at build index 1 should be the game scene.
    }

    /*
    Description: Callback for when the Play button is pressed. Starts gameplay.
    */
    public void PlayButtonCallback()
    {
        m_fadeScript.SetCallback(LoadGameScene);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);

        // Disable main menu buttons.
        for (int i = 0; i < m_titleScreenButtons.Length; ++i)
            m_titleScreenButtons[i].interactable = false;
    }

    /*
    Description: Callback for when the Options button is pressed. Enters the options menu.
    */
    public void OptionsButtonCallback()
    {
        // Switch to options menu.
        SwitchMenu(EMenuState.MENU_OPTIONS);
    }

    /*
    Description: Callback for when the Quit button is pressed. Quits the game.
    */
    public void QuitButtonCallback()
    {
        m_fadeScript.SetFadeRate(1.5f);
        m_fadeScript.SetCallback(Application.Quit);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);
    }

    /*
    Description: Release the apply button because changes have been made.
    */
    public void ReleaseApplyButtonCallback()
    {
        m_applyButton.interactable = true;
    }

    /*
    Description: Callback for when the Apply button is pressed, Saves current settings on the options menu.
    */
    public void ApplyButtonCallback()
    {
        // Set settings values.

        // Volumes
        m_settings.m_fMasterVolume = m_masterVolume.value;
        m_settings.m_fBossVolume = m_bossVolume.value;
        m_settings.m_fPlayerVolume = m_playerVolume.value;
        m_settings.m_fGrappleVolume = m_grappleVolume.value;
        m_settings.m_fWindVolume = m_windVolume.value;
        m_settings.m_fMusicVolume = m_musicVolume.value;

        // Sets the data to the settings values
        m_settingSaver.SetData(m_settings);

        // Writes the data into the file
        m_settingSaver.WriteFile();

        // Disable button until changes are mode.
        m_applyButton.interactable = false;
    }

    /*
    Description: Back button callback on the options menu.
    */
    public void OptionsBackButtonCallback()
    {
        // Switch to title screen menu.
        SwitchMenu(EMenuState.MENU_TITLE);
    }

    /*
    Description: Callback which will revert the options values back to the save data.
    */
    public void OptionsRevertButtonCallback()
    {
        // Get default settings.
        m_settings = m_settingSaver.GetData();

        // Update option UI values.
        UpdateOptionsUI();
    }

    /*
    Description: Callback which will reset the options values to the default values.
    */
    public void OptionsDefaultsButtonCallback()
    {
        // Get default settings.
        m_settings = m_settingSaver.GetDefaults();

        // Update option UI values.
        UpdateOptionsUI();
    }

    /*
    Description: Callback which will update the music volume with the new music volume slider value.
    */
    public void UpdateMusicVolumeCallback()
    {
        MusicManager.SetVolume(m_musicVolume.value, m_masterVolume.value);
    }
}
