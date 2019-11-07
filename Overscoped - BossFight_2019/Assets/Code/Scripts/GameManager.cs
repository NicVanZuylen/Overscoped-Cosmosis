using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private GameObject m_player;
    private BossBehaviour m_bossScript;
    private Plane m_worldSplitPlane; // Separates the arena and tutorial areas.
    private MusicManager m_musicManager;
    private bool m_once;
    private bool m_bBossDeadOnce;
    private void Awake()
    {
        m_player = GameObject.FindGameObjectWithTag("Player");
        m_bossScript = GameObject.FindGameObjectWithTag("Boss").GetComponent<BossBehaviour>();
        GameObject worldPlane = transform.GetChild(0).gameObject;
        m_musicManager = GetComponent<MusicManager>();

        // Play random tutorial track.
        m_musicManager.PlayRandomTrack();

        // Construct plane structure using plane gameobject.
        m_worldSplitPlane = new Plane(worldPlane.transform.up, worldPlane.transform.position);

        // Plane is no longer needed.
        Destroy(worldPlane);
    }

    // Update is called once per frame
    void Update()
    {
        // Boss will begin to attack the player once they are on the arena side of the plane.
        if (m_worldSplitPlane.GetSide(m_player.transform.position) && !m_bossScript.enabled)
        {
            // Play combat track.
            m_musicManager.PlayTrackIndex(0, 1);

            Debug.Log("Good people of Cyrodiil. WELCOME, to the Arena!");

            // Enable boss AI and disable game manager script for updates.
            m_bossScript.enabled = true;
            m_once = true;
        }

        if (!m_bBossDeadOnce)
        {
            if (!m_bossScript.gameObject.activeInHierarchy)
            {
                Debug.Log("Boss dead");
                //Play music once boss is dead
                m_musicManager.PlayTrackIndex(0);
                m_bBossDeadOnce = true;
            }
        }
    }
}
