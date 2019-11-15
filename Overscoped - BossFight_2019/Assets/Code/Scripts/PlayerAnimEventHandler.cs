using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Picks up bobbing events from the adjacent animator component.
 * Author: Nic Van Zuylen
*/

[RequireComponent(typeof(Animator))]

public class PlayerAnimEventHandler : MonoBehaviour
{
    private Animator m_animator; // Animator reference.
    private CameraEffects m_camEffects; // Camera effects controller script reference.
    private PlayerController m_controller; // Player controller script reference.
    private GrappleHook m_grappleScript; // Grapple controller script reference.
    private PlayerBeam m_beamScript; // Player beam controller script reference.

    [Header("Footstep sound FX")]

    [SerializeField]
    private AudioSelection m_footStepSFX = new AudioSelection();

    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_camEffects = GetComponentInParent<CameraEffects>();
        m_controller = GetComponentInParent<PlayerController>();
        m_grappleScript = GetComponentInParent<GrappleHook>();
        m_beamScript = GetComponentInParent<PlayerBeam>();
    }

    /*
    Description: Event to trigger a bob in the camera head bobbing effect.
    */
    public void EvBob()
    {
        // Bail conditions...
        if (!m_controller.IsGrounded() || m_animator.GetAnimatorTransitionInfo(0).IsName("Run -> Idle"))
            return;

        // Play random footstep noise.
        m_footStepSFX.PlayRandom(PlayerStats.GetPlayerVolume());

        m_camEffects.Step();
    }

    /*
    Description: Event to tell the grapple script to begin the spell.
    */
    public void EvStartGrapple()
    {
        m_grappleScript.BeginGrapple();
    }

    /*
    Description: Event to tell the beam script to start the beam charge FX.
    */
    public void EvStartBeamCharge()
    {
        m_beamScript.StartBeamCharge();
    }

    /*
    Description: Event to tell the beam script to start the beam.
    */
    public void EvStartBeam()
    {
        m_beamScript.StartBeam();
    }
}
