using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupRespawner : MonoBehaviour
{
    [Tooltip("Amount of time between pickup respawns.")]
    [SerializeField]
    private float m_fRespawnTime = 10.0f;

    private static Queue<BeamPickup> m_pickupPool;
    private static float m_fGlobalRespawnTimer;
    private static float m_fGlobalRespawnTime;

    private void Awake()
    {
        m_pickupPool = new Queue<BeamPickup>();
        m_fGlobalRespawnTime = m_fRespawnTime;
    }

    private void Update()
    {
        if (m_pickupPool.Count == 0)
            return;

        m_fGlobalRespawnTimer -= Time.deltaTime;

        if (m_fGlobalRespawnTimer <= 0.0f)
        {
            m_pickupPool.Dequeue().gameObject.SetActive(true);

            m_fGlobalRespawnTimer = m_fGlobalRespawnTime;
        }
    }

    /*
    Description: Enqueue a pickup for respawn.
    Param:
        BeamPickup pickup: The pickup to respawn.
    */
    public static void EnqueuePickup(BeamPickup pickup)
    {
        m_pickupPool.Enqueue(pickup);
    }

    /*
    Description: Set the amount of time between crystal respawns.
    Param:
        float fTime: The amount of time between respawns.
    */
    public static void SetRespawnTime(float fTime)
    {
        m_fGlobalRespawnTime = fTime;
    }

}
