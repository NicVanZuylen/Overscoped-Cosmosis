using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorTarget : MonoBehaviour
{
    [Tooltip("AOE hazard VFX")]
    [SerializeField]
    private ParticleObject m_aoeParticles;

    [Tooltip("Impact explosion VFX")]
    [SerializeField]
    private ParticleObject m_explosion;

    [Tooltip("Duration of the AOE hazard.")]
    [SerializeField]
    private float m_fAOEDuration = 10.0f;

    [Tooltip("Damage per second of the AOE hazard when the player stands in it.")]
    [SerializeField]
    private float m_fAOEDamagePerSec = 10.0f;

    [SerializeField]
    private Renderer[] m_particlesToFade = null;

    private GameObject m_indicator; // Indicator effect object.
    private Queue<MeteorTarget> m_targetPool; // Object pool of all available meteor targets.
    private PlayerStats m_playerStats; // Player stats script reference.
    private BoxCollider m_collider; // AOE hazard collider.
    private float m_fAOETime; // Current AOE timer value.

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

            if (m_particlesToFade[0].material.GetFloat("_Alpha") > 0)
                m_particlesToFade[0].material.SetFloat("_Alpha", m_particlesToFade[0].material.GetFloat("_Alpha") - Time.deltaTime);
            else if (m_particlesToFade[1].material.GetFloat("_Alpha") > 0)
                m_particlesToFade[1].material.SetFloat("_Alpha", m_particlesToFade[1].material.GetFloat("_Alpha") - Time.deltaTime);
            else if (m_particlesToFade[2].material.GetFloat("_Alpha") > 0)
                m_particlesToFade[2].material.SetFloat("_Alpha", m_particlesToFade[2].material.GetFloat("_Alpha") - Time.deltaTime);
            else if (m_particlesToFade[0].material.GetFloat("_Alpha") <= 0 && m_particlesToFade[1].material.GetFloat("_Alpha") <= 0 && m_particlesToFade[2].material.GetFloat("_Alpha") <= 0 && m_aoeParticles.IsPlaying())
                m_aoeParticles.Stop();
        }
        else
        {
            m_particlesToFade[0].material.SetFloat("_Alpha", 0.3f);
            m_particlesToFade[1].material.SetFloat("_Alpha", 0.5f);
            m_particlesToFade[2].material.SetFloat("_Alpha", 1);
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
        // Disable indicator FX.
        m_indicator.SetActive(false);

        // Begin and reset AOE time.
        enabled = true;
        m_fAOETime = m_fAOEDuration;

        // Play explosion VFX.
        m_explosion.Play();

        // Play AOE VFX.
        m_aoeParticles.Play();        
    }

    private void OnTriggerStay(Collider other)
    {
        // Damage player over time.
        if (other.gameObject == m_playerStats.gameObject && m_fAOETime > 0.0f)
            m_playerStats.DealDamage(m_fAOEDamagePerSec * Time.fixedDeltaTime);
    }
}
