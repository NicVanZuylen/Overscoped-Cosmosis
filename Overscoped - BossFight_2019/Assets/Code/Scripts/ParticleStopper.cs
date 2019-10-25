using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]

public class ParticleStopper : MonoBehaviour
{
    [SerializeField]
    private bool m_bDisableOnStop = true;

    [SerializeField]
    private ParticleSystemStopBehavior m_stopBehaviour = ParticleSystemStopBehavior.StopEmitting;

    private ParticleSystem m_system;
    private float m_fDuration;
    private float m_fTimer;

    void Awake()
    {
        m_system = GetComponent<ParticleSystem>();

        m_fDuration = m_system.main.duration;
        m_fTimer = m_fDuration;
    }

    void Update()
    {
        if(m_system.isPlaying)
            m_fTimer -= Time.deltaTime;

        // Disable when timer runs out.
        if(m_fTimer <= 0.0f)
        {
            if(m_system.isPlaying)
                m_system.Stop(true, m_stopBehaviour);

            if (m_system.particleCount == 0)
            {
                m_fTimer = m_fDuration;

                if(m_bDisableOnStop)
                    gameObject.SetActive(false);
            }
        }
    }
}
