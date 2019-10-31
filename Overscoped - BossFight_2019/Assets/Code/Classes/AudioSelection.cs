using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Helper class to assign and play arrays of sound FX.
 * Author: Nic Van Zuylen
*/

[System.Serializable]
public struct AudioSelection
{
    [Tooltip("Selection of playable clips.")]
    public AudioClip[] m_clips;

    [Tooltip("Audio source clips will play from.")]
    public AudioSource m_source;

    [Tooltip("Minimum time between playing cilps.")]
    public float m_fCooldown;

    private float m_fCurrentCD;
    private int m_nLastRandIndex;

    /*
    Description: Reduce cooldown over time.
    */
    public void CountCooldown()
    {
        m_fCurrentCD -= Time.deltaTime;
    }

    /*
    Description: Returns if the audio cooldown has expired.
    Return Type: bool
    */
    public bool CooldownExpired()
    {
        return m_fCooldown <= 0.0f;
    }

    /*
    Description: Play an audio clip at the given index.
    Param:
        int nIndex: The index of the clip to play. If the index is out of range play the final clip.
        float fVolume: The volume to play audio at.
    */
    public void PlayIndex(int nIndex, float fVolume = 1.0f)
    {
        if (m_clips.Length == 0 || m_fCurrentCD > 0.0f)
            return;

        // Restrict index to array length.
        if (nIndex >= m_clips.Length)
            nIndex = m_clips.Length - 1;

        // Check the clip exists and play it.
        if (m_clips[nIndex] && m_source)
        {
            m_source.PlayOneShot(m_clips[nIndex], fVolume);
            m_fCurrentCD = m_fCooldown;
        }
    }

    /*
    Description: Play a random audio clip.
    Param:
        float fVolume: The volume to play audio at.
    */
    public void PlayRandom(float fVolume = 1.0f)
    {
        if (m_clips.Length == 0 || m_fCurrentCD > 0.0f)
            return;

        int nRandIndex = Random.Range(0, m_clips.Length);

        // If the new random index is equal to the last find a random index in the range before or after it.
        if(nRandIndex == m_nLastRandIndex)
        {
            int nPartitionIndex = 0;

            // Select a partition of the array range before or after the new random index.
            if (m_nLastRandIndex == m_clips.Length - 1)
                nPartitionIndex = 0;
            else if (m_nLastRandIndex == 0)
                nPartitionIndex = 1;
            else
                nPartitionIndex = Random.Range(0, 2);

            // Pick a random index in the chosen partition.
            switch (nPartitionIndex)
            {
                case 0:
                    nRandIndex = Random.Range(0, m_nLastRandIndex);
                    break;

                case 1:
                    nRandIndex = Random.Range(m_nLastRandIndex + 1, m_clips.Length);
                    break;
            }
        }

        // Check the clip exists and play it.
        if (m_clips[nRandIndex] && m_source)
        {
            m_source.PlayOneShot(m_clips[nRandIndex], fVolume);
            m_fCurrentCD = m_fCooldown;
            m_nLastRandIndex = nRandIndex;
        }
    }
}
