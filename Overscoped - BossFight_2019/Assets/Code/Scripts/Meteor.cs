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
    private GameObject m_meteorAOEObj = null;

    [Tooltip("Amount of active meteor AOEs allowed at any given time.")]
    [SerializeField]
    private int m_nAOECount = 5;

    [Tooltip("Speed in which the meteor will travel.")]
    [SerializeField]
    private float m_fSpeed = 50.0f;

    [Tooltip("Damage dealt to the player on a direct hit.")]
    [SerializeField]
    private float m_fDirectHitDamage = 30.0f;

    private PlayerStats m_playerStats;
    public Transform m_childPlane;
    private Rigidbody m_rigidBody;
    private Vector3 m_v3Target;
    private Vector3 m_v3TravelDirection;

    // Meteor spawn target
    private GameObject m_targetVolume;

    // Meteor AOE pool
    private Stack<MeteorAOE> m_meteorAOEStack;

    private void Start()
    {
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_player.GetComponent<Collider>(),true);
    }

    public void Init()
    {
        //Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_player.GetComponent<Collider>(), true);

        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_rigidBody = GetComponent<Rigidbody>();
        m_childPlane = transform.GetChild(0);

        m_v3Target = m_player.transform.position;

        GameObject[] m_targetObjects = GameObject.FindGameObjectsWithTag("MeteorSpawn");
        
        // Ignore collisions with spawn volumes.
        SphereCollider thisCollider = GetComponent<SphereCollider>();
        for (int i = 0; i < m_targetObjects.Length; ++i)
        {
            Physics.IgnoreCollision(m_targetObjects[i].GetComponentInChildren<Collider>(), thisCollider, true);
            Physics.IgnoreCollision(m_targetObjects[i].GetComponent<Collider>(), thisCollider, true);
        }
    }

    void Update()
    {
        // Make meteor move toward player
        transform.position += m_v3TravelDirection * m_fSpeed * Time.deltaTime;

        // Make the plane effect always face the player.
        m_childPlane.transform.LookAt(m_player.transform, Vector3.up);

        m_rigidBody.velocity = Vector3.zero;
    }

    public void Summon(Vector3 v3Origin, GameObject target)
    {
        gameObject.SetActive(true);

        

        m_v3Target = target.transform.position;
        m_targetVolume = target;

        // Initial position.
        transform.position = v3Origin;

        // Direction of travel.
        m_v3TravelDirection = (m_v3Target - transform.position).normalized;

    }

    void OnTriggerEnter(Collider collider)
    {
        // Deal damage to the player.
        if(collider.gameObject == m_player)
        {
            m_playerStats.DealDamage(m_fDirectHitDamage);
            gameObject.SetActive(false);
        }
        else if(collider.tag == "MeteorSpawn")
        {
            gameObject.SetActive(false);
            //Explosion, AOE particle
        }
    }
}
