using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorTarget : MonoBehaviour
{
    [Tooltip("AOE hazard VFX.")]
    [SerializeField]
    private ParticleObject m_aoeParticles = new ParticleObject();

    [Tooltip("Explosion impact effect for meteors.")]
    [SerializeField]
    private ParticleObject m_explosion = new ParticleObject();

    [Tooltip("Indicator effect for the meteor AOE.")]
    [SerializeField]
    private ParticleObject m_indicator = new ParticleObject();

    [SerializeField]
    private float m_fAOEDuration = 10.0f;

    [SerializeField]
    private float m_fAOEDamagePerSec = 10.0f;

    [SerializeField]
    private Renderer[] m_particlesToFade = null;

    //private GameObject m_indicator;
    private Queue<MeteorTarget> m_targetPool;
    private PlayerStats m_playerStats;
    private BoxCollider m_collider;
    private Material[] m_fadeMaterials; // Cached material references for AOE particles.
    private float[] m_fAOEAlphaFades;
    private float m_fAOETime;

    public void Init(GameObject player, Queue<MeteorTarget> targetPool)
    {
        m_playerStats = player.GetComponent<PlayerStats>();
        m_collider = GetComponent<BoxCollider>();
        m_targetPool = targetPool;

        m_fadeMaterials = new Material[m_particlesToFade.Length];

        // Cache fade particle materials.
        for (int i = 0; i < m_fadeMaterials.Length; ++i)
            m_fadeMaterials[i] = m_particlesToFade[i].material;

        m_fAOEAlphaFades = new float[m_fadeMaterials.Length];

        for (int i = 0; i < m_fAOEAlphaFades.Length; ++i)
            m_fAOEAlphaFades[i] = m_fadeMaterials[i].GetFloat("_Alpha");
    }

    private void Update()
    {
        m_fAOETime -= Time.deltaTime;

        if(m_fAOETime <= 0.0f && m_aoeParticles.IsPlaying())
        {
            // Make this target available in the pool again.
            m_targetPool.Enqueue(this);

            // Stop explosion VFX if they are somehow still playing.
            if (m_explosion.IsPlaying())
                m_explosion.Stop();

            for(int i = 0; i < m_fadeMaterials.Length; ++i)
            {
                m_fAOEAlphaFades[i] -= Time.deltaTime;
                m_fadeMaterials[i].SetFloat("_Alpha", Mathf.Max(m_fAOEAlphaFades[i], 0.0f));
            }

            // Disable this object and stop particle effects once faded.
            if (m_fAOEAlphaFades[0] <= 0.0f)
            {
                m_aoeParticles.Stop();
                gameObject.SetActive(false);
            }
        }
        else
        {
            m_fAOEAlphaFades[0] = 0.3f;
            m_fAOEAlphaFades[1] = 0.5f;
            m_fAOEAlphaFades[2] = 1.0f;
        }
    }

    public void SummonMeteor(Meteor meteor, Vector3 v3Origin)
    {
        if (!m_indicator.IsPlaying())
            m_indicator.Play();

        if (m_explosion.IsPlaying())
            m_explosion.Stop();

        meteor.Summon(v3Origin, this);
    }

    public void StartAOE()
    {
        if (m_indicator.IsPlaying())
            m_indicator.Stop();
        
        // Begin and reset AOE time.
        enabled = true;
        m_fAOETime = m_fAOEDuration;

        if(!m_explosion.IsPlaying())
            m_explosion.Play();

        if(!m_aoeParticles.IsPlaying())
            m_aoeParticles.Play();

        // Set initial material alphas.
        m_fAOEAlphaFades[0] = 0.3f;
        m_fAOEAlphaFades[1] = 0.5f;
        m_fAOEAlphaFades[2] = 1.0f;
    }

    private void OnTriggerStay(Collider other)
    {
        // Damage player.
        if (other.gameObject == m_playerStats.gameObject && m_fAOETime > 0.0f)
            m_playerStats.DealDamage(m_fAOEDamagePerSec * Time.fixedDeltaTime);
    }
}
