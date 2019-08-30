using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meteor : MonoBehaviour
{
    [Tooltip("Reference to the player.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Reference to the Meteror's AOE")]
    [SerializeField]
    private MeteorAOE m_meteorAOE = null;

    [Tooltip("Speed in which the meteor will travel.")]
    [SerializeField]
    private float m_fSpeed = 50.0f;

    [Tooltip("Damage dealt to the player on a direct hit.")]
    [SerializeField]
    private float m_fDirectHitDamage = 30.0f;

    private PlayerStats m_playerStats;
    private Transform m_childPlane;
    private Rigidbody m_rigidBody;
    private Vector3 m_v3Target;
    private Vector3 m_v3TravelDirection;

    void Awake()
    {
        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_rigidBody = GetComponent<Rigidbody>();
        m_childPlane = transform.GetChild(0);

        m_v3Target = m_player.transform.position;

        GameObject[] m_targetObjects = GameObject.FindGameObjectsWithTag("MeteorSpawn");

        SphereCollider thisCollider = GetComponent<SphereCollider>();

        for (int i = 0; i < m_targetObjects.Length; ++i)
            Physics.IgnoreCollision(m_targetObjects[i].GetComponent<BoxCollider>(), thisCollider, true);
    }

    // Update is called once per frame
    void Update()
    {
        // Make meteor move toward player
        transform.position += m_v3TravelDirection * m_fSpeed * Time.deltaTime;

        // Make the plane effect always face the player.
        m_childPlane.transform.LookAt(m_player.transform, Vector3.up);

        m_rigidBody.velocity = Vector3.zero;
    }

    public void Summon(Vector3 v3Origin, Vector3 v3Target)
    {
        gameObject.SetActive(true);

        m_v3Target = v3Target;

        // Initial position.
        transform.position = v3Origin;

        // Direction of travel.
        m_v3TravelDirection = (m_v3Target - transform.position).normalized;
    }

    void OnTriggerEnter(Collider collider)
    {
        m_meteorAOE.AOE(transform.position);

        // Deal damage to the player.
        if(collider.gameObject == m_player)
        {
            m_playerStats.DealDamage(m_fDirectHitDamage);
        }

        // Disable.
        gameObject.SetActive(false);
    }
}
