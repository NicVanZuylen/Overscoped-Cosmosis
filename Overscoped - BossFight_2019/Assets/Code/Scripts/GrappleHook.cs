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

    [Tooltip("The minimum time grappling before the release boost can be applied.")]
    public float m_fMinReleaseBoostTime = 0.3f;

    [Tooltip("The magnitude of the forward force applied to the player upon rope release.")]
    public float m_fReleaseForce = 20.0f;

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
    public float m_fRippleWaveAmp = 0.15f;

    [Tooltip("Thickness the line will expand to when popping.")]
    public float m_fPopThickness = 2.0f;

    [Tooltip("Rate in which the line will expand and pop after use.")]
    public float m_fPopRate = 3.0f;

    [Tooltip("Amount of time shake is amplified following rope impact.")]
    public float m_fImpactShakeDuration = 0.6f;

    [Tooltip("Multiplier for the impact shake effect.")]
    public float m_fImpactShakeMult = 5.0f;

    public GameObject m_handEffect;
    public GameObject m_impactEffect;
    public Material m_grappleLineMat;
    public Material m_pullLineMat;

    public ComputeShader m_lineCompute;

    // Private:

    private ComputeBuffer m_outputPointBuffer;
    private ComputeBuffer m_bezierPointBuffer; // Contains points for the main and wobble bezier curves.
    private ComputeBuffer m_bezierIntPointBuffer;
    private int m_nPointKernelIndex;
    private const int m_nWobbleBezierCount = 16;

    private PlayerController m_controller;
    private PlayerStats m_stats;
    private Animator m_animController;
    private CameraEffects m_cameraEffects;
    private Transform m_cameraTransform;
    private Hook m_graphookScript;
    private bool m_bGrappleHookActive;
    private bool m_bJustImpacted;

    // Rope & Grapple function
    private GradientColorKey[] m_colorKeys;
    private RaycastHit m_fireHit;
    private Bezier m_ropeCurve;
    private Transform m_hitTransform;
    private Vector3[] m_v3GrapLinePoints;
    private Vector3[] m_v3ShakeVectors;
    private Vector3 m_v3GrapplePoint;
    private Vector3 m_v3GrappleNormal;
    private float m_fGrapRopeLength;
    private float m_fShakeTime;
    private float m_fLineThickness;
    private float fRippleMult;
    private float m_fImpactShakeTime;
    private float m_fCurrentLineThickness;
    private float m_fGrappleTime;

    // Pull function
    private PullObject m_pullObj;
    private Vector3 m_v3PullTension;
    private float m_fPullRopeLength;

    // Misc.

    void Awake()
    {
        const int nPointCount = 4;

        m_outputPointBuffer = new ComputeBuffer(m_grappleLine.positionCount * 2, sizeof(float) * 3); // Multiplied by two to include wobble effect vectors.
        m_bezierPointBuffer = new ComputeBuffer(nPointCount + m_nWobbleBezierCount, sizeof(float) * 3); // Contains points for the main and wobble bezier curves.
        m_bezierIntPointBuffer = new ComputeBuffer((nPointCount + m_nWobbleBezierCount) * m_grappleLine.positionCount, sizeof(float) * 3);

        m_nPointKernelIndex = m_lineCompute.FindKernel("CSMain");
        m_lineCompute.SetBuffer(m_nPointKernelIndex, "outPoints", m_outputPointBuffer);
        m_lineCompute.SetBuffer(m_nPointKernelIndex, "bezierPoints", m_bezierPointBuffer);
        m_lineCompute.SetBuffer(m_nPointKernelIndex, "bezierIntPoints", m_bezierIntPointBuffer);

        m_lineCompute.SetInt("inPointCount", nPointCount);
        m_lineCompute.SetInt("inWobblePointCount", m_nWobbleBezierCount);

        // Component retreival.
        m_controller = GetComponent<PlayerController>();
        m_stats = GetComponent<PlayerStats>();
        m_animController = GetComponentInChildren<Animator>();

        Camera cam = GetComponentInChildren<Camera>();

        m_cameraEffects = cam.GetComponent<CameraEffects>();
        m_cameraTransform = cam.transform;
        m_graphookScript = m_grappleHook.GetComponent<Hook>();

        // Line color and curve.
        m_colorKeys = new GradientColorKey[m_grappleLine.colorGradient.colorKeys.Length];
        m_ropeCurve = new Bezier(nPointCount);

        for (int i = 0; i < m_grappleLine.colorGradient.colorKeys.Length; ++i)
            m_colorKeys[i] = m_grappleLine.colorGradient.colorKeys[i];

        // Rope points.
        m_v3GrapLinePoints = new Vector3[m_grappleLine.positionCount];
        m_v3ShakeVectors = new Vector3[m_nWobbleBezierCount];

        m_v3ShakeVectors[0] = Vector3.zero;
        m_v3ShakeVectors[m_v3ShakeVectors.Length - 1] = Vector3.zero;

        // Line material
        m_grappleLine.materials[0] = m_grappleLineMat;

        m_grappleHook.SetActive(false);

        // Misc.
        m_v3GrapplePoint = Vector3.zero;
        m_fLineThickness = m_grappleLine.startWidth;
        m_fShakeTime = 0.0f;
        m_fGrappleTime = 0.0f;
        m_bGrappleHookActive = false;
    }

    private void OnDestroy()
    {
        m_bezierIntPointBuffer.Release();
        m_bezierPointBuffer.Release();
        m_outputPointBuffer.Release();
    }

    void Update()
    {
        // ------------------------------------------------------------------------------------------------------------------------------
        // Shooting

        Ray grapRay = new Ray(m_cameraTransform.position, m_cameraTransform.forward);

        bool bPlayerHasEnoughMana = m_stats.EnoughMana();

        if(bPlayerHasEnoughMana && !m_bGrappleHookActive && Input.GetMouseButtonDown(0) && Physics.SphereCast(grapRay, m_fHookRadius, out m_fireHit, m_fGrapplebreakDistance))
        {
            m_hitTransform = m_fireHit.transform;

            m_v3GrapplePoint = m_fireHit.point;
            m_v3GrappleNormal = m_fireHit.normal;
            m_fGrapRopeLength = m_fireHit.distance;

            // Fire hook in grapple mode.
            m_grappleHook.SetActive(true);
            m_graphookScript.SetPullType(Hook.EHookPullMode.PULL_FLY_TOWARDS);

            // Fly time.
            m_fGrappleTime = 0.0f;

            // Material
            m_grappleLine.material = m_grappleLineMat;

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

            m_hitTransform = m_fireHit.transform;

            m_v3GrapplePoint = m_fireHit.point;
            m_v3GrappleNormal = m_fireHit.normal;
            m_fGrapRopeLength = m_fireHit.distance;

            // Fire hook in pull mode.
            m_grappleHook.SetActive(true);
            m_graphookScript.SetPullType(Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER);

            // Material
            m_grappleLine.material = m_pullLineMat;

            m_grappleHook.transform.position = m_grappleNode.position;
            m_grappleHook.transform.rotation = m_grappleNode.parent.rotation;

            // Revert hook color back to original.
            m_grappleLine.colorGradient.SetKeys(m_colorKeys, m_grappleLine.colorGradient.alphaKeys);
            m_grappleLine.startColor = m_colorKeys[0].color;
            m_grappleLine.endColor = m_colorKeys[0].color;

            m_bGrappleHookActive = true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // Effects & Animations

        // Line will disable when the pop effect finishes.
        m_grappleLine.enabled = m_bGrappleHookActive || m_fCurrentLineThickness < (m_fPopThickness - 0.1f);
        m_handEffect.SetActive(m_bGrappleHookActive);

        bool bImpacted = m_graphookScript.IsLodged() && m_bGrappleHookActive;

        // Animations
        m_animController.SetBool("isCasting", m_bGrappleHookActive);

        m_impactEffect.SetActive(bImpacted);
        m_animController.SetBool("isGrappled", bImpacted);

        // ------------------------------------------------------------------------------------------------------------------------------
        // Active behaviour

        if (m_bGrappleHookActive)
        {
            m_graphookScript.SetCurve(m_ropeCurve);
            m_graphookScript.SetTarget(m_v3GrapplePoint, m_fGrapRopeLength);

            if (m_graphookScript.IsLodged() && m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_FLY_TOWARDS) // Hook is stuck in an object.
            {
                if (m_bJustImpacted)
                {
                    m_cameraEffects.ApplyShakeOverTime(0.05f, 1.0f, true);
                    m_grappleHook.transform.parent = m_hitTransform;

                    m_bJustImpacted = false;
                }
                else
                    m_cameraEffects.ApplyShakeOverTime(0.1f, 0.1f);

                // Increment grapple time.
                m_fGrappleTime += Time.deltaTime;

                // Add small FOV offset.
                m_cameraEffects.SetFOVOffset(5.0f);

                m_v3GrapplePoint = m_grappleHook.transform.position;

                // Impact particle effect.
                m_impactEffect.transform.position = m_v3GrapplePoint;
                m_impactEffect.transform.rotation = Quaternion.LookRotation(m_v3GrappleNormal, Vector3.up);

                m_controller.OverrideMovement(GrappleFly);

                // Exit when releasing the left mouse button.
                if (Input.GetMouseButtonUp(0) || m_stats.GetMana() <= 0.0f)
                {
                    m_controller.FreeOverride();

                    m_bGrappleHookActive = false;
                    m_graphookScript.UnLodge();

                    // Release impulse.
                    if(m_fGrappleTime >= m_fMinReleaseBoostTime)
                        m_controller.AddImpulse(m_controller.SurfaceForward() * m_fReleaseForce);
                }
            }
            else if(m_graphookScript.IsLodged() && m_graphookScript.GetPullType() == Hook.EHookPullMode.PULL_PULL_TOWARDS_PLAYER) // Hook is lodged in pull mode.
            {
                if(m_bJustImpacted)
                {
                    m_grappleHook.transform.parent = m_hitTransform;
                    m_cameraEffects.ApplyShakeOverTime(0.05f, 1.0f, true);

                    m_bJustImpacted = false;
                }

                m_v3GrapplePoint = m_grappleHook.transform.position;

                // Impact particle effect.
                m_impactEffect.transform.position = m_v3GrapplePoint;
                m_impactEffect.transform.rotation = Quaternion.LookRotation(m_v3GrappleNormal, Vector3.up);

                // Exit when releasing the right mouse button.
                if (Input.GetMouseButtonUp(1) || m_stats.GetMana() <= 0.0f)
                {
                    m_controller.FreeOverride();

                    m_bGrappleHookActive = false;
                    m_graphookScript.UnLodge();
                }

                PullObject();
            }
            else // Hook is still flying.
            {
                m_bJustImpacted = true;

                m_grappleHook.transform.parent = null;

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

        // ------------------------------------------------------------------------------------------------------------------------------
    }

    private void LateUpdate()
    {
        // ------------------------------------------------------------------------------------------------------------------------------
        // Effects

        if (!m_bGrappleHookActive)
        {
            // Expand thickness after use.
            m_fCurrentLineThickness = Mathf.Lerp(m_fCurrentLineThickness, m_fPopThickness, m_fPopRate);

            m_fCurrentLineThickness = Mathf.Clamp(m_fCurrentLineThickness, 0.0f, m_fPopThickness);

            // Set line shader opacity.
            m_grappleLine.material.SetFloat("_Opacity", 1.0f - (m_fCurrentLineThickness / m_fPopThickness));

            // Grapple is not active, play the poof effect.
            m_grappleLine.startWidth = m_fCurrentLineThickness;
            m_grappleLine.endWidth = m_fCurrentLineThickness;

            return;
        }

        // Reset thickness to default when in use.
        m_fCurrentLineThickness = m_fLineThickness;
        m_grappleLine.startWidth = m_fCurrentLineThickness;
        m_grappleLine.endWidth = m_fCurrentLineThickness;

        // Reset line shader opacity.
        m_grappleLine.material.SetFloat("_Opacity", 1.0f);

        Vector3 v3DiffNoY = (m_graphookScript.Destination() + m_grappleNode.position) * 0.5f;
        v3DiffNoY -= m_grappleNode.position;
        v3DiffNoY.y = 0.0f;

        // Get amount the player is look horizontally away to the destination.
        // This will be applied to the curve.
        Vector3 v3HorizontalVec = m_controller.LookRight();
        float fHorizontalAmount = Vector3.Dot(v3HorizontalVec, v3DiffNoY);

        Vector3 v3CurveCorner = m_grappleNode.transform.position + (v3DiffNoY) - (fHorizontalAmount * v3HorizontalVec);

        // Curve points.
        m_ropeCurve.m_v3Points[0] = m_grappleNode.position;
        m_ropeCurve.m_v3Points[1] = v3CurveCorner;
        //m_ropeCurve.m_v3Points[2] = m_v3GrapplePoint + (m_v3GrappleNormal * (Mathf.Min(5.0f, v3DiffNoY.magnitude)));
        m_ropeCurve.m_v3Points[2] = m_v3GrapplePoint;
        m_ropeCurve.m_v3Points[m_ropeCurve.m_v3Points.Length - 1] = m_v3GrapplePoint;

        //float fWobbleMult = m_fWobbleWaveAmp;
        bool bHookLodged = m_graphookScript.IsLodged();

        // Value specifiying the shake magnitude for this frame.
        float fShakeMag = m_fShakeMagnitude;

        // When hooked-in tension should be high so remove the wobble effect.
        if (bHookLodged)
        {
            if (m_fImpactShakeTime > 0.0f && fRippleMult <= 0.1f)
            {
                // Rope shake during impact shake time.
                float fImpactShakeMult = Mathf.Clamp(m_fImpactShakeTime / m_fImpactShakeDuration, 0.0f, 1.0f);
                fShakeMag = m_fImpactShakeMult * fImpactShakeMult;

                // Override shake time to be zero, causing a very fast shake.
                m_fShakeTime = 0.0f;

                m_fImpactShakeTime -= Time.deltaTime;
            }
            
            // Lerp ripple multiplier to zero.
            fRippleMult = Mathf.Lerp(fRippleMult, 0.0f, 0.4f);
        }
        else
        {
            fRippleMult = m_fRippleWaveAmp;

            m_fImpactShakeTime = m_fImpactShakeDuration;
        }

        Vector3 v3WobbleShake = Vector3.zero;

        // Calculate shake offsets.
        if (m_fShakeTime <= 0.0f)
        {
            for (int i = 1; i < m_nWobbleBezierCount - 1; ++i)
            {
                Vector3 v3RandomOffset;

                float fRemainingMag = 1.0f;

                // Calculate random unit vector.
                v3RandomOffset.x = Random.Range(-1.0f, 1.0f);
                fRemainingMag -= Mathf.Abs(v3RandomOffset.x);
                
                v3RandomOffset.y = Random.Range(-fRemainingMag, fRemainingMag);
                fRemainingMag -= Mathf.Abs(v3RandomOffset.y);
                
                v3RandomOffset.z = Random.Range(-fRemainingMag, fRemainingMag);

                // Multiply by magnitude.
                m_v3ShakeVectors[i] = v3RandomOffset * fShakeMag;
            }

            m_fShakeTime = m_fRopeShakeDelay;
        }
        else
            m_fShakeTime -= Time.deltaTime;

        // Set compute shader globals...
        m_lineCompute.SetFloat("inFlyProgress", m_graphookScript.FlyProgress());
        m_lineCompute.SetFloat("inRippleMagnitude", fRippleMult);

        // Set compute shader buffer data...
        m_bezierPointBuffer.SetData(m_ropeCurve.m_v3Points, 0, 0, m_ropeCurve.m_v3Points.Length);
        m_bezierPointBuffer.SetData(m_v3ShakeVectors, 0, m_ropeCurve.m_v3Points.Length, m_nWobbleBezierCount);

        m_lineCompute.Dispatch(m_nPointKernelIndex, Mathf.CeilToInt((float)m_grappleLine.positionCount / 256.0f), 1, 1);

        // Get compute shader output.
        m_outputPointBuffer.GetData(m_v3GrapLinePoints , 0, 0, m_grappleLine.positionCount);

        // Ensure points connect to start and end nodes.
        m_v3GrapLinePoints[0] = m_grappleNode.position;
        m_v3GrapLinePoints[m_grappleLine.positionCount - 1] = m_grappleHook.transform.position;

        // Hand particle effect rotation.
        m_handEffect.transform.rotation = Quaternion.LookRotation(m_v3GrapLinePoints[1] - m_v3GrapLinePoints[0], Vector3.up);

        // Apply points to line renderer.
        m_grappleLine.SetPositions(m_v3GrapLinePoints);

        // ------------------------------------------------------------------------------------------------------------------------------
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

    /*
    Description: Tug/pull an object towards the player when a tension threshold is exceeded.
    */
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

    /*
    Description: Whether or not the grapple is active (hooked or flying).
    Return Type: bool
    */
    public bool IsActive()
    {
        return m_bGrappleHookActive;
    }
}
