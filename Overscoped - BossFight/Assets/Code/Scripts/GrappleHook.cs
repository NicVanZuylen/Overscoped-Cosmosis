using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]

public class GrappleHook : MonoBehaviour
{
    // Public:

    public GameObject m_grappleHook;
    public GameObject m_pullHook;
    public LineRenderer m_grappleLine;
    public LineRenderer m_pullLine;
    public Transform m_grappleNode;
    public Transform m_pullNode;

    [Tooltip("Acceleration due to grapple pull.")]
    public float m_flyAcceleration = 30.0f;

    [Tooltip("Maximum speed in the direction of the grapple line the player can travel.")]
    public float m_maxFlySpeed = 40.0f;

    [Tooltip("Radius from the hook in which the player will detach.")]
    public float m_destinationRadius = 2.0f;

    [Tooltip("Allowance of lateral movement when grappling.")]
    public float m_driftTolerance = 8.0f;

    [Tooltip("Force exerted upwards upon the player when they reach the grapple destination.")]
    public float m_pushUpForce = 4.0f;

    [Tooltip("Player move acceleration when landing after successfully reaching the grapple hook.")]
    public float m_landMoveAcceleration = 5.0f;

    [Tooltip("The distance the grapple hook will travel before cancelling.")]
    public float m_breakDistance = 20.0f;

    [Tooltip("Whether or not the player will be restricted a spherical volume with the rope length as the radius when grappling.")]
    public bool m_restrictToRopeLength = true;

    // Private:

    private PlayerController m_controller;
    private Hook m_graphookScript;
    private Hook m_pullHookScript;
    private Vector3 m_grapplePoint;
    private Vector3 m_pullTension;
    private float m_grapRopeLength;
    private float m_pullRopeLength;
    private bool m_grappleHookActive;
    private bool m_pullHookActive;
    private bool m_grapRopeLenCalculated;
    private bool m_pullRopeLenCalculated;

    void Awake()
    {
        m_controller = GetComponent<PlayerController>();

        m_graphookScript = m_grappleHook.GetComponent<Hook>();
        m_pullHookScript = m_pullHook.GetComponent<Hook>();

        m_grappleHook.SetActive(false);

        m_grapplePoint = Vector3.zero;
        m_grappleHookActive = false;
        m_pullHookActive = false;
        m_grapRopeLenCalculated = false;
        m_pullRopeLenCalculated = false;
    }

    void Update()
    {
        if(!m_grappleHookActive && Input.GetMouseButtonDown(0))
        {
            // Fire grapple hook.
            m_grappleHook.SetActive(true);

            m_grappleHook.transform.position = m_grappleNode.position;
            m_grappleHook.transform.rotation = m_grappleNode.parent.rotation;

            m_controller.FreeOverride();

            m_grappleHookActive = true;
        }

        if (!m_pullHookActive && Input.GetMouseButtonDown(1))
        {
            // Fire pulling hook.
            m_pullHook.SetActive(true);

            m_pullHook.transform.position = m_pullNode.position;
            m_pullHook.transform.rotation = m_pullNode.parent.rotation;

            m_pullHookActive = true;
        }

        m_grappleLine.enabled = m_grappleHookActive;
        m_pullLine.enabled = m_pullHookActive;

        if (m_grappleHookActive)
        {
            if (m_graphookScript.IsLodged() && m_graphookScript.PullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS) // Hook is stuck in an object.
            {
                m_grapplePoint = m_grappleHook.transform.position;

                // Calculate rope length initially.
                if (!m_grapRopeLenCalculated)
                {
                    m_grapRopeLength = (m_grappleHook.transform.position - transform.position).sqrMagnitude;
                    m_grapRopeLenCalculated = true;
                }

                m_controller.OverrideMovement(GrappleFly);

                if (Input.GetMouseButtonDown(0))
                {
                    m_controller.FreeOverride();

                    m_grappleHookActive = false;
                    m_graphookScript.UnLodge();
                }
            }
            else if(m_graphookScript.IsLodged() && m_graphookScript.PullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER)
            {
                // Cancel if it hits a pull object.
                m_grappleHookActive = false;
                m_graphookScript.UnLodge();
            }
            else // Hook is still flying.
            {
                m_grapRopeLength = (m_grappleHook.transform.position - transform.position).sqrMagnitude;
                m_grapRopeLenCalculated = false;

                // Cancel since the rope is now too long.
                if (m_grapRopeLength >= m_breakDistance * m_breakDistance)
                {
                    m_controller.FreeOverride();
                    m_grappleHook.SetActive(false);
                    m_grappleHookActive = false;
                }
            }
        }
        
        if(m_pullHookActive)
        {
            if (m_pullHookScript.IsLodged() && m_pullHookScript.PullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER)
            {
                // Calculate rope length initially.
                if (!m_pullRopeLenCalculated)
                {
                    m_pullRopeLength = (m_pullHook.transform.position - transform.position).sqrMagnitude;
                    m_pullRopeLenCalculated = true;
                }

                PullObject();
            }
            else if (m_pullHookScript.IsLodged() && m_pullHookScript.PullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS)
            {
                // Cancel if it is a static (un-pullable) object.
                m_pullHook.SetActive(false);

                m_pullHookScript.UnLodge();
                m_pullHookActive = false;
            }
            else
            {
                m_pullRopeLenCalculated = false;
                m_pullRopeLength = (m_pullHook.transform.position - transform.position).sqrMagnitude;

                // Cancel since the rope is now too long.
                if(m_pullRopeLength > m_breakDistance * m_breakDistance)
                {
                    m_pullHook.SetActive(false);

                    m_pullHookScript.UnLodge();
                    m_pullHookActive = false;
                }
            }
                
        }
    }

    private void LateUpdate()
    {
        Vector3[] grapLineArray = new Vector3[2];
        grapLineArray[0] = m_grappleNode.position;
        grapLineArray[1] = m_grappleHook.transform.position;

        Vector3[] pullLineArray = new Vector3[2];
        pullLineArray[0] = m_pullNode.position;
        pullLineArray[1] = m_pullHook.transform.position;

        m_grappleLine.SetPositions(grapLineArray);
        m_pullLine.SetPositions(pullLineArray);
    }

    Vector3 GrappleFly(PlayerController controller)
    {
        Vector3 netForce = Vector3.zero;

        Vector3 grappleDif = m_grapplePoint - transform.position;
        Vector3 grappleDir = grappleDif.normalized;
        
        float pullComponent = Vector3.Dot(controller.GetVelocity(), grappleDir);
        
        Vector3 nonPullComponent = controller.GetVelocity() - (grappleDir * pullComponent);

        if (pullComponent < m_maxFlySpeed)
            netForce += grappleDir * m_flyAcceleration * Time.deltaTime;

        // Controls
        netForce += m_controller.WASDAcceleration(m_controller.m_airAcceleration);

        // Lateral drag.
        if (nonPullComponent.sqrMagnitude < 1.0f)
            netForce -= nonPullComponent * m_driftTolerance * Time.deltaTime;
        else
            netForce -= nonPullComponent.normalized * m_driftTolerance * Time.deltaTime;

        // Stop when within radius and on ground.
        if (grappleDif.sqrMagnitude <= m_destinationRadius * m_destinationRadius)
        {
            controller.SetVelocity(Vector3.zero);

            // Disable hook visuals.
            m_grappleHook.SetActive(false);
            m_grappleHookActive = false;

            m_controller.OverrideMovement(GrappleLand);

            if(m_grapplePoint.y > transform.position.y)
                netForce += Vector3.up * m_pushUpForce;

            netForce += m_controller.LookForward() * m_pushUpForce;
        }

        float tension = Vector3.Dot(m_controller.GetVelocity() + netForce, grappleDir);

        // Prevent the rope from stretching beyond the rope length when initially grappling.
        if (m_restrictToRopeLength && grappleDif.sqrMagnitude > m_grapRopeLength && tension < 0.0f)
            netForce -= tension * grappleDir;

        Vector3 newVelocity = controller.GetVelocity() + netForce;
        return newVelocity;
    }

    Vector3 GrappleLand(PlayerController controller)
    {
        Vector3 netForce = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            netForce += controller.LookForward() * m_landMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            netForce += controller.LookLeft() * m_landMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            netForce += controller.LookBack() * m_landMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            netForce += controller.LookRight() * m_landMoveAcceleration * Time.deltaTime;

        netForce += Physics.gravity * Time.deltaTime;

        Vector3 velocity = controller.GetVelocity();

        if(velocity.sqrMagnitude < 1.0f)
        {
            velocity.x -= velocity.x * 5.0f * Time.deltaTime;
            velocity.z -= velocity.x * 5.0f * Time.deltaTime;
        }
        else 
        {
            Vector3 velNor = velocity.normalized;
            velocity.x -= velNor.x * 5.0f * Time.deltaTime;
            velocity.z -= velNor.z * 5.0f * Time.deltaTime;
        }

        if (controller.IsGrounded())
        {
            controller.FreeOverride();
        }

        return velocity + netForce;
    }

    void PullObject()
    {
        PullObject pullObj = m_pullHookScript.HookedObject();

        Vector3 objDiff = transform.position - m_pullHook.transform.position;
        Vector3 objDir = objDiff.normalized;
        float mag = objDiff.sqrMagnitude;

        float damage = 0.0f;

        if (mag > m_breakDistance * m_breakDistance)
        {
            damage += Mathf.Infinity;
        }
        else if (mag > m_pullRopeLength)
        {
            damage += mag - m_pullRopeLength;

            // Tension force.
            //float tensionVal = (damage / (m_breakDistance * m_breakDistance));
            float tensionVal = (mag / (m_breakDistance * m_breakDistance));
            Debug.Log(tensionVal);

            m_controller.SetVelocity(m_controller.GetVelocity() + (objDir * -tensionVal * 1.0f));
        }

        pullObj.Damage(objDir, damage * Time.deltaTime);

        // Release hook.
        if (!pullObj.IsCoupled())
        {
            m_pullHookActive = false;
            m_pullHookScript.UnLodge();
        }
    }
}
