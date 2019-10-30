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
    private Animator m_animator;
    private CameraEffects m_camEffects;
    private PlayerController m_controller;
    private GrappleHook m_grappleScript;
    private PlayerBeam m_beamScript;

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

    public void EvBob() // Called by animation event.
    {
        // Bail conditions...
        if (!m_controller.IsGrounded() || m_animator.GetAnimatorTransitionInfo(0).IsName("Run -> Idle"))
            return;

        // Play random footstep noise.
        m_footStepSFX.PlayRandom();

        Debug.Log("Bob!");

        m_camEffects.Step();
    }

    public void EvStartGrapple() // Event to tell the grapple script to begin the spell.
    {
        m_grappleScript.BeginGrapple();
    }

    public void EvStartBeam() // Event to tell the beam script to start the beam.
    {
        m_beamScript.StartBeam();
    }
}
