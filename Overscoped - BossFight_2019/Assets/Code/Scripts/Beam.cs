using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beam : MonoBehaviour
{
    public float m_fMaxLength = 1.0f;
    public int m_ParticleMeshLength = 1;
    public float m_fFinalSize = 1f;
    public bool scalingWithSize = true;

    //Length of the beam
    private float m_fHitLength;
    //particle system
    private ParticleSystem ps;
    //particle system renderer
    private ParticleSystemRenderer psr;
    //Position at the end of the beam
    private Vector3 m_v3EndPoint;
    //Positions of where the particles need to spawn
    [SerializeField]
    private Vector3[] m_v3ParticleSpawnPositions;
    [SerializeField]
    private ParticleSystem.Particle[] particles;
    private int m_PositionArrayLength;

    void BeamControl()
    {
        psr.material.SetVector("_StartPosition", transform.position);
        psr.material.SetVector("_EndPosition", m_v3EndPoint);
        psr.material.SetFloat("_Distance", m_fHitLength);
        psr.material.SetFloat("_MaxDist", m_fHitLength);
        psr.material.SetFloat("_FinalSize", m_fFinalSize);
    }

    void BeamCastRay()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, m_fMaxLength))
        {
            m_fHitLength = hit.distance;
            m_PositionArrayLength = Mathf.RoundToInt(hit.distance / (m_ParticleMeshLength * m_fFinalSize));
            if (m_PositionArrayLength < hit.distance)
                m_PositionArrayLength += 1;
            m_v3ParticleSpawnPositions = new Vector3[m_PositionArrayLength];
            m_v3EndPoint = hit.point;
        }
        else
        {

            m_fHitLength = m_fMaxLength;
            m_PositionArrayLength = Mathf.RoundToInt(m_fMaxLength / (m_ParticleMeshLength * m_fFinalSize));
            if(m_PositionArrayLength < m_fMaxLength)
                m_PositionArrayLength += 1;
            m_v3ParticleSpawnPositions = new Vector3[m_PositionArrayLength];
            m_v3EndPoint = Vector3.MoveTowards(transform.position, transform.forward * 1000f, m_fMaxLength);
        }
    }

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        psr = GetComponent<ParticleSystemRenderer>();
        m_fHitLength = 0;
        BeamCastRay();
        BeamControl();
        UpdateBeamParts();
    }

    void UpdateBeamParts()
    {
        particles = new ParticleSystem.Particle[m_PositionArrayLength];

        for(int i = 0; i< m_PositionArrayLength; i++)
        {
            m_v3ParticleSpawnPositions[i] = new Vector3(0f, 0f, 0f) + new Vector3(0f, 0f, i * m_ParticleMeshLength * m_fFinalSize);
            particles[i].position = m_v3ParticleSpawnPositions[i];
            particles[i].startSize = m_fFinalSize;
            particles[i].startColor = new Color(1f, 1f, 1f);
        }

        ps.SetParticles(particles, particles.Length);
    }


    void Update()
    {
        if(scalingWithSize == true)
        {
            m_fFinalSize = gameObject.transform.lossyScale.x;
        }
        BeamCastRay();
        BeamControl();
        if (m_PositionArrayLength != particles.Length)
        {
            UpdateBeamParts();
        }
    }
}
