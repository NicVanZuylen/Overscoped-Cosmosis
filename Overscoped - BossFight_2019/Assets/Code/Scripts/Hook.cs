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
    public float flySpeed = 5.0f;

    // Private:

    private SphereCollider m_collider;
    private PullObject m_pullObj;
    private EHookPullMode m_pullType;
    private bool m_lodged;

    void Awake()
    {
        m_collider = GetComponent<SphereCollider>();
        Physics.IgnoreCollision(m_collider, m_playerController, true);

        m_pullType = EHookPullMode.PULL_FLY_TOWARDS;
        m_lodged = false;
    }

    void Update()
    {
        if (!m_lodged)
            transform.Translate(transform.forward * flySpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!m_lodged)
        {
            Debug.Log(other.tag);

            if (other.tag == "PullObj")
            {
                m_pullObj = other.GetComponent<PullObject>();
                transform.parent = m_pullObj.transform;

                m_pullType = EHookPullMode.PULL_PULL_TOWARDS_PLAYER;
            }
            else
                m_pullType = EHookPullMode.PULL_FLY_TOWARDS;
        }

        m_lodged = true;
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
        return m_lodged;
    }

    public void UnLodge()
    {
        gameObject.SetActive(false);
        transform.parent = null;
        m_lodged = false;
    }

    private void OnEnable()
    {
        m_lodged = false;
    }
}
