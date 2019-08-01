using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalPunch : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> m_portals = null;

    [SerializeField]
    private GameObject m_player;

    private bool m_bPunch;

    [SerializeField]
    private float m_fPortalSpawnTime = 1.0f;
    [SerializeField]
    private float m_fPunchSpeedUp = 1.0f;
    [SerializeField]
    private float m_fPunchSpeedDown = 1.0f;
    [SerializeField]
    private float m_fPunchSitTime = 1.0f;
    [SerializeField]
    private float m_fPortalEndTime = 1.0f;
    [SerializeField]
    private float m_fPunchHeight = 5.0f;

    private void OnEnable()
    {
        StartCoroutine(PortalWaitTime());
    }

    private void Update()
    {
        if (m_bPunch == true)
        {
            foreach (GameObject portal in m_portals)
            {
                portal.transform.GetChild(0).position = Vector3.MoveTowards(portal.transform.GetChild(0).position, new Vector3(portal.transform.position.x, portal.transform.position.y + m_fPunchHeight, portal.transform.position.z), m_fPunchSpeedUp * Time.deltaTime);
            }
        }
        else
        {
            foreach (GameObject portal in m_portals)
            {
                portal.transform.GetChild(0).position = Vector3.MoveTowards(portal.transform.GetChild(0).position, new Vector3(portal.transform.position.x, portal.transform.position.y - m_fPunchHeight, portal.transform.position.z), m_fPunchSpeedDown * Time.deltaTime);
            }
        }    
    }

    IEnumerator PortalWaitTime()
    {
        yield return new WaitForSeconds(m_fPortalSpawnTime);
        Punch();
    }

    void Punch()
    {
        m_bPunch = true;
        StartCoroutine(End());
    }

    IEnumerator End()
    {
        yield return new WaitForSeconds(m_fPunchSitTime);
        m_bPunch = false;       
        yield return new WaitForSeconds(m_fPortalEndTime);
        gameObject.SetActive(false);
        m_bPunch = false;
    }
    //private void OnCollisionEnter(Collision collision)
    //{
    //    m_player.GetComponent<PlayerStats>().
    //}
}
