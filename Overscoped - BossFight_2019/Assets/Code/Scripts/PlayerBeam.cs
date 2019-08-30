using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBeam : MonoBehaviour
{
    [Tooltip("Beam GUI material.")]
    [SerializeField]
    private Material m_guiMaterial = null;

    [Tooltip("Maximum beam charge value.")]
    [SerializeField]
    private float m_fMaxBeamCharge = 100.0f;

    [Tooltip("Loss rate of beam charge when firing it.")]
    [SerializeField]
    private float m_fChargeLossRate = 20.0f;

    [Tooltip("Minimum beam charge cost to use the beam.")]
    [SerializeField]
    private float m_fMinBeamCharge = 15.0f;

    [Tooltip("Rate of which beam charge will replenish.")]
    [SerializeField]
    private float m_fRechargeRate = 50.0f;

    [Tooltip("Delay before beam recharge.")]
    [SerializeField]
    private float m_fRechargeDelay = 1.0f;

    [Tooltip("Beam line renderer reference.")]
    [SerializeField]
    private LineRenderer m_beamLine = null;

    [Tooltip("Start point of the beam effect.")]
    [SerializeField]
    private Transform m_beamOrigin = null;

    [Tooltip("Maximum range of the beam.")]
    [SerializeField]
    private float m_fBeamRange = 500.0f;

    [Tooltip("Reference to the boss chestplate script.")]
    [SerializeField]
    private ChestPlate m_bossChestScript;

    private PlayerController m_controller;
    private CameraEffects m_camEffects;
    private RaycastHit m_sphereCastHit;
    private Vector3 m_v3BeamDestination;
    private float m_fBeamCharge;
    private float m_fCurrentRechargeDelay;
    private bool m_bCanCast;
    private bool m_bBeamUnlocked;

    void Awake()
    {
        m_controller = GetComponent<PlayerController>();
        m_camEffects = GetComponentInChildren<CameraEffects>();
        m_beamLine.enabled = false;

        m_fBeamCharge = m_fMaxBeamCharge;

        m_bBeamUnlocked = true;
    }
    
    void LateUpdate()
    {
        m_bCanCast = m_beamLine.enabled || (Input.GetMouseButtonDown(1) && m_fBeamCharge >= m_fMinBeamCharge);

        m_beamLine.enabled = Input.GetMouseButton(1) && m_bCanCast && m_fBeamCharge >= 0.0f;

        if (m_bBeamUnlocked && m_beamLine.enabled)
        {
            // Use the player controller's look rotation, to avoid offsets from camera shake.
            m_beamOrigin.rotation = m_controller.LookRotation();

            // Apply camera shake.
            m_camEffects.ApplyShake(0.1f, 0.3f);

            const int nRaymask = ~(1 << 2); // Layer bitmask includes every layer but the ignore raycast layer.
            Ray sphereRay = new Ray(m_beamOrigin.position, m_beamOrigin.forward);
            bool bRayHit = Physics.Raycast(sphereRay, out m_sphereCastHit, m_fBeamRange, nRaymask, QueryTriggerInteraction.Ignore);

            // Find end point of the line.
            if (bRayHit)
            {
                float fHitProj = Vector3.Dot(m_sphereCastHit.point, m_beamOrigin.forward);
                float fOriginProj = Vector3.Dot(m_beamOrigin.position, m_beamOrigin.forward);

                float fProjDiff = fHitProj - fOriginProj;

                // Check if boss chestplate was hit, and if so deal damage.
                if (m_sphereCastHit.collider.gameObject == m_bossChestScript.gameObject)
                    m_bossChestScript.DealBeamDamage();

                m_v3BeamDestination = m_beamOrigin.position + (m_beamOrigin.forward * fProjDiff);
            }
            else
            {
                m_v3BeamDestination = m_beamOrigin.position + (m_beamOrigin.forward * m_fBeamRange);
            }

            // Set linerenderer points.
            m_beamLine.SetPosition(0, m_beamOrigin.position);
            m_beamLine.SetPosition(1, m_v3BeamDestination);

            // Beam charge.
            m_fBeamCharge -= m_fChargeLossRate * Time.deltaTime;
            m_fCurrentRechargeDelay = m_fRechargeDelay;

            // Set GUI material property.
            m_guiMaterial.SetFloat("_Resource", m_fBeamCharge / m_fMaxBeamCharge);
        }
        else if(m_bBeamUnlocked)
        {
            m_fCurrentRechargeDelay -= Time.deltaTime;

            if(m_fCurrentRechargeDelay <= 0.0f && m_fBeamCharge < m_fMaxBeamCharge)
            {
                m_fBeamCharge += m_fRechargeRate * Time.deltaTime;

                // Clamp beam charge level to max.
                m_fBeamCharge = Mathf.Clamp(m_fBeamCharge, 0.0f, m_fMaxBeamCharge);
            }

            // Set GUI material property.
            m_guiMaterial.SetFloat("_Resource", m_fBeamCharge / m_fMaxBeamCharge);
        }
    }

    /*
    Description: Set whether or not to enable the player's ability to use the beam.
    */
    public void UnlockBeam(bool bUnlock)
    {
        m_bBeamUnlocked = bUnlock;
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
        return m_beamLine.enabled;
    }
}
