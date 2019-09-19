using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorInitialize : MonoBehaviour
{
    //Reference to everything that the meteor needs
    public GameObject[] m_meteors;
    public GameObject m_indicator;
    public ParticleSystem m_explosion;
    public ParticleSystem m_meteorAOE;

    private Transform m_meteorSpawnPosition;
    void Start()
    {
        //Sets the meteor spawn position to the first child(Which should be the MeteorSpawnPoint)
        m_meteorSpawnPosition = transform.GetChild(0);
    }

    void Update()
    {
        
    }
}
