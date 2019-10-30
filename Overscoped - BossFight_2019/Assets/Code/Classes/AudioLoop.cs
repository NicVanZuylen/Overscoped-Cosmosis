using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Helper class for creating audio loops with Unity's audio system.
 * Author: Nic Van Zuylen
*/

public enum ESpacialMode
{
    AUDIO_SPACE_WORLD,
    AUDIO_SPACE_NONE
}

public class AudioLoop
{
    private AudioSource m_source;
    private bool m_bClipIsNull;

    public AudioLoop(AudioClip clip, GameObject obj, ESpacialMode mode = ESpacialMode.AUDIO_SPACE_WORLD, float fAttenuationDist = 100.0f)
    {
        // Add audio source to the provided object.
        obj.AddComponent<AudioSource>();
        AudioSource[] sources = obj.GetComponents<AudioSource>();
        m_source = sources[sources.Length - 1];

        m_bClipIsNull = clip == null;

        // Set clip and source settings.
        if(!m_bClipIsNull)
            m_source.clip = clip;

        m_source.playOnAwake = false;
        m_source.minDistance = 1.0f;
        m_source.maxDistance = fAttenuationDist;
        m_source.loop = true;
        m_source.enabled = !m_bClipIsNull;

        switch(mode)
        {
            case ESpacialMode.AUDIO_SPACE_WORLD:

                m_source.spatialBlend = 1.0f;
                m_source.spatialize = true;

                break;

            case ESpacialMode.AUDIO_SPACE_NONE:

                m_source.spatialBlend = 0.0f;

                break;
        }
    }

    /*
    Description: Get the audio source component.
    Return Type: AudioSource
    */
    public AudioSource GetSource()
    {
        return m_source;
    }

    /*
    Description: Play the audio loop.
    Param:
        float fVolume: The volume to play the audio at.
    */
    public void Play(float fVolume = 1.0f)
    {
        if(!m_bClipIsNull)
        {
            m_source.volume = fVolume;
            m_source.Play();
        }
    }

    /*
    Description: Stop the audio loop.
    */
    public void Stop()
    {
        if(!m_bClipIsNull)
            m_source.Stop();
    }

    /*
    Description: Returns whether or not the audio loop is playing.
    Return Type: bool
    */
    public bool IsPlaying()
    {
        return m_source.isPlaying;
    }
}
