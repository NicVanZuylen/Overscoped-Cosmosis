using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorInitialize : MonoBehaviour
{
    //Reference to everything that the meteor needs
    public Meteor[] m_meteors;
    public GameObject m_indicator;
    public ParticleSystem m_explosion;
    public ParticleSystem m_meteorAOE;

    public List<Meteor> m_meteorInst;

    private Vector3 m_meteorSpawnPosition;
    public bool test = false;

    void Start()
    {
        //Sets the meteor spawn position to the first child(Which should be the MeteorSpawnPoint)
        m_meteorSpawnPosition = transform.GetChild(0).position;

        foreach(Meteor meteor in m_meteors)
        {

            Meteor meteorInst = Instantiate(meteor);

            meteorInst.Init();

            meteorInst.gameObject.SetActive(false);

            m_meteorInst.Add(meteorInst);

            meteor.gameObject.SetActive(false);
        }    
    }

    public void Summon()
    {
        Meteor meteor = m_meteorInst[Random.Range(0, m_meteorInst.Count)];

        meteor.Summon(m_meteorSpawnPosition, gameObject);
    }
}
