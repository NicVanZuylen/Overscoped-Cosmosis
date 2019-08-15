using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class PullObject : MonoBehaviour
{
    // Private: 

    [SerializeField]
    private float m_pullHealth;

    [SerializeField]
    private Transform m_decoupleVector = null;

    [SerializeField]
    private float m_decoupleForce = 5.0f;

    private Rigidbody m_rigidbody;
    private bool m_coupled;

    void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_rigidbody.isKinematic = true;
        m_coupled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsCoupled()
    {
        return m_coupled;
    }

    public void Decouple(Vector3 playerDirection)
    {
        m_rigidbody.isKinematic = false;
        m_coupled = false;
        transform.parent = null;

        if(m_decoupleVector != null)
            m_rigidbody.AddForce((m_decoupleVector.forward + playerDirection) * m_decoupleForce, ForceMode.Impulse);
        else
            m_rigidbody.AddForce(playerDirection * m_decoupleForce, ForceMode.Impulse);
    }

    public void Damage(Vector3 playerDirection, float damage)
    {
        m_pullHealth -= damage;

        if (m_pullHealth <= 0.0f)
        {
            Decouple(playerDirection);
        }
    }
}
