using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Handles behaviour and VFX of the player's cosmic beam attack.
 * Author: Nic Van Zuylen
*/

public class PlayerBeam : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------
    [Header("Object References")]

    [Tooltip("Beam GUI material.")]
    [SerializeField]
    private Material m_guiMaterial = null;

    [SerializeField]
    private GameObject[] m_beamParticleObjects = null;

    // -------------------------------------------------------------------------------------------------
    [Header("Beam Attack Properties")]

    [Tooltip("Maximum beam charge value.")]
    [SerializeField]
    private float m_fMaxBeamCharge = 100.0f;

    [Tooltip("Loss rate of beam charge when firing it.")]
    [SerializeField]
    private float m_fChargeLossRate = 20.0f;

    [Tooltip("Minimum beam charge cost to use the beam.")]
    [SerializeField]
    private float m_fMinBeamCharge = 15.0f;

    [Tooltip("Start point of the beam effect.")]
    [SerializeField]
    private Transform m_beamOrigin = null;

    [Tooltip("Maximum range of the beam in metres.")]
    [SerializeField]
    private int m_nBeamLength = 512;

    // -------------------------------------------------------------------------------------------------
    [Header("Audio")]

    [SerializeField]
    private AudioClip m_chargeLoopSFX = null;

    [SerializeField]
    private AudioClip m_fireLoopSFX = null;

    [SerializeField]
    private AudioClip m_impactLoopSFX = null;

    private AudioLoop m_chargeAudioLoop;
    private AudioLoop m_fireAudioLoop;
    private AudioLoop m_impactAudioLoop;

    // -------------------------------------------------------------------------------------------------

    private PlayerController m_controller;
    private CameraEffects m_camEffects;
    private RaycastHit m_sphereCastHit;
    private GameObject m_endObj;
    private float m_fBeamCharge; // Actual current charge value.
    private float m_fCurrentMeterLevel; // Current charge as displayed on the HUD.
    private int m_nDissolveBracerIndex;
    private bool m_bCanCast;
    private bool m_bBeamUnlocked;
    private bool m_bBeamEnabled;

    // Volumetric beam effect.
    private ParticleSystem[] m_beamParticles;
    private ParticleSystemRenderer[] m_beamParticleRenderers;
    private ParticleSystem.Particle[] m_particles;
    private float m_fMeshLength;

    void Awake()
    {
        // Component retreival.
        m_controller = GetComponent<PlayerController>();
        m_camEffects = GetComponentInChildren<CameraEffects>();

        m_bBeamUnlocked = true;

        m_fBeamCharge = m_fMaxBeamCharge;

        m_fBeamCharge = 0.0f;

        m_beamParticles = new ParticleSystem[m_beamParticleObjects.Length];
        m_beamParticleRenderers = new ParticleSystemRenderer[m_beamParticleObjects.Length];

        for (int i = 0; i < m_beamParticleObjects.Length; ++i)
        {
            m_beamParticles[i] = m_beamParticleObjects[i].GetComponent<ParticleSystem>();
            m_beamParticleRenderers[i] = m_beamParticleObjects[i].GetComponent<ParticleSystemRenderer>();
        }

        m_particles = new ParticleSystem.Particle[m_nBeamLength / 2];

        m_fMeshLength = 2.0f;

        m_endObj = new GameObject("Player_Beam_End_Point");

        m_chargeAudioLoop = new AudioLoop(m_chargeLoopSFX, gameObject, ESpacialMode.AUDIO_SPACE_NONE);
        m_fireAudioLoop = new AudioLoop(m_fireLoopSFX, gameObject, ESpacialMode.AUDIO_SPACE_NONE);
        m_impactAudioLoop = new AudioLoop(m_impactLoopSFX, m_endObj, ESpacialMode.AUDIO_SPACE_WORLD, (m_nBeamLength * m_fMeshLength) + 10.0f);
    }

    /*
    Description: Update volumetric beam particle systems.
    Param:
        ParticleSystem[] particleSystems: The particlesystems to update.
        ParticleSystemRenderer[] renderers: The particle renderers to update material properties on.
        ParticleSystem.Particle[] particles: Pre-allocated array of particles to update.
        Vector3 v3Origin: Origin point of the beam.
        float fLength: The length of the beam.
        bool bHit: Whether or not the beam hit a surface.
    */
    private void UpdateParticlePositions(ParticleSystem[] particleSystems, ParticleSystemRenderer[] renderers, ParticleSystem.Particle[] particles, Vector3 v3Origin, float fLength, bool bHit)
    {
        int nParticleAmount = 0;

        // Loop through all particles/segments and set their positions.
        for(int i = 0; i < particles.Length; ++i)
        {
            float fOffset = i * m_fMeshLength;

            // Set new offset.
            particles[i].position = new Vector3(0.0f, 0.0f, fOffset);
            particles[i].startSize = 1.0f;
            particles[i].startColor = Color.white;
            ++nParticleAmount;

            // Stop once length is exceeded.
            if (bHit && fOffset + m_fMeshLength >= fLength)
            {
                break;
            }
        }

        for(int i = 0; i < m_beamParticles.Length; ++i)
        {
            // Set particle data.
            particleSystems[i].SetParticles(m_particles, nParticleAmount);

            // Set origin and length in shader.
            renderers[i].material.SetVector("_LineOrigin", v3Origin);
            renderers[i].material.SetFloat("_LineLength", fLength);
        }
    }

    void LateUpdate()
    {
        if (PauseMenu.IsPaused())
            return;

        if (m_bBeamUnlocked)
        {
            m_bCanCast = m_bBeamEnabled || (Input.GetMouseButtonDown(1) && m_fBeamCharge >= m_fMinBeamCharge);

            m_bBeamEnabled = Input.GetMouseButton(1) && m_bCanCast && m_fBeamCharge >= 0.0f;

            // Enable/Disable effects.
            for (int i = 0; i < m_beamParticles.Length; ++i)
            {
                m_beamParticleRenderers[i].enabled = m_bBeamEnabled;
            }

            if (m_bBeamEnabled)
            {
                // Use the player controller's look rotation, to avoid offsets from camera shake.
                m_beamOrigin.rotation = m_controller.LookRotation();

                // Apply camera shake.
                m_camEffects.ApplyShake(0.1f, 0.3f);

                // Play SFX loops.
                if (!m_fireAudioLoop.IsPlaying())
                    m_fireAudioLoop.Play();

                if (!m_impactAudioLoop.IsPlaying())
                    m_impactAudioLoop.Play();

                const int nRaymask = ~(1 << 2); // Layer bitmask includes every layer but the ignore raycast layer.
                Ray sphereRay = new Ray(m_beamOrigin.position, m_beamOrigin.forward);
                bool bRayHit = Physics.Raycast(sphereRay, out m_sphereCastHit, (float)m_nBeamLength, nRaymask, QueryTriggerInteraction.Ignore);

                // Find end point of the line.
                if (bRayHit)
                {
                    // Update volumetric beam particle positions.
                    UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_particles, transform.position, m_sphereCastHit.distance, true);

                    float fHitProj = Vector3.Dot(m_sphereCastHit.point, m_beamOrigin.forward);
                    float fOriginProj = Vector3.Dot(m_beamOrigin.position, m_beamOrigin.forward);

                    float fProjDiff = fHitProj - fOriginProj;

                    m_endObj.transform.position = m_beamOrigin.position + (m_beamOrigin.forward * fProjDiff);
                }
                else
                {
                    // Update volumetric beam particle positions.
                    UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_particles, transform.position, m_nBeamLength, false);

                    m_endObj.transform.position = m_beamOrigin.position + (m_beamOrigin.forward * m_nBeamLength);
                }

                // Reduce beam charge.
                m_fBeamCharge -= m_fChargeLossRate * Time.deltaTime;
            }
            else if (m_fireAudioLoop.IsPlaying() || m_impactAudioLoop.IsPlaying())
            {
                m_fireAudioLoop.Stop();
                m_impactAudioLoop.Stop();
            }
        }

        m_fCurrentMeterLevel = Mathf.MoveTowards(m_fCurrentMeterLevel, m_fBeamCharge, 30.0f * Time.deltaTime);

        // Set GUI material property.
        m_guiMaterial.SetFloat("_Resource", m_fCurrentMeterLevel / m_fMaxBeamCharge);
    }

    /*
    Description: Set whether or not to enable the player's ability to use the beam.
    */
    public void UnlockBeam(bool bUnlock)
    {
        m_bBeamUnlocked = bUnlock;
        m_bBeamEnabled = false;
        enabled = bUnlock;
    }

    /*
    Description: Get whether or not the beam is unlocked.
    */
    public bool BeamUnlocked()
    {
        return m_bBeamUnlocked;
    }

    /*
    Description: Get whether or not the beam is currently in use. 
    */
    public bool BeamEnabled()
    {
        return m_bBeamEnabled;
    }

    /*
    Description: Increase the beam charge level by the specified amount.
    Param:
       float fCharge: The charge level to add.
    */
    public void IncreaseCharge(float fCharge)
    {
        m_fBeamCharge = Mathf.Min(m_fBeamCharge + fCharge, m_fMaxBeamCharge);
    }
}
