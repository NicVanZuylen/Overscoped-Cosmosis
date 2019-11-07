using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
struct TrackGroup
{
    public AudioClip[] m_tracks;

    public TrackGroup(int nTrackCount)
    {
        m_tracks = new AudioClip[nTrackCount];
    }

    public AudioClip this[int nIndex]
    {
        get => m_tracks[nIndex];
    }
}

public class MusicManager : MonoBehaviour
{
    [Tooltip("Array of groups of music tracks to play in this scene.")]
    [SerializeField]
    private TrackGroup[] m_tracks = null;

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
    private int m_nLastRandIndex;

    private void Awake()
    {
        if (m_tracks.Length == 0)
            m_tracks = new TrackGroup[1];

        m_firstSource.playOnAwake = false;
        m_secondSource.playOnAwake = false;
        m_firstSource.loop = true;
        m_secondSource.loop = true;

        m_currentSource = m_firstSource;
        m_lastSource = m_secondSource;

        m_fCrossFadeProgress = 0.0f;
        m_nLastRandIndex = -1;

        enabled = false;
    }

    private void Update()
    {
        m_fCrossFadeProgress += Time.unscaledDeltaTime;

        float fUnitProgress = m_fCrossFadeProgress / m_fCrossFadeTime;

        // Raise volume of current source as the last source is lowered.
        m_currentSource.volume = fUnitProgress;
        m_lastSource.volume = 1.0f - fUnitProgress;

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
    Param:
        int nGroup: Index of the group of tracks to play from.
    */
    public void PlayRandomTrack(int nGroup = 0)
    {
        // Exit if there are no tracks to play.
        if (m_tracks[nGroup].m_tracks.Length == 0)
            return;

        int nRandSongIndex = Random.Range(0, m_tracks.Length);

        // If the new random index is equal to the last find a random index in the range before or after it.
        if (nRandSongIndex == m_nLastRandIndex)
        {
            int nPartitionIndex = 0;

            // Select a partition of the array range before or after the new random index.
            if (m_nLastRandIndex == m_tracks[nGroup].m_tracks.Length - 1)
                nPartitionIndex = 0;
            else if (m_nLastRandIndex == 0)
                nPartitionIndex = 1;
            else
                nPartitionIndex = Random.Range(0, 2);

            // Pick a random index in the chosen partition.
            switch (nPartitionIndex)
            {
                case 0:
                    nRandSongIndex = Random.Range(0, m_nLastRandIndex);
                    break;

                case 1:
                    nRandSongIndex = Random.Range(m_nLastRandIndex + 1, m_tracks[nGroup].m_tracks.Length);
                    break;
            }
        }

        // Remember last random song index.
        m_nLastRandIndex = nRandSongIndex;

        SetTrack(m_tracks[nGroup][nRandSongIndex]);
    }

    /*
    Description: Play the track at the provided index.
    Param:
        int nIndex: The index of the track to play.
        int nGroup: Index of the group of tracks to play from.
    */
    public void PlayTrackIndex(int nIndex, int nGroup = 0)
    {
        // Exit if there are no tracks to play.
        if (m_tracks[nGroup].m_tracks.Length == 0)
            return;

        SetTrack(m_tracks[nGroup][nIndex]);
    }

    /*
    Description: Play the track with the provided name.
    Param:
        string sTrackName: The name of the track to play.
        int nGroup: Index of the group of tracks to play from.
    */
    public void PlayTrackName(string sTrackName, int nGroup = 0)
    {
        // Exit if there are no tracks to play.
        if (m_tracks[nGroup].m_tracks.Length == 0)
            return;

        // Find matching track and play it.
        for (int i = 0; i < m_tracks.Length; ++i)
        {
            if(sTrackName == m_tracks[nGroup][i].name)
            {
                SetTrack(m_tracks[nGroup][i]);

                return;
            }
        }
    }
}
