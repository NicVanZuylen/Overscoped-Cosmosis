using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class Meteor : MonoBehaviour
{
    [Tooltip("Explosion effect on impact.")]
    [SerializeField]
    private ParticleObject m_impactEffect = new ParticleObject();

    [Tooltip("Passive particle effect while the meteor is active.")]
    [SerializeField]
    private ParticleObject m_passiveEffect = new ParticleObject();

    [Tooltip("Speed in which the meteor will travel.")]
    [SerializeField]
    private float m_fSpeed = 50.0f;

    private GameObject m_player; // Player object reference.
    private PlayerStats m_playerStats; // Player stats script reference.
    private Rigidbody m_rigidBody; // Meteor rigidbody reference.
    private MeteorTarget m_targetScript; // Target script reference.
    private Vector3 m_v3Target; // Target position.
    private Vector3 m_v3TravelDirection; // Direction towards target.

    // Meteor spawn target
    private GameObject m_targetVolume;

    public void Init(GameObject player)
    {
        m_player = player;
        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_rigidBody = GetComponent<Rigidbody>();

        m_v3Target = m_player.transform.position;

        GameObject[] m_targetObjects = GameObject.FindGameObjectsWithTag("MeteorSpawn");
    }   

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

        if(!m_passiveEffect.IsPlaying())
            m_passiveEffect.Play();

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
            // Stop passive effect.
            if (m_passiveEffect.IsPlaying())
                m_passiveEffect.Stop();

            gameObject.SetActive(false);

            // Play impact effect.
            m_impactEffect.SetPosition(transform.position);

            if(!m_impactEffect.IsPlaying())
                m_impactEffect.Play();

            // Begin AOE hazard.
            m_targetScript.StartAOE();
        }
    }
}
