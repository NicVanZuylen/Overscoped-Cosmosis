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
    public float m_fFlyAcceleration = 30.0f;

    [Tooltip("Maximum speed in the direction of the grapple line the player can travel.")]
    public float m_fMaxFlySpeed = 40.0f;

    [Tooltip("Radius from the hook in which the player will detach.")]
    public float m_fDestinationRadius = 2.0f;

    [Tooltip("Allowance of lateral movement when grappling.")]
    public float m_fDriftTolerance = 8.0f;

    [Tooltip("Force exerted upwards upon the player when they reach the grapple destination.")]
    public float m_fPushUpForce = 4.0f;

    [Tooltip("Player move acceleration when landing after successfully reaching the grapple hook.")]
    public float m_fLandMoveAcceleration = 5.0f;

    [Tooltip("The distance the grapple hook will travel before cancelling.")]
    public float m_fGrapplebreakDistance = 20.0f;

    [Tooltip("The distance the pull hook must be extended beyond the initial rope length on impact to decouple the target object.")]
    public float m_fPullBreakDistance = 8.0f;

    [Tooltip("Whether or not the player will be restricted a spherical volume with the rope length as the radius when grappling.")]
    public bool m_bRestrictToRopeLength = true;

    // Private:

    private PlayerController m_controller;
    private Hook m_graphookScript;
    private Hook m_pullHookScript;
    private GradientColorKey[] m_colorKeys;
    private Vector3 m_v3GrapplePoint;
    private Vector3 m_v3PullTension;
    private float m_fGrapRopeLength;
    private float m_fPullRopeLength;
    private bool m_bGrappleHookActive;
    private bool m_bPullHookActive;
    private bool m_bGrapRopeLenCalculated;
    private bool m_bPullRopeLenCalculated;

    void Awake()
    {
        m_controller = GetComponent<PlayerController>();

        m_graphookScript = m_grappleHook.GetComponent<Hook>();
        m_pullHookScript = m_pullHook.GetComponent<Hook>();

        m_colorKeys = new GradientColorKey[m_pullLine.colorGradient.colorKeys.Length];

        for (int i = 0; i < m_pullLine.colorGradient.colorKeys.Length; ++i)
            m_colorKeys[i] = m_pullLine.colorGradient.colorKeys[i];

        m_grappleHook.SetActive(false);

        m_v3GrapplePoint = Vector3.zero;
        m_bGrappleHookActive = false;
        m_bPullHookActive = false;
        m_bGrapRopeLenCalculated = false;
        m_bPullRopeLenCalculated = false;
    }

    void Update()
    {
        if(!m_bGrappleHookActive && Input.GetMouseButtonDown(0))
        {
            // Fire grapple hook.
            m_grappleHook.SetActive(true);

            m_grappleHook.transform.position = m_grappleNode.position;
            m_grappleHook.transform.rotation = m_grappleNode.parent.rotation;

            m_controller.FreeOverride();

            m_bGrappleHookActive = true;
        }

        if (!m_bPullHookActive && Input.GetMouseButtonDown(1))
        {
            // Fire pulling hook.
            m_pullHook.SetActive(true);

            m_pullHook.transform.position = m_pullNode.position;
            m_pullHook.transform.rotation = m_pullNode.parent.rotation;

            // Revert hook color back to original.
            m_pullLine.colorGradient.SetKeys(m_colorKeys, m_pullLine.colorGradient.alphaKeys);
            m_pullLine.startColor = m_colorKeys[0].color;
            m_pullLine.endColor = m_colorKeys[0].color;

            m_bPullHookActive = true;
        }

        m_grappleLine.enabled = m_bGrappleHookActive;
        m_pullLine.enabled = m_bPullHookActive;

        if (m_bGrappleHookActive)
        {
            if (m_graphookScript.IsLodged() && m_graphookScript.PullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS) // Hook is stuck in an object.
            {
                m_v3GrapplePoint = m_grappleHook.transform.position;

                // Calculate rope length initially.
                if (!m_bGrapRopeLenCalculated)
                {
                    m_fGrapRopeLength = (m_grappleHook.transform.position - transform.position).sqrMagnitude;
                    m_bGrapRopeLenCalculated = true;
                }

                m_controller.OverrideMovement(GrappleFly);

                if (Input.GetMouseButtonDown(0))
                {
                    m_controller.FreeOverride();

                    m_bGrappleHookActive = false;
                    m_graphookScript.UnLodge();
                }
            }
            else if(m_graphookScript.IsLodged() && m_graphookScript.PullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER)
            {
                // Cancel if it hits a pull object.
                m_bGrappleHookActive = false;
                m_graphookScript.UnLodge();
            }
            else // Hook is still flying.
            {
                m_fGrapRopeLength = (m_grappleHook.transform.position - transform.position).sqrMagnitude;
                m_bGrapRopeLenCalculated = false;

                // Cancel since the rope is now too long.
                if (m_fGrapRopeLength >= m_fGrapplebreakDistance * m_fGrapplebreakDistance)
                {
                    m_controller.FreeOverride();
                    m_grappleHook.SetActive(false);
                    m_bGrappleHookActive = false;
                }
            }
        }
        
        if(m_bPullHookActive)
        {
            if (m_pullHookScript.IsLodged() && m_pullHookScript.PullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER)
            {
                // Calculate rope length initially.
                if (!m_bPullRopeLenCalculated)
                {
                    m_fPullRopeLength = (m_pullHook.transform.position - transform.position).sqrMagnitude;
                    m_bPullRopeLenCalculated = true;
                }

                PullObject();
            }
            else if (m_pullHookScript.IsLodged() && m_pullHookScript.PullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS)
            {
                // Cancel if it is a static (un-pullable) object.
                m_pullHook.SetActive(false);

                m_pullHookScript.UnLodge();
                m_bPullHookActive = false;
            }
            else
            {
                m_bPullRopeLenCalculated = false;
                m_fPullRopeLength = (m_pullHook.transform.position - transform.position).sqrMagnitude;

                // Cancel since the rope is now too long.
                if(m_fPullRopeLength > m_fGrapplebreakDistance * m_fGrapplebreakDistance)
                {
                    m_pullHook.SetActive(false);

                    m_pullHookScript.UnLodge();
                    m_bPullHookActive = false;
                }
            }
                
        }
    }

    private void LateUpdate()
    {
        Vector3[] v3GrapLineArray = new Vector3[2];
        v3GrapLineArray[0] = m_grappleNode.position;
        v3GrapLineArray[1] = m_grappleHook.transform.position;

        Vector3[] v3PullLineArray = new Vector3[2];
        v3PullLineArray[0] = m_pullNode.position;
        v3PullLineArray[1] = m_pullHook.transform.position;

        m_grappleLine.SetPositions(v3GrapLineArray);
        m_pullLine.SetPositions(v3PullLineArray);
    }

    Vector3 GrappleFly(PlayerController controller)
    {
        Vector3 v3NetForce = Vector3.zero;

        Vector3 v3GrappleDif = m_v3GrapplePoint - transform.position;
        Vector3 v3GrappleDir = v3GrappleDif.normalized;
        
        float fPullComponent = Vector3.Dot(controller.GetVelocity(), v3GrappleDir);
        
        Vector3 v3NonPullComponent = controller.GetVelocity() - (v3GrappleDir * fPullComponent);

        if (fPullComponent < m_fMaxFlySpeed)
            v3NetForce += v3GrappleDir * m_fFlyAcceleration * Time.deltaTime;

        // Controls
        v3NetForce += m_controller.WASDAcceleration(m_controller.m_fAirAcceleration);

        // Lateral drag.
        if (v3NonPullComponent.sqrMagnitude < 1.0f)
            v3NetForce -= v3NonPullComponent * m_fDriftTolerance * Time.deltaTime;
        else
            v3NetForce -= v3NonPullComponent.normalized * m_fDriftTolerance * Time.deltaTime;

        // Stop when within radius and on ground.
        if (v3GrappleDif.sqrMagnitude <= m_fDestinationRadius * m_fDestinationRadius)
        {
            controller.SetVelocity(Vector3.zero);

            // Disable hook visuals.
            m_grappleHook.SetActive(false);
            m_bGrappleHookActive = false;

            m_controller.OverrideMovement(GrappleLand);

            if(m_v3GrapplePoint.y > transform.position.y)
                v3NetForce += Vector3.up * m_fPushUpForce;

            v3NetForce += m_controller.LookForward() * m_fPushUpForce;
        }

        float tension = Vector3.Dot(m_controller.GetVelocity() + v3NetForce, v3GrappleDir);

        // Prevent the rope from stretching beyond the rope length when initially grappling.
        if (m_bRestrictToRopeLength && v3GrappleDif.sqrMagnitude > m_fGrapRopeLength && tension < 0.0f)
            v3NetForce -= tension * v3GrappleDir;

        return m_controller.GetVelocity() + v3NetForce;
    }

    Vector3 GrappleLand(PlayerController controller)
    {
        Vector3 v3NetForce = Vector3.zero;

        // Movement during grapple landing phase.
        if (Input.GetKey(KeyCode.W))
            v3NetForce += controller.LookForward() * m_fLandMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            v3NetForce += controller.LookLeft() * m_fLandMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            v3NetForce += controller.LookBack() * m_fLandMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            v3NetForce += controller.LookRight() * m_fLandMoveAcceleration * Time.deltaTime;

        // Add gravity.
        v3NetForce += Physics.gravity * Time.deltaTime;

        Vector3 v3Velocity = controller.GetVelocity();

        // Drag
        if(v3Velocity.sqrMagnitude < 1.0f)
        {
            v3Velocity.x -= v3Velocity.x * 5.0f * Time.deltaTime;
            v3Velocity.z -= v3Velocity.x * 5.0f * Time.deltaTime;
        }
        else 
        {
            Vector3 v3VelNor = v3Velocity.normalized;
            v3Velocity.x -= v3VelNor.x * 5.0f * Time.deltaTime;
            v3Velocity.z -= v3VelNor.z * 5.0f * Time.deltaTime;
        }

        // Free override on landing.
        if (controller.IsGrounded())
        {
            controller.FreeOverride();
        }

        return v3Velocity + v3NetForce;
    }

    void PullObject()
    {
        PullObject pullObj = m_pullHookScript.HookedObject();

        Vector3 v3ObjDiff = transform.position - m_pullHook.transform.position;
        Vector3 v3ObjDir = v3ObjDiff.normalized;
        float fRopeDistSqr = v3ObjDiff.sqrMagnitude; // Current rope length squared.

        // Distance beyond initial rope distance on impact the rope must be pulled to to decouple the pull object. (Squared)
        float fBreakDistSqr = m_fPullBreakDistance * m_fPullBreakDistance;

        float fDistBeyondThreshold = fRopeDistSqr - m_fPullRopeLength; // Distance beyond inital rope length.
        float fTension = fDistBeyondThreshold / fBreakDistSqr; // Tension value used to sample the color gradient and detect when the pull object should decouple.

        Color ropeColor = m_pullLine.colorGradient.Evaluate(Mathf.Clamp(fTension, 0.0f, 1.0f));

        // Set color to sampled color.
        m_pullLine.startColor = ropeColor;
        m_pullLine.endColor = ropeColor;

        if (fTension >= 1.0f)
        {
            // Object is decoupled.s
            pullObj.Decouple(v3ObjDir);
        
            // Free the pull hook.
            m_bPullHookActive = false;
            m_pullHookScript.UnLodge();
        }
    }
}
