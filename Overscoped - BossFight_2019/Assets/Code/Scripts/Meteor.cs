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
    private Transform m_childPlane;
    private Rigidbody m_rigidBody;
    private Vector3 m_v3Target;
    private Vector3 m_v3TravelDirection;

    // Meteor spawn target
    private GameObject m_targetVolume;

    // Meteor AOE pool
    private Stack<MeteorAOE> m_meteorAOEStack;

    private void Awake()
    {
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_player.GetComponent<Collider>());
    }

    public void Init(List<GameObject> spawnVolumePool)
    {
        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_rigidBody = GetComponent<Rigidbody>();
        m_childPlane = transform.GetChild(0);

        m_v3Target = m_player.transform.position;

        GameObject[] m_targetObjects = GameObject.FindGameObjectsWithTag("MeteorSpawn");

        // Ignore collisions with spawn volumes.
        SphereCollider thisCollider = GetComponent<SphereCollider>();
        for (int i = 0; i < m_targetObjects.Length; ++i)
        {
            Physics.IgnoreCollision(m_targetObjects[i].GetComponentInChildren<BoxCollider>(), thisCollider, true);
            Physics.IgnoreCollision(m_targetObjects[i].GetComponent<BoxCollider>(), thisCollider, true);
        }

        m_meteorAOEStack = new Stack<MeteorAOE>();

        // Create AOE hazards and add to the object pool.
        for (int i = 0; i < m_nAOECount; ++i)
        {
            GameObject newAOE = Instantiate(m_meteorAOEObj);
            newAOE.SetActive(false);

            MeteorAOE aoeScript = newAOE.GetComponent<MeteorAOE>();
            aoeScript.SetPools(m_meteorAOEStack, spawnVolumePool);

            m_meteorAOEStack.Push(aoeScript);
        }

        gameObject.SetActive(false);
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

    /*
    Description: Get whether or not the meteor is available, based upon if there are any available AOE hazards to spawn.
    Return Type: bool
    */
    public bool Available()
    {
        return m_meteorAOEStack.Count > 0;
    }

    
    void OnTriggerEnter(Collider collider)
    {
        // Remove AOE object from object pool and activate.
        m_meteorAOEStack.Pop().AOE(m_targetVolume);

        // Deal damage to the player.
        if(collider.gameObject == m_player)
        {
            m_playerStats.DealDamage(m_fDirectHitDamage);
        }

        // Disable.
        gameObject.SetActive(false);
    }
}
