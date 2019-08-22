using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorAOE : MonoBehaviour
{
    [Tooltip("Reference to the player.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Damage dealt to the player overtime.")]
    [SerializeField]
    private float m_fAoeDPS = 10.0f;

    private PlayerStats m_playerStats;

    [SerializeField]
    private ParticleSystem m_AoeParticle;

    void Start()
    {
        m_playerStats = m_player.GetComponent<PlayerStats>();
    }

    public void AOE(Vector3 v3Position)
    {
        gameObject.SetActive(true);
        transform.position = v3Position;
        m_AoeParticle.Play();
    }
    
    void OnTriggerStay(Collider other)
    {
        // Deal damage to the player.
        if (other.gameObject == m_player)
        {
            m_playerStats.DealDamage(m_fAoeDPS * Time.deltaTime);
        }
    }

    IEnumerator WaitForParticle()
    {
        yield return new WaitForSeconds(m_AoeParticle.main.duration);
        gameObject.SetActive(false);
    } 
}
