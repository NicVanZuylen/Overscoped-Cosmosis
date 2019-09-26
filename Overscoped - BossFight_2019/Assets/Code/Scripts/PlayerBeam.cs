﻿using System.Collections;
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

    [Tooltip("Beam line renderer reference.")]
    [SerializeField]
    private LineRenderer m_beamLine = null;

    [Tooltip("Start point of the beam effect.")]
    [SerializeField]
    private Transform m_beamOrigin = null;

    [Tooltip("Maximum range of the beam.")]
    [SerializeField]
    private float m_fBeamRange = 500.0f;

    private PlayerController m_controller;
    private CameraEffects m_camEffects;
    private RaycastHit m_sphereCastHit;
    private Vector3 m_v3BeamDestination;
    private float m_fBeamCharge; // Actual current charge value.
    private float m_fCurrentMeterLevel; // Current charge as displayed on the HUD.
    private int m_nDissolveBracerIndex;
    private bool m_bCanCast;
    private bool m_bBeamUnlocked;

    void Awake()
    {
        // Component retreival.
        m_controller = GetComponent<PlayerController>();
        m_camEffects = GetComponentInChildren<CameraEffects>();
        m_beamLine.enabled = false;

        m_bBeamUnlocked = true;

        m_fBeamCharge = m_fMaxBeamCharge;

    }

    void LateUpdate()
    {
        if (m_bBeamUnlocked)
        {
            m_bCanCast = m_beamLine.enabled || (Input.GetMouseButtonDown(1) && m_fBeamCharge >= m_fMinBeamCharge);

            m_beamLine.enabled = Input.GetMouseButton(1) && m_bCanCast && m_fBeamCharge >= 0.0f;

            if (m_beamLine.enabled)
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

                    m_v3BeamDestination = m_beamOrigin.position + (m_beamOrigin.forward * fProjDiff);
                }
                else
                {
                    m_v3BeamDestination = m_beamOrigin.position + (m_beamOrigin.forward * m_fBeamRange);
                }

                // Set linerenderer points.
                m_beamLine.SetPosition(0, m_beamOrigin.position);
                m_beamLine.SetPosition(1, m_v3BeamDestination);

                // Reduce beam charge.
                m_fBeamCharge -= m_fChargeLossRate * Time.deltaTime;
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
        m_beamLine.enabled = false;
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
        return m_beamLine.enabled;
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
