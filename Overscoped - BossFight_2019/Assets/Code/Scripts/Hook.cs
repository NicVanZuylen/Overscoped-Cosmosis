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
    private Bezier m_curve;
    private EHookPullMode m_pullType;
    private Vector3 m_v3Destination;
    private float m_fRopeLength;
    private float m_fFlyProgress;
    private const int m_nCurveCount = 2;
    private bool m_bLodged;

    void Awake()
    {
        m_collider = GetComponent<SphereCollider>();
        Physics.IgnoreCollision(m_collider, m_playerController, true);

        m_pullType = EHookPullMode.PULL_FLY_TOWARDS;

        m_fRopeLength = Mathf.Infinity;
        m_bLodged = false;
    }

    void Update()
    {
        //if (!m_bLodged)
          //  transform.Translate(transform.forward * m_fFlySpeed * Time.deltaTime, Space.World);

        if(!m_bLodged)
        {
            m_fFlyProgress += m_fFlySpeed * Time.deltaTime;
            transform.position = m_curve.Evaluate(FlyProgress());

            if(m_fFlyProgress >= m_fRopeLength)
            {
                transform.position = m_v3Destination;

                m_bLodged = true;
            }
        }
    }

    public void SetTarget(Vector3 v3Destination, float fDistance)
    {
        m_v3Destination = v3Destination;
        m_fRopeLength = fDistance;
    }

    public void SetCurve(Bezier curve)
    {
        m_curve = curve;
    }

    public Vector3 Destination()
    {
        return m_v3Destination;
    }

    public float FlyProgress()
    {
        return m_fFlyProgress / m_fRopeLength;
    }

    public PullObject HookedObject()
    {
        return m_pullObj;
    }

    public void SetPullType(EHookPullMode pullType)
    {
        m_pullType = pullType;
    }

    public EHookPullMode GetPullType()
    {
        return m_pullType;
    }

    public bool IsLodged()
    {
        return m_bLodged;
    }

    public void UnLodge()
    {
        transform.parent = null;
        m_fFlyProgress = 0.0f;
        m_bLodged = false;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        m_fFlyProgress = 0.0f;
        m_bLodged = false;
    }
}
