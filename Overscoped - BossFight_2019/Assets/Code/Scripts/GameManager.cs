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
    private bool m_BossDeadOnce;
    private void Awake()
    {
        m_player = GameObject.FindGameObjectWithTag("Player");
        m_bossScript = GameObject.FindGameObjectWithTag("Boss").GetComponent<BossBehaviour>();
        GameObject worldPlane = transform.GetChild(0).gameObject;
        m_musicManager = GetComponent<MusicManager>();

        m_musicManager.PlayTrackIndex(0);

        // Construct plane structure using plane gameobject.
        m_worldSplitPlane = new Plane(worldPlane.transform.up, worldPlane.transform.position);

        // Plane is no longer needed.
        Destroy(worldPlane);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            m_musicManager.PlayTrackIndex(1);
        //ensure that the music is only played once
        if (!m_once)
        {
            // Boss will begin to attack the player once they are on the arena side of the plane.
            if (m_worldSplitPlane.GetSide(m_player.transform.position))
            {
                m_musicManager.PlayTrackIndex(1);

                Debug.Log("Good people of Cyrodiil. WELCOME, to the Arena!");

                // Enable boss AI and disable game manager script for updates.
                m_bossScript.enabled = true;
                //enabled = false;
                m_once = true;
            }
        }
        if (!m_BossDeadOnce)
        {
            if (!m_bossScript.gameObject.activeInHierarchy)
            {
                Debug.Log("Boss dead");
                //Play music once boss is dead
                m_musicManager.PlayTrackIndex(0);
                m_BossDeadOnce = true;
            }
        }
    }
}
