using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Helper class for enable and disable multiple particlesystem VFX.
 * Author: Nic Van Zuylen
*/

[System.Serializable]
public struct ParticleObject
{
    [Tooltip("Particle effects to be controlled by this particle object.")]
    public ParticleSystem[] m_particleSystems;

    [Tooltip("GameObjects to be enabled and disabled alongside particle effects.")]
    public GameObject[] m_objects;

    private bool m_bPlaying;

    /*
    Description: Set position of the VFX.
    Param:
        Vector3 v3Position: The new position for the VFX.
    */
    public void SetPosition(Vector3 v3Position)
    {
        for (int i = 0; i < m_particleSystems.Length; ++i)
            m_particleSystems[i].transform.position = v3Position;
    }

    /*
    Description: Play all included particlesystems.
    */
    public void Play()
    {
        for (int i = 0; i < m_objects.Length; ++i)
            m_objects[i].SetActive(true);
        for (int i = 0; i < m_particleSystems.Length; ++i)
        {
            m_particleSystems[i].Play(true);
        }


        m_bPlaying = true;
    }

    /*
    Description: Stop all included particlesystems.
    */
    public void Stop(ParticleSystemStopBehavior stopBehaviour = ParticleSystemStopBehavior.StopEmitting)
    {
        for (int i = 0; i < m_particleSystems.Length; ++i)
            m_particleSystems[i].Stop(true, stopBehaviour);

        for (int i = 0; i < m_objects.Length; ++i)
            m_objects[i].SetActive(false);

        m_bPlaying = false;
    }

    /*
    Description: Whether or not the VFX are playing.
    Return Type: bool
    */
    public bool IsPlaying()
    {
        return m_bPlaying;
    }
}
