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
    [Header("Effects")]

    [Tooltip("Transform of the end particles object.")]
    [SerializeField]
    private Transform m_endParticleTransform = null;

    [Tooltip("VFX played at the beam's origin point.")]
    [SerializeField]
    private ParticleObject m_originParticles = new ParticleObject();

    [Tooltip("VFX played at the beam impact point.")]
    [SerializeField]
    private ParticleObject m_impactParticles = new ParticleObject();

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
    private GrappleHook m_grappleScript;
    private CameraEffects m_camEffects;
    private Animator m_animator;
    private RaycastHit m_beamHit;
    private GameObject m_endObj;
    private ChestPlate m_bossChestScript;
    private float m_fBeamCharge; // Actual current charge value.
    private float m_fCurrentMeterLevel; // Current charge as displayed on the HUD.
    private int m_nDissolveBracerIndex;
    private const int m_nRayMask = ~(1 << 2); // Layer bitmask includes every layer but: IgnoreRaycast, NoGrapple.
    private bool m_bBeamUnlocked;
    private bool m_bCanCast;
    private bool m_bCasting;
    private bool m_bBeamEnabled;

    // Volumetric beam effect.
    private ParticleSystem[] m_beamParticles;
    private ParticleSystemRenderer[] m_beamParticleRenderers;
    private ParticleSystem.Particle[] m_particles;
    private float m_fMeshLength;

    // Impact particle effects.
    private ParticleSystem m_impactEffect;

    void Awake()
    {
        // Component retreival.
        m_controller = GetComponent<PlayerController>();
        m_grappleScript = GetComponent<GrappleHook>();
        m_camEffects = GetComponentInChildren<CameraEffects>();
        m_animator = GetComponentInChildren<Animator>();

        m_bBeamUnlocked = true;

#if UNITY_EDITOR
        m_fBeamCharge = 100000.0f;
#else
        m_fBeamCharge = 0.0f;
#endif

        m_beamParticles = new ParticleSystem[m_beamParticleObjects.Length];
        m_beamParticleRenderers = new ParticleSystemRenderer[m_beamParticleObjects.Length];

        for (int i = 0; i < m_beamParticleObjects.Length; ++i)
        {
            m_beamParticles[i] = m_beamParticleObjects[i].GetComponent<ParticleSystem>();
            m_beamParticleRenderers[i] = m_beamParticleObjects[i].GetComponent<ParticleSystemRenderer>();

            // Set origin and length in shader.
            m_beamParticleRenderers[i].material.SetVector("_LineOrigin", m_beamOrigin.position);
            m_beamParticleRenderers[i].material.SetFloat("_LineLength", 0.0f);
        }

        m_particles = new ParticleSystem.Particle[m_nBeamLength / 2];

        m_fMeshLength = 10.0f;

        m_endObj = new GameObject("Player_Beam_End_Point");

        // Set end particle effect parent object.
        if(m_endParticleTransform)
        {
            m_endParticleTransform.parent = m_endObj.transform;
            m_endParticleTransform.localPosition = Vector3.zero;
        }

        m_bossChestScript = GameObject.FindGameObjectWithTag("BossChest").GetComponentInChildren<ChestPlate>();

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
        float fLength: The maximum length of the beam.
        float fSegmentLength: The length of each mesh segment.
        bool bHit: Whether or not the beam hit a surface.
    */
    public static void UpdateParticlePositions(ParticleSystem[] particleSystems, ParticleSystemRenderer[] renderers, ParticleSystem.Particle[] particles, Vector3 v3Origin, float fMaxLength, float fSegmentLength, bool bHit)
    {
        int nParticleAmount = 0;

        // Loop through all particles/segments and set their positions.
        for(int i = 0; i < particles.Length; ++i)
        {
            float fOffset = i * fSegmentLength;

            // Set new offset.
            particles[i].position = new Vector3(0.0f, 0.0f, fOffset);
            particles[i].startSize3D = new Vector3(0.1f, 0.1f, 1.0f);
            particles[i].startColor = Color.white;
            ++nParticleAmount;

            // Stop once length is exceeded.
            if (fOffset + fSegmentLength >= fMaxLength)
            {
                particles[i].startSize3D = new Vector3(0.1f, 0.1f, (fMaxLength - fOffset) / fSegmentLength);

                break;
            }
        }

        for(int i = 0; i < particleSystems.Length; ++i)
        {
            // Set particle data.
            particleSystems[i].SetParticles(particles, nParticleAmount);

            // Set origin and length in shader.
            renderers[i].material.SetVector("_LineOrigin", v3Origin);
            renderers[i].material.SetFloat("_LineLength", fMaxLength);
        }
    }

    private void StopEffects()
    {
        // Stop volumetric beam effect.
        for (int i = 0; i < m_beamParticles.Length; ++i)
        {
            if(m_beamParticleRenderers[i].enabled)
                m_beamParticleRenderers[i].enabled = false;
        }

        // Stop audio and particle effects.
        if(m_fireAudioLoop.IsPlaying())
            m_fireAudioLoop.Stop();

        if(m_impactAudioLoop.IsPlaying())
            m_impactAudioLoop.Stop();

        // Stop animations.
        m_animator.SetBool("isBeamCasting", false);
        m_animator.SetBool("beamActive", false);

        if (m_originParticles.IsPlaying())
            m_originParticles.Stop(ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void LateUpdate()
    {
        if (PauseMenu.IsPaused())
            return;

        if (m_bBeamUnlocked)
        {
            m_bCanCast = m_fBeamCharge >= m_fMinBeamCharge && !m_grappleScript.GrappleActive();
            m_bCasting = m_bCanCast && !m_bBeamEnabled && Input.GetMouseButton(1);

            // Tell the animator whether or not to play to beam casting animation.
            m_animator.SetBool("isBeamCasting", m_bCasting);

            // Release condition.
            if (m_bBeamEnabled && (Input.GetMouseButtonUp(1) || m_fBeamCharge <= 0.0f))
            {
                // Stop beam and ensure beam charge does not fall below zero.

                if (m_fBeamCharge < 0.0f)
                    m_fBeamCharge = 0.0f;

                m_bBeamEnabled = false;
                m_animator.SetBool("beamActive", false);
            }

            if (m_bBeamEnabled)
            {
                // Enable volumetric effects.
                if (!m_beamParticleRenderers[0].enabled)
                {
                    for (int i = 0; i < m_beamParticles.Length; ++i)
                    {
                        m_beamParticleRenderers[i].enabled = true;
                    }
                }

                // Use the player controller's look rotation, to avoid offsets from camera shake.
                m_beamOrigin.rotation = m_controller.LookRotation();

                // Apply camera shake.
                m_camEffects.ApplyShake(0.1f, 0.3f);
                m_camEffects.ApplyChromAbbShake(0.1f, 0.1f, 0.5f);

                // Play origin effect.
                if (!m_originParticles.IsPlaying())
                    m_originParticles.Play();

                // Play SFX loops.
                if (!m_fireAudioLoop.IsPlaying())
                    m_fireAudioLoop.Play();

                if (!m_impactAudioLoop.IsPlaying())
                    m_impactAudioLoop.Play();

                Ray sphereRay = new Ray(m_beamOrigin.position, m_beamOrigin.forward);
                bool bRayHit = Physics.Raycast(sphereRay, out m_beamHit, (float)m_nBeamLength, m_nRayMask, QueryTriggerInteraction.Ignore);

                // Find end point of the line.
                if (bRayHit)
                {
                    // Play impact particle effects.
                    if (m_impactEffect && !m_impactEffect.isPlaying)
                        m_impactEffect.Play();

                    // Update volumetric beam particle positions.
                    UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_particles, m_beamOrigin.position, Mathf.Max(m_beamHit.distance, 2.0f), m_fMeshLength, true);

                    float fHitProj = Vector3.Dot(m_beamHit.point, m_beamOrigin.forward);
                    float fOriginProj = Vector3.Dot(m_beamOrigin.position, m_beamOrigin.forward);

                    float fProjDiff = fHitProj - fOriginProj;

                    m_endObj.transform.position = m_beamOrigin.position + (m_beamOrigin.forward * fProjDiff);

                    // Deal damage if the beam ray hits the force field.
                    if(m_beamHit.collider.tag == "BossChest")
                    {
                        m_bossChestScript.DealBeamDamage();
                    }
                }
                else
                {
                    // Stop impact effect.
                    if (m_impactEffect && m_impactEffect.isPlaying)
                        m_impactEffect.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    
                    // Update volumetric beam particle positions.
                    UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_particles, m_beamOrigin.position, m_nBeamLength, m_fMeshLength, false);

                    m_endObj.transform.position = m_beamOrigin.position + (m_beamOrigin.forward * m_nBeamLength);
                }

                // Reduce beam charge.
                m_fBeamCharge -= m_fChargeLossRate * Time.deltaTime;
            }
            else if(m_beamParticleRenderers[0].enabled)
            {
                // Stop all particle and sound FX.
                StopEffects();
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

        // Force disable effects.
        if(!bUnlock)
        {
            StopEffects();
        }
    }

    /*
    Description: Get whether or not the beam is unlocked.
    */
    public bool BeamUnlocked()
    {
        return m_bBeamUnlocked;
    }
    
    /*
    Description: Finish beam charge and enable the beam.
    */
    public void StartBeam()
    {
        m_bBeamEnabled = true;

        m_animator.SetBool("beamActive", true);
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
