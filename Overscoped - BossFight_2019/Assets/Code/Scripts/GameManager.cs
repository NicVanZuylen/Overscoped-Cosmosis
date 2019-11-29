using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private Image[] m_bossHealthImages = null;

    [Tooltip("Push added to the player when spawning from the portal.")]
    [SerializeField]
    private float m_fExitPortalForce = 10.0f;

    private GameObject m_player;
    private Transform m_playerTransform;
    private BossBehaviour m_bossScript;
    private Plane m_worldSplitPlane; // Separates the arena and tutorial areas.
    private MusicManager m_musicManager;
    private Settings m_settings;
    private SettingsIO m_settingLoader;
    private bool m_bBossDeadOnce;

    private void Awake()
    {
        m_player = GameObject.FindGameObjectWithTag("Player");
        m_playerTransform = m_player.transform;
        m_bossScript = GameObject.FindGameObjectWithTag("Boss").GetComponent<BossBehaviour>();
        GameObject worldPlane = transform.GetChild(0).gameObject;
        m_musicManager = GetComponent<MusicManager>();

        for (int i = 0; i < m_bossHealthImages.Length; ++i)
            m_bossHealthImages[i].enabled = false;

        // Give the player a push to give the sense the just exited the portal.
        if(!PlayerStats.CheckpointReached())
        {
            PlayerController playerController = m_player.GetComponent<PlayerController>();
            playerController.AddImpulse(playerController.LookForward() * m_fExitPortalForce);
        }

        // Play random track out of the first group to start.
        m_musicManager.PlayRandomTrack();

        // Construct plane structure using plane gameobject.
        m_worldSplitPlane = new Plane(worldPlane.transform.up, worldPlane.transform.position);

        // Plane is no longer needed.
        Destroy(worldPlane);

        // Create instance of SettingsIO
        m_settingLoader = new SettingsIO();

        // Reads the folder
        m_settingLoader.ReadFile();

        // Sets settings to the values in the file
        m_settings = m_settingLoader.GetData();

        // Sets the volume
        BossBehaviour.SetVolume(m_settings.m_fBossVolume, m_settings.m_fMasterVolume);
        PlayerStats.SetVolume(m_settings.m_fPlayerVolume,m_settings.m_fWindVolume, m_settings.m_fMasterVolume);
        GrappleHook.SetVolume(m_settings.m_fGrappleVolume, m_settings.m_fMasterVolume);
        MusicManager.SetVolume(m_settings.m_fMusicVolume, m_settings.m_fMasterVolume);
    }

    // Update is called once per frame
    void Update()
    {
        // Boss will begin to attack the player once they are on the arena side of the plane.
        if (m_worldSplitPlane.GetSide(m_playerTransform.position) && !m_bossScript.enabled)
        {
            m_musicManager.PlayTrackIndex(0, 1);

            // Enable boss health bar once fight starts.
            for (int i = 0; i < m_bossHealthImages.Length; ++i)
                m_bossHealthImages[i].enabled = true;

            Debug.Log("Good people of Cyrodiil. WELCOME, to the Arena!");

            // Enable boss AI and disable game manager script for updates.
            m_bossScript.enabled = true;
        }

        if (!m_bBossDeadOnce)
        {
            if (!m_bossScript.gameObject.activeInHierarchy)
            {
                Debug.Log("Boss dead");

                // Disable health bar once fight is finished.
                for (int i = 0; i < m_bossHealthImages.Length; ++i)
                    m_bossHealthImages[i].enabled = false;

                // Change music track when boss is killed.
                m_musicManager.PlayTrackIndex(0);
                m_bBossDeadOnce = true;
            }
        }
    }
}
