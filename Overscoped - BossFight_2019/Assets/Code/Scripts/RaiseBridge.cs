using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaiseBridge : MonoBehaviour
{
    [SerializeField]
    private Animator[] m_animators = null;

    private void Awake()
    {
        for(int i = 0; i < m_animators.Length; ++i)
            m_animators[i].SetFloat("RisingSpeed", 0.0f);
    }

    public void Raise()
    {
        for (int i = 0; i < m_animators.Length; ++i)
            m_animators[i].SetFloat("RisingSpeed", 1.0f);

        enabled = false;
    }
}
