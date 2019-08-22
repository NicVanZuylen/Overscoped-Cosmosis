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

    [SerializeField]
    private float m_fWaitForParticleTimer;

    private float m_fWaitForParticleCooldown;

    void Start()
    {
        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_fWaitForParticleCooldown = m_AoeParticle.main.duration;
        m_fWaitForParticleTimer = m_fWaitForParticleCooldown;
    }

    public void AOE()
    {
        m_fWaitForParticleTimer = m_fWaitForParticleCooldown;
        gameObject.SetActive(true);
        m_AoeParticle.Play();
    }

    private void Update()
    {
        m_fWaitForParticleTimer -= Time.deltaTime;
        if(m_fWaitForParticleTimer <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Deal damage to the player.
        if (other.gameObject == m_player)
        {
            m_playerStats.DealDamage(m_fAoeDPS * Time.deltaTime);
        }
    }
}
