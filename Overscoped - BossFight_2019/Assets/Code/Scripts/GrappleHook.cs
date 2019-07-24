using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]

public class GrappleHook : MonoBehaviour
{
    // Public:
    public GameObject m_grappleHook;
    public LineRenderer m_grappleLine;
    public Transform m_grappleNode;

    [Header("Physics")]
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

    [Header("Pull Mode")]

    [Tooltip("The distance the pull hook must be extended beyond the initial rope length on impact to decouple the target object.")]
    public float m_fPullBreakDistance = 8.0f;

    [Header("Misc.")]
    [Tooltip("Whether or not the player will be restricted a spherical volume with the rope length as the radius when grappling.")]
    public bool m_bRestrictToRopeLength = true;

    [Tooltip("Spherecast radius for firing the grapple hook.")]
    public float m_fHookRadius = 0.5f;

    [Header("Effects")]
    [Tooltip("Color of the grapple line when in grapple mode.")]
    public Color m_grappleModeColor;

    [Tooltip("Delay between getting a new set of random points for the rope shake effect.")]
    public float m_fRopeShakeDelay = 0.1f;

    [Tooltip("Speed of rope point movement between random points.")]
    public float m_fShakeSpeed = 0.1f;

    [Tooltip("Maximum offset of rope points from thier position on the curve.")]
    public float m_fShakeMagnitude = 0.05f;

    [Tooltip("Amount of waves in the rope whilst it flies.")]
    public int m_nWaveCount = 3;

    [Tooltip("Wave amplitude multiplier of the wobble effect.")]
    public float m_fWobbleWaveAmp = 0.15f;

    public GameObject m_handEffect;
    public GameObject m_impactEffect;

    // Private:

    private PlayerController m_controller;
    private PlayerStats m_stats;
    private Hook m_graphookScript;
    private bool m_bGrappleHookActive;

    // Rope & Grapple function
    private GradientColorKey[] m_colorKeys;
    private RaycastHit m_fireHit;
    private Bezier m_ropeCurve;
    private Vector3[] m_v3GrapLinePoints;
    private Vector3[] m_v3GrapLinePointOffsets;
    private Vector3[] m_v3ShakeVectors;
    private Vector3[] m_v3WobbleVectors;
    private Vector3 m_v3GrapplePoint;
    private Vector3 m_v3GrappleNormal;
    private float m_fGrapRopeLength;
    private float m_fShakeTime;

    // Pull function
    private PullObject m_pullObj;
    private Vector3 m_v3PullTension;
    private float m_fPullRopeLength;

    // Misc.

    void Awake()
    {
        // Component retreival.
        m_controller = GetComponent<PlayerController>();
        m_stats = GetComponent<PlayerStats>();
        m_graphookScript = m_grappleHook.GetComponent<Hook>();

        // Rope color and curve.
        m_colorKeys = new GradientColorKey[m_grappleLine.colorGradient.colorKeys.Length];
        m_ropeCurve = new Bezier(2);

        for (int i = 0; i < m_grappleLine.colorGradient.colorKeys.Length; ++i)
            m_colorKeys[i] = m_grappleLine.colorGradient.colorKeys[i];

        // Rope points.
        m_v3GrapLinePoints = new Vector3[m_grappleLine.positionCount];
        m_v3GrapLinePointOffsets = new Vector3[m_grappleLine.positionCount];
        m_v3ShakeVectors = new Vector3[m_grappleLine.positionCount];
        m_v3WobbleVectors = new Vector3[m_grappleLine.positionCount];

        m_grappleHook.SetActive(false);

        // Misc.
        m_v3GrapplePoint = Vector3.zero;
        m_fShakeTime = 0.0f;
        m_bGrappleHookActive = false;
    }

    void Update()
    {
        Ray grapRay = new Ray(m_grappleNode.position, m_grappleNode.forward);

        bool bPlayerHasEnoughMana = m_stats.EnoughMana();

        if(bPlayerHasEnoughMana && !m_bGrappleHookActive && Input.GetMouseButtonDown(0) && Physics.SphereCast(grapRay, m_fHookRadius, out m_fireHit, m_fGrapplebreakDistance))
        {
            m_v3GrapplePoint = m_fireHit.point;
            m_v3GrappleNormal = m_fireHit.normal;
            m_fGrapRopeLength = m_fireHit.distance;

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
        else if (bPlayerHasEnoughMana && !m_bGrappleHookActive && Input.GetMouseButtonDown(1) && Physics.SphereCast(grapRay, m_fHookRadius, out m_fireHit, m_fGrapplebreakDistance))
        {
            if (m_fireHit.collider.tag == "PullObj")
                m_pullObj = m_fireHit.collider.gameObject.GetComponent<PullObject>();
            else
                m_pullObj = null;

            m_v3GrapplePoint = m_fireHit.point;
            m_v3GrappleNormal = m_fireHit.normal;
            m_fGrapRopeLength = m_fireHit.distance;

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

        // Effects
        m_grappleLine.enabled = m_bGrappleHookActive;
        m_handEffect.SetActive(m_bGrappleHookActive);
        m_impactEffect.SetActive(m_graphookScript.IsLodged() && m_bGrappleHookActive);

        if (m_bGrappleHookActive)
        {

            m_graphookScript.SetCurve(m_ropeCurve);
            m_graphookScript.SetTarget(m_v3GrapplePoint, m_fGrapRopeLength);

            if (m_graphookScript.IsLodged() && m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS) // Hook is stuck in an object.
            {
                m_v3GrapplePoint = m_grappleHook.transform.position;

                // Impact particle effect.
                m_impactEffect.transform.position = m_v3GrapplePoint;
                m_impactEffect.transform.rotation = Quaternion.LookRotation(m_v3GrappleNormal, Vector3.up);

                m_controller.OverrideMovement(GrappleFly);

                // Exit when releasing the left mouse button.
                if (Input.GetMouseButtonUp(0) || !bPlayerHasEnoughMana)
                {
                    m_controller.FreeOverride();

                    m_bGrappleHookActive = false;
                    m_graphookScript.UnLodge();
                }
            }
            else if(m_graphookScript.IsLodged() && m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER) // Hook is lodged in pull mode.
            {
                // Impact particle effect.
                m_impactEffect.transform.position = m_v3GrapplePoint;
                m_impactEffect.transform.rotation = Quaternion.LookRotation(m_v3GrappleNormal, Vector3.up);

                // Exit when releasing the right mouse button.
                if (Input.GetMouseButtonUp(1) || !bPlayerHasEnoughMana)
                {
                    m_controller.FreeOverride();

                    m_bGrappleHookActive = false;
                    m_graphookScript.UnLodge();
                }

                PullObject();
            }
            else // Hook is still flying.
            {
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
    }

    const float m_fOffsetLerpFly = 0.1f;
    const float m_fOffsetLerpLodged = 0.35f;
    const float m_fImpactShakeDuration = 0.4f;
    float m_fImpactShakeTime = 0.0f;

    private void LateUpdate()
    {
        if (!m_bGrappleHookActive)
            return;

        Vector3 v3DiffNoY = (m_graphookScript.Destination() + m_grappleNode.position) * 0.5f;
        v3DiffNoY -= m_grappleNode.position;
        v3DiffNoY.y = 0.0f;

        // Get amount the player is look horizontally away to the destination.
        // This will be applied to the curve.
        Vector3 v3HorizontalVec = m_controller.LookRight();
        float fHorizontalAmount = Vector3.Dot(v3HorizontalVec, v3DiffNoY);

        Vector3 v3CurveCorner = m_grappleNode.transform.position + (v3DiffNoY) - (fHorizontalAmount * v3HorizontalVec);

        // Curve points.
        m_ropeCurve.m_v3Start = m_grappleNode.position;
        m_ropeCurve.m_v3Corners[0] = v3CurveCorner;
        m_ropeCurve.m_v3Corners[1] = m_v3GrapplePoint + (m_v3GrappleNormal * (Mathf.Min(5.0f, v3DiffNoY.magnitude)));
        m_ropeCurve.m_v3End = m_v3GrapplePoint;

        // Draw curve line segments in scene view.
        Debug.DrawLine(m_ropeCurve.m_v3Start, m_ropeCurve.m_v3Corners[0], Color.green);
        Debug.DrawLine(m_ropeCurve.m_v3Corners[0], m_ropeCurve.m_v3Corners[1], Color.green);
        Debug.DrawLine(m_ropeCurve.m_v3Corners[1], m_ropeCurve.m_v3End, Color.green);

        m_fShakeTime -= Time.deltaTime;

        // Set each line point to the correct points on the curve.
        int nPointCount = m_grappleLine.positionCount;

        Vector3 v3WobbleAxis = Vector3.Cross(Vector3.up, v3DiffNoY).normalized;

        float fWobbleMult = m_fWobbleWaveAmp;
        float fImpactShakeMult = 0.3f * Mathf.Clamp(m_fImpactShakeTime / m_fImpactShakeDuration, 0.0f, 1.0f);
        bool bHookLodged = m_graphookScript.IsLodged();

        // When hooked-in tension should be high so remove the wobble effect.
        if (bHookLodged)
        {
            fWobbleMult = 0.0f;

            m_fImpactShakeTime -= Time.deltaTime;
        }
        else
            m_fImpactShakeTime = m_fImpactShakeDuration;

        Vector3 v3WobbleShake = Vector3.zero;

        for (int i = 1; i < nPointCount; ++i)
        {
            float fPointProgress = (float)i / nPointCount;
            float fCurvePoint = Mathf.Sin(fPointProgress * m_nWaveCount * Mathf.PI);

            if (m_fShakeTime <= 0.0f)
            {
                // Re-randomize shake vector.
                m_v3ShakeVectors[i].x = Random.Range(-1.0f, 1.0f);
                m_v3ShakeVectors[i].y = Random.Range(-1.0f, 1.0f);
                m_v3ShakeVectors[i].z = Random.Range(-1.0f, 1.0f);
                m_v3ShakeVectors[i] *= m_fShakeMagnitude;

                if(bHookLodged)
                    v3WobbleShake = v3WobbleAxis * fCurvePoint * fImpactShakeMult;
            }

            // Wave offset that produces a low-tension effect.
            Vector3 v3WobbleOffset = v3WobbleAxis * fCurvePoint * fPointProgress * fWobbleMult;

            // Different lerp values are used for both rope states, when lodged the value is more snappy.
            if(!m_graphookScript.IsLodged())
                m_v3WobbleVectors[i] = Vector3.Lerp(m_v3WobbleVectors[i], v3WobbleOffset, m_fOffsetLerpFly);
            else
                m_v3WobbleVectors[i] = Vector3.Lerp(m_v3WobbleVectors[i], v3WobbleOffset + v3WobbleShake, m_fOffsetLerpLodged);

            m_v3GrapLinePointOffsets[i] = Vector3.Lerp(m_v3GrapLinePointOffsets[i], m_v3ShakeVectors[i], m_fShakeSpeed);
            m_v3GrapLinePoints[i] = m_ropeCurve.Evaluate(((float)i / (float)nPointCount) * m_graphookScript.FlyProgress()) + m_v3GrapLinePointOffsets[i] + m_v3WobbleVectors[i];
        }

        if (m_fShakeTime <= 0.0f)
            m_fShakeTime = m_fRopeShakeDelay;

        // First point hardcoded to hook node.
        m_v3GrapLinePoints[0] = m_grappleNode.position;
        m_v3GrapLinePoints[nPointCount - 1] = m_grappleHook.transform.position;

        // Hand particle effect rotation.
        m_handEffect.transform.rotation = Quaternion.LookRotation(m_v3GrapLinePoints[1] - m_v3GrapLinePoints[0], Vector3.up);

        m_grappleLine.SetPositions(m_v3GrapLinePoints);
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
        Vector3 v3ObjDiff = transform.position - m_grappleHook.transform.position;
        Vector3 v3ObjDir = v3ObjDiff.normalized;
        float fRopeDistSqr = v3ObjDiff.sqrMagnitude; // Current rope length squared.

        // Distance beyond initial rope distance on impact the rope must be pulled to to decouple the pull object. (Squared)
        float fBreakDistSqr = m_fPullBreakDistance * m_fPullBreakDistance;

        float fDistBeyondThreshold = fRopeDistSqr - m_fGrapRopeLength; // Distance beyond inital rope length.
        float fTension = fDistBeyondThreshold / fBreakDistSqr; // Tension value used to sample the color gradient and detect when the pull object should decouple.

        Color ropeColor = m_grappleLine.colorGradient.Evaluate(Mathf.Clamp(fTension, 0.0f, 1.0f));

        // Set color to sampled color.
        m_grappleLine.startColor = ropeColor;
        m_grappleLine.endColor = ropeColor;

        if (fTension >= 1.0f)
        {
            // Object is decoupled.
            if(m_pullObj != null)
                m_pullObj.Decouple(v3ObjDir);
        
            // Free the pull hook.
            m_bGrappleHookActive = false;
            m_graphookScript.UnLodge();
        }
    }

    public bool IsActive()
    {
        return m_bGrappleHookActive;
    }
}
