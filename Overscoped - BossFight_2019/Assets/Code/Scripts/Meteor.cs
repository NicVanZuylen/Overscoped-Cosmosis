using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class Meteor : MonoBehaviour
{
    [Tooltip("Reference to the player.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Explosion effect on impact.")]
    [SerializeField]
    private GameObject m_impactEffect = null;

    [Tooltip("Speed in which the meteor will travel.")]
    [SerializeField]
    private float m_fSpeed = 50.0f;

    private PlayerStats m_playerStats;
    private Rigidbody m_rigidBody;
    private MeteorTarget m_targetScript;
    private Vector3 m_v3Target;
    private Vector3 m_v3TravelDirection;

    // Impact effect object.
    private ParticleSystem m_impactEffectParticles;

    // Meteor spawn target
    private GameObject m_targetVolume;

    public void Init(GameObject player)
    {
        m_player = player;

        //Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_player.GetComponent<Collider>(), true);

        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_rigidBody = GetComponent<Rigidbody>();

        // Create impact explosion object and get particlesystem.
        if(m_impactEffect)
        {
            GameObject impactObj = Instantiate(m_impactEffect);
            impactObj.SetActive(false);
            m_impactEffectParticles = impactObj.GetComponent<ParticleSystem>();
        }

        m_v3Target = m_player.transform.position;

        GameObject[] m_targetObjects = GameObject.FindGameObjectsWithTag("MeteorSpawn");
        
        // Ignore collisions with spawn volumes.
        //SphereCollider thisCollider = GetComponent<SphereCollider>();
        //for (int i = 0; i < m_targetObjects.Length; ++i)
        //{
        //    Physics.IgnoreCollision(m_targetObjects[i].GetComponentInChildren<Collider>(), thisCollider, true);
        //    Physics.IgnoreCollision(m_targetObjects[i].GetComponent<Collider>(), thisCollider, true);
        //}
    }   //

    void Update()
    {
        // Make meteor move toward player
        transform.position += m_v3TravelDirection * m_fSpeed * Time.deltaTime;

        m_rigidBody.velocity = Vector3.zero;

        Quaternion rotation = Quaternion.LookRotation(m_v3TravelDirection);

        transform.rotation = Quaternion.Euler(transform.rotation.x, rotation.y, transform.rotation.z); ;
    }

    public void Summon(Vector3 v3Origin, MeteorTarget targetScript)
    {
        gameObject.SetActive(true);

        gameObject.GetComponent<ParticleSystem>().Play();

        m_targetScript = targetScript;

        m_v3Target = targetScript.transform.position;
        m_targetVolume = targetScript.gameObject;

        // Initial position.
        transform.position = v3Origin;

        // Direction of travel.
        m_v3TravelDirection = (m_v3Target - transform.position).normalized;  
    }

    void OnTriggerEnter(Collider collider)
    {
        if(collider.tag == "MeteorSpawn")
        {
            gameObject.SetActive(false);

            // Play impact effect.
            if (m_impactEffectParticles)
            {
                m_impactEffect.transform.position = transform.position;
                m_impactEffect.SetActive(true);
                m_impactEffectParticles.Play();
            }

            // Begin AOE hazard.
            m_targetScript.StartAOE();
        }
    }
}
