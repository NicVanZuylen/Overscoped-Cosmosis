using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorTarget : MonoBehaviour
{
    [SerializeField]
    private ParticleObject m_aoeParticles;

    [SerializeField]
    private ParticleObject m_explosion;

    [SerializeField]
    private float m_fAOEDuration = 10.0f;

    [SerializeField]
    private float m_fAOEDamagePerSec = 10.0f;

    private GameObject m_indicator;
    private Queue<MeteorTarget> m_targetPool;
    private PlayerStats m_playerStats;
    private BoxCollider m_collider;
    private float m_fAOETime;

    public void Init(GameObject player, Queue<MeteorTarget> targetPool)
    {
        m_indicator = transform.GetChild(2).gameObject;
        m_playerStats = player.GetComponent<PlayerStats>();
        m_collider = GetComponent<BoxCollider>();
        m_targetPool = targetPool;
    }

    private void Update()
    {
        m_fAOETime -= Time.deltaTime;

        if(m_fAOETime <= 0.0f)
        {
            // Make this target available in the pool again.
            m_targetPool.Enqueue(this);

            // Stop particle effects and script updates.
            if(m_aoeParticles.IsPlaying())
                m_aoeParticles.Stop();

            enabled = false;
        }
    }

    public void SummonMeteor(Meteor meteor, Vector3 v3Origin)
    {
        m_indicator.SetActive(true);

        if (m_explosion.IsPlaying())
            m_explosion.Stop();

        meteor.Summon(v3Origin, this);
    }

    public void StartAOE()
    {
        m_indicator.SetActive(false);
        // Begin and reset AOE time.
        enabled = true;
        m_fAOETime = m_fAOEDuration;

        m_explosion.Play();

        m_aoeParticles.Play();        
    }

    private void OnTriggerStay(Collider other)
    {
        // Damage player.
        if (other.gameObject == m_playerStats.gameObject && m_fAOETime > 0.0f)
            m_playerStats.DealDamage(m_fAOEDamagePerSec * Time.fixedDeltaTime);
    }
}
