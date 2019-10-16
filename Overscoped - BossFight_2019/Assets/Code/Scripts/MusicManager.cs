using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Tooltip("Music tracks to play in this scene.")]
    [SerializeField]
    private AudioClip[] m_tracks = null;

    [Tooltip("Whether or not to cross fade tracks when changing them.")]
    [SerializeField]
    private bool m_bCrossFadeTracks = true;

    [Tooltip("Duration of audio cossfade between songs.")]
    [SerializeField]
    private float m_fCrossFadeTime = 5.0f;

    [Header("Sources: Both ideally should be at the same location.")]

    [Tooltip("The first audio source to use for music, they are swapped during crossfade.")]
    [SerializeField]
    private AudioSource m_firstSource = null;

    [Tooltip("The second audio source to use for music, they are swapped during crossfade.")]
    [SerializeField]
    private AudioSource m_secondSource = null;

    private AudioSource m_lastSource;
    private AudioSource m_currentSource;
    private float m_fCrossFadeProgress;

    private void Awake()
    {
        m_firstSource.playOnAwake = false;
        m_secondSource.playOnAwake = false;
        m_firstSource.loop = true;
        m_secondSource.loop = true;

        m_currentSource = m_firstSource;
        m_lastSource = m_secondSource;

        enabled = false;
    }

    private void Update()
    {
        m_fCrossFadeProgress += Time.unscaledDeltaTime;

        float fUnitProgress = m_fCrossFadeProgress / m_fCrossFadeTime;

        // Raise volume of current source as the last source is lowered.
        m_currentSource.volume = fUnitProgress;
        m_lastSource.volume = 1.0f - fUnitProgress;

        Debug.Log("Volume Now: " + m_currentSource.volume + " Volume Last: " + m_lastSource.volume);

        // Disable when finished.
        if(m_fCrossFadeProgress >= m_fCrossFadeTime)
        {
            m_lastSource.Stop();
            enabled = false;
        }
    }

    public void BeginCrossFade()
    {
        m_fCrossFadeProgress = 0.0f;
        enabled = true;
    }

    public void SetTrack(AudioClip track)
    {
        // Swap primary music sources.
        if (m_currentSource == m_firstSource)
        {
            m_lastSource = m_currentSource;
            m_currentSource = m_secondSource;
        }
        else
        {
            m_lastSource = m_currentSource;
            m_currentSource = m_firstSource;
        }

        m_currentSource.clip = track;
        m_currentSource.Play();

        if (!m_bCrossFadeTracks)
            m_lastSource.Stop();
        else
            BeginCrossFade();
    }

    /*
    Description: Play a random track out of the list of available tracks.
    */
    public void PlayRandomTrack()
    {
        int nRandSongIndex = Random.Range(0, m_tracks.Length);

        SetTrack(m_tracks[nRandSongIndex]);
    }

    /*
    Description: Play the track at the provided index.
    Param:
        int nIndex: The index of the track to play.
    */
    public void PlayTrackIndex(int nIndex)
    {
        SetTrack(m_tracks[nIndex]);
    }

    /*
    Description: Play the track with the provided name.
    Param:
        string sTrackName: The name of the track to play.
    */
    public void PlayTrackName(string sTrackName)
    {
        // Find matching track and play it.
        for(int i = 0; i < m_tracks.Length; ++i)
        {
            if(sTrackName == m_tracks[i].name)
            {
                SetTrack(m_tracks[i]);

                return;
            }
        }
    }
}
