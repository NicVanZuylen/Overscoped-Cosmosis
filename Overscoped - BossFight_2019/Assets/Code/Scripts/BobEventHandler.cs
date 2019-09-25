using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobEventHandler : MonoBehaviour
{
    private Animator m_animator;
    private CameraEffects m_camEffects;

    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_camEffects = GetComponentInParent<CameraEffects>();
    }

    public void Bob() // Called by animation event.
    {
        if (m_animator.GetAnimatorTransitionInfo(0).IsName("Run -> Idle"))
            return;

        m_camEffects.Step();
    }
}
