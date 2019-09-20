using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorManager : MonoBehaviour
{
    public List<MeteorInitialize> m_meteors;
    public void Add(MeteorInitialize meteor)
    {
        m_meteors.Add(meteor);
    }

    public void Reset()
    {
        m_meteors = new List<MeteorInitialize>();
    }

    public void SummonList()
    {
        //int randomMeteor = Random.Range(0, m_meteors.Count);

        for(int i = 0; i < m_meteors.Count; i++)
        {
            m_meteors[i].Summon();
        }
    }
}
