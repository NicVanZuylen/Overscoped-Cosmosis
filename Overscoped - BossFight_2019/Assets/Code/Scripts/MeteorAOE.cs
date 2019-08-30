using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorAOE : MonoBehaviour
{
    [Tooltip("Reference to the player.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Damage dealt to the player overtime.")]
    [SerializeField]
    private float m_fAoeDPS = 10.0f;

    private PlayerStats m_playerStats;

    [Tooltip("Reference to the particle effect")]
    [SerializeField]
    private ParticleSystem m_AoeParticle = null;

    private float m_fWaitForParticleTimer;
    private float m_fWaitForParticleCooldown;

    GameObject m_occupiedSpawn;
    private List<GameObject> m_meteorSpawnPool;
    private Stack<MeteorAOE> m_inactiveStack;

    void Start()
    {
        m_player = GameObject.FindGameObjectWithTag("Player");

        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_fWaitForParticleCooldown = m_AoeParticle.main.duration;
        m_fWaitForParticleTimer = m_fWaitForParticleCooldown;
    }

    private void Update()
    {
        m_fWaitForParticleTimer -= Time.deltaTime;
        if(m_fWaitForParticleTimer <= 0)
        {
            // Return to pool and deactivate object.
            m_inactiveStack.Push(this);
            gameObject.SetActive(false);

            // Return meteor spawner to pool.
            m_meteorSpawnPool.Add(m_occupiedSpawn);
            m_occupiedSpawn = null;
        }
    }

    /*
    Description: Set object pools.
    Param:
        Stack<MeteorAOE> aoePool: The object pool for meteor AOE objects.
        List<GameObject> spawnPool: The object pool for meteorspawn volumes.
    */
    public void SetPools(Stack<MeteorAOE> aoePool, List<GameObject> spawnPool)
    {
        m_inactiveStack = aoePool;
        m_meteorSpawnPool = spawnPool;
    }

    /*
    Description: Activate the AOE hazard.
    Param:
        GameObject occupiedSpawn: The spawn volume in which the AOE hazard will occupy.
    */
    public void AOE(GameObject occupiedSpawn)
    {
        m_occupiedSpawn = occupiedSpawn;
        transform.position = m_occupiedSpawn.transform.position;

        m_fWaitForParticleTimer = m_fWaitForParticleCooldown;
        gameObject.SetActive(true);
        m_AoeParticle.Play();
    }


    void OnTriggerStay(Collider other)
    {
        // Deal damage to the player.
        if (other.gameObject == m_player)
        {
            m_playerStats.DealDamage(m_fAoeDPS * Time.deltaTime);
        }
    }
}
