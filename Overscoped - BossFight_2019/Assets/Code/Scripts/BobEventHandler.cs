using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobEventHandler : MonoBehaviour
{
    private Animator m_animator;
    private CameraEffects m_camEffects;
    private PlayerController m_controller;

    [Header("Footstep sound FX")]

    [SerializeField]
    private AudioSource m_sfxSource = null;

    [SerializeField]
    private AudioClip[] m_footStepSFX = null;

    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_camEffects = GetComponentInParent<CameraEffects>();
        m_controller = GetComponentInParent<PlayerController>();
    }

    public void Bob() // Called by animation event.
    {
        // Bail conditions...
        if (!m_controller.IsGrounded() || m_animator.GetAnimatorTransitionInfo(0).IsName("Run -> Idle"))
            return;

        // Play random footstep sound effect.
        int nRandSFXIndex = Random.Range(0, m_footStepSFX.Length);

        if (m_footStepSFX.Length > 0 && m_footStepSFX[nRandSFXIndex])
            m_sfxSource.PlayOneShot(m_footStepSFX[nRandSFXIndex]);

        m_camEffects.Step();
    }
}
