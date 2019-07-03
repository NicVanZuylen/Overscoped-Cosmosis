using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]

public class Hook : MonoBehaviour
{
    public enum EHookPullMode
    {
        PULL_FLY_TOWARDS, // The player is pulled towards the object.
        PULL_PULL_TOWARDS_PLAYER // The object is pulled towards the player.
    }

    // Public:

    public CharacterController m_playerController;
    public float m_fFlySpeed = 5.0f;

    // Private:

    private SphereCollider m_collider;
    private PullObject m_pullObj;
    private EHookPullMode m_pullType;
    private bool m_bLodged;

    void Awake()
    {
        m_collider = GetComponent<SphereCollider>();
        Physics.IgnoreCollision(m_collider, m_playerController, true);

        m_pullType = EHookPullMode.PULL_FLY_TOWARDS;
        m_bLodged = false;
    }

    void Update()
    {
        if (!m_bLodged)
            transform.Translate(transform.forward * m_fFlySpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!m_bLodged)
        {
            if (other.tag == "PullObj")
            {
                m_pullObj = other.GetComponent<PullObject>();
                transform.parent = m_pullObj.transform;

                m_pullType = EHookPullMode.PULL_PULL_TOWARDS_PLAYER;
            }
            else
                m_pullType = EHookPullMode.PULL_FLY_TOWARDS;
        }

        m_bLodged = true;
    }

    public PullObject HookedObject()
    {
        return m_pullObj;
    }

    public EHookPullMode PullType()
    {
        return m_pullType;
    }

    public bool IsLodged()
    {
        return m_bLodged;
    }

    public void UnLodge()
    {
        gameObject.SetActive(false);
        transform.parent = null;
        m_bLodged = false;
    }

    private void OnEnable()
    {
        m_bLodged = false;
    }
}
