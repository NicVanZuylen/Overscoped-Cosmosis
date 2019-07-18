using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]

public class GrappleHook : MonoBehaviour
{
    // Public:
    public GameObject m_grappleHook;
    public Color m_grappleModeColor;
    public LineRenderer m_grappleLine;
    public Transform m_grappleNode;

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
    private GradientColorKey[] m_colorKeys;
    private Bezier m_ropeCurve;
    private Vector3[] m_v3GrapLinePoints;
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

        m_colorKeys = new GradientColorKey[m_grappleLine.colorGradient.colorKeys.Length];
        m_ropeCurve = new Bezier();
        m_v3GrapLinePoints = new Vector3[m_grappleLine.positionCount];

        for (int i = 0; i < m_grappleLine.colorGradient.colorKeys.Length; ++i)
            m_colorKeys[i] = m_grappleLine.colorGradient.colorKeys[i];

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
            // Fire hook in grapple mode.
            m_grappleHook.SetActive(true);
            m_graphookScript.SetPullType(Hook.EHookPullMode.PULL_FLY_TOWARDS);

            m_grappleHook.transform.position = m_grappleNode.position;
            m_grappleHook.transform.rotation = m_grappleNode.parent.rotation;

            m_grappleLine.startColor = m_grappleModeColor;
            m_grappleLine.endColor = m_grappleModeColor;

            m_controller.FreeOverride();

            m_bGrappleHookActive = true;
        }
        else if (!m_bGrappleHookActive && Input.GetMouseButtonDown(1))
        {
            // Fire hook in pull mode.
            m_grappleHook.SetActive(true);
            m_graphookScript.SetPullType(Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER);

            m_grappleHook.transform.position = m_grappleNode.position;
            m_grappleHook.transform.rotation = m_grappleNode.parent.rotation;

            // Revert hook color back to original.
            m_grappleLine.colorGradient.SetKeys(m_colorKeys, m_grappleLine.colorGradient.alphaKeys);
            m_grappleLine.startColor = m_colorKeys[0].color;
            m_grappleLine.endColor = m_colorKeys[0].color;

            m_bGrappleHookActive = true;
        }

        /*
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
        */

        m_grappleLine.enabled = m_bGrappleHookActive;

        if (m_bGrappleHookActive)
        {
            if (m_graphookScript.IsLodged() && m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS) // Hook is stuck in an object.
            {
                m_v3GrapplePoint = m_grappleHook.transform.position;

                // Calculate rope length initially.
                if (!m_bGrapRopeLenCalculated)
                {
                    m_fGrapRopeLength = (m_grappleHook.transform.position - transform.position).sqrMagnitude;
                    m_bGrapRopeLenCalculated = true;
                }

                m_controller.OverrideMovement(GrappleFly);

                // Exit when releasing the left mouse button.
                if (Input.GetMouseButtonUp(0))
                {
                    m_controller.FreeOverride();

                    m_bGrappleHookActive = false;
                    m_graphookScript.UnLodge();
                }
            }
            else if(m_graphookScript.IsLodged() && m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER) // Hook is lodged in pull mode.
            {
                // Exit when releasing the right mouse button.
                if (Input.GetMouseButtonUp(1))
                {
                    m_controller.FreeOverride();

                    m_bGrappleHookActive = false;
                    m_graphookScript.UnLodge();
                }

                PullObject();
            }
            else // Hook is still flying.
            {
                m_fGrapRopeLength = (m_grappleHook.transform.position - transform.position).sqrMagnitude;
                m_bGrapRopeLenCalculated = false;

                bool bForceRelease = (m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS && Input.GetMouseButtonUp(0)) 
                    || (m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER && Input.GetMouseButtonUp(1));

                // Cancel if the rope becomes too long, or the player released the left mouse button.
                if (bForceRelease || m_fGrapRopeLength >= m_fGrapplebreakDistance * m_fGrapplebreakDistance)
                {
                    m_controller.FreeOverride();
                    m_grappleHook.SetActive(false);
                    m_bGrappleHookActive = false;
                }
            }
        }
        
        /*
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
        */
    }

    private void LateUpdate()
    {
        Vector3 v3DiffNoY = m_grappleHook.transform.position - m_grappleNode.position;
        v3DiffNoY.y = 0.0f;

        // Get amount the player is look horizontally away to the destination.
        // This will be applied to the curve.
        Vector3 v3HorizontalVec = m_controller.LookRight();
        float fHorizontalAmount = Vector3.Dot(v3HorizontalVec, v3DiffNoY);

        Vector3 v3CurveCorner = m_grappleNode.transform.position + (v3DiffNoY * 0.5f) - (fHorizontalAmount * v3HorizontalVec * 0.5f);

        Debug.DrawLine(m_grappleNode.position, v3CurveCorner, Color.green);
        Debug.DrawLine(v3CurveCorner, m_grappleHook.transform.position, Color.green);

        // Set curve points.
        m_ropeCurve.SetStart(m_grappleNode.position);
        m_ropeCurve.SetCorner(v3CurveCorner);
        m_ropeCurve.SetEnd(m_grappleHook.transform.position);

        int nPointCount = m_grappleLine.positionCount;
        for(int i = 1; i < nPointCount - 1; ++i)
        {
            m_v3GrapLinePoints[i] = m_ropeCurve.Evaluate((float)i / nPointCount);
        }

        m_v3GrapLinePoints[0] = m_grappleNode.position;
        m_v3GrapLinePoints[nPointCount - 1] = m_grappleHook.transform.position;

        //Vector3[] v3GrapLineArray = new Vector3[2];
        //v3GrapLineArray[0] = m_grappleNode.position;
        //v3GrapLineArray[1] = m_grappleHook.transform.position;

        /*
        Vector3[] v3PullLineArray = new Vector3[2];
        v3PullLineArray[0] = m_pullNode.position;
        v3PullLineArray[1] = m_pullHook.transform.position;
        */

        //m_grappleLine.SetPositions(v3GrapLineArray);
        m_grappleLine.SetPositions(m_v3GrapLinePoints);
        //m_pullLine.SetPositions(v3PullLineArray);
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
        v3NetForce += m_controller.MoveDirection() * m_controller.m_fAirAcceleration * Time.deltaTime;

        // Lateral drag.
        if (v3NonPullComponent.sqrMagnitude < 1.0f)
            v3NetForce -= v3NonPullComponent * m_fDriftTolerance * Time.deltaTime;
        else
            v3NetForce -= v3NonPullComponent.normalized * m_fDriftTolerance * Time.deltaTime;

        // Stop when within radius and on ground.
        if (v3GrappleDif.sqrMagnitude <= m_fDestinationRadius * m_fDestinationRadius)
        {
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

        // Calculate 2-dimensional movement vectors.
        Vector3 v3ForwardVec = controller.LookForward();
        Vector3 v3RightVec = Vector3.Cross(Vector3.up, v3ForwardVec);

        // Movement during grapple landing phase.
        if (Input.GetKey(KeyCode.W))
            v3NetForce += v3ForwardVec * m_fLandMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            v3NetForce -= v3RightVec * m_fLandMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            v3NetForce -= v3ForwardVec * m_fLandMoveAcceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            v3NetForce += v3RightVec * m_fLandMoveAcceleration * Time.deltaTime;

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
        PullObject pullObj = m_graphookScript.HookedObject();

        Vector3 v3ObjDiff = transform.position - m_grappleHook.transform.position;
        Vector3 v3ObjDir = v3ObjDiff.normalized;
        float fRopeDistSqr = v3ObjDiff.sqrMagnitude; // Current rope length squared.

        // Distance beyond initial rope distance on impact the rope must be pulled to to decouple the pull object. (Squared)
        float fBreakDistSqr = m_fPullBreakDistance * m_fPullBreakDistance;

        float fDistBeyondThreshold = fRopeDistSqr - m_fPullRopeLength; // Distance beyond inital rope length.
        float fTension = fDistBeyondThreshold / fBreakDistSqr; // Tension value used to sample the color gradient and detect when the pull object should decouple.

        Color ropeColor = m_grappleLine.colorGradient.Evaluate(Mathf.Clamp(fTension, 0.0f, 1.0f));

        // Set color to sampled color.
        m_grappleLine.startColor = ropeColor;
        m_grappleLine.endColor = ropeColor;

        if (fTension >= 1.0f)
        {
            // Object is decoupled.s
            pullObj.Decouple(v3ObjDir);
        
            // Free the pull hook.
            m_bGrappleHookActive = false;
            m_graphookScript.UnLodge();
        }
    }
}
