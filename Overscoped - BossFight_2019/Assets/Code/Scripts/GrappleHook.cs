using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(AudioSource))]

public class GrappleHook : MonoBehaviour
{
    // Private: 

    // -------------------------------------------------------------------------------------------------
    [Header("Object References")]

    [SerializeField]
    private LineRenderer m_grappleLine = null;

    [SerializeField]
    private LineRenderer m_pullLine = null;

    [Tooltip("Grapple impact point object.")]
    [SerializeField]
    private GameObject m_grapplePoint = null;

    [SerializeField]
    private Transform m_grappleNode = null;

    [SerializeField]
    private Transform m_pullNode = null;

    // -------------------------------------------------------------------------------------------------
    [Header("Physics")]

    [Tooltip("Acceleration due to grapple pull.")]
    [SerializeField]
    private float m_fPullAcceleration = 30.0f;

    [Tooltip("Acceleration due to grapple pull.")]
    [SerializeField]
    private float m_fAirAcceleration = 30.0f;

    [Tooltip("Maximum speed in the direction of the grapple line the player can travel.")]
    [SerializeField]
    private float m_fMaxFlySpeed = 40.0f;

    [Tooltip("Radius from the hook in which the player will detach.")]
    [SerializeField]
    private float m_fDestinationRadius = 2.0f;

    [Tooltip("Allowance of lateral movement when grappling.")]
    [SerializeField]
    private float m_fDriftTolerance = 8.0f;

    [Tooltip("Player move acceleration when landing after successfully reaching the grapple hook.")]
    [SerializeField]
    private float m_fLandMoveAcceleration = 5.0f;

    [Tooltip("The distance the grapple hook will travel before cancelling.")]
    [SerializeField]
    private float m_fGrappleRange = 20.0f;

    [Tooltip("The minimum time grappling before the release boost can be applied.")]
    [SerializeField]
    private float m_fMinReleaseBoostTime = 0.3f;

    [Tooltip("The magnitude of the forward force applied to the player upon rope release.")]
    [SerializeField]
    private float m_fReleaseForce = 20.0f;

    [Tooltip("The magnitude of the foward force applied to the player when grappling from the ground.")]
    [SerializeField]
    private float m_fForwardGroundGrappleForce = 10.0f;

    [Tooltip("The magnitude of the upward force applied to the player when grappling from the ground.")]
    [SerializeField]
    private float m_fUpGroundGrappleForce = 5.0f;

    [Tooltip("Whether or not to use the default jumping gravity whilst flying after grapple.")]
    [SerializeField]
    private bool m_bJumpGravOnRelease = false;

    // -------------------------------------------------------------------------------------------------
    [Header("Grapple Mode")]

    [Tooltip("Rate in which the pull line will travel towards its destination.")]
    [SerializeField]
    private float m_fGrappleLineSpeed = 30.0f;

    // -------------------------------------------------------------------------------------------------
    [Header("Pull Mode")]

    [Tooltip("The distance the pull hook must be extended beyond the initial rope length on impact to decouple the target object.")]
    [SerializeField]
    private float m_fPullBreakDistance = 8.0f;

    [Tooltip("Rate in which the pull line will travel towards its destination.")]
    [SerializeField]
    private float m_fPullLineSpeed = 30.0f;

    // -------------------------------------------------------------------------------------------------
    [Header("Misc.")]

    [Tooltip("Whether or not the player will be restricted a spherical volume with the rope length as the radius when grappling.")]
    [SerializeField]
    private bool m_bRestrictToRopeLength = true;

    [Tooltip("Spherecast radius for firing the grapple hook.")]
    [SerializeField]
    private float m_fHookRadius = 0.5f;

    // -------------------------------------------------------------------------------------------------
    [Header("Effects")]

    [Tooltip("Delay between getting a new set of random points for the rope shake effect.")]
    [SerializeField]
    private float m_fRopeShakeDelay = 0.1f;

    [Tooltip("Speed of rope point movement between random points.")]
    [SerializeField]
    private float m_fShakeSpeed = 0.1f;

    [Tooltip("Maximum offset of rope points from thier position on the curve.")]
    [SerializeField]
    private float m_fShakeMagnitude = 0.05f;

    [Tooltip("Amount of waves in the rope whilst it flies.")]
    [SerializeField]
    private int m_nWaveCount = 3;

    [Tooltip("Wave amplitude multiplier of the wobble effect.")]
    [SerializeField]
    private float m_fRippleWaveAmp = 0.15f;

    [Tooltip("Thickness the line will expand to when popping.")]
    [SerializeField]
    public float m_fPopThickness = 2.0f;

    [Tooltip("Rate in which the line will expand and pop after use.")]
    [SerializeField]
    private float m_fPopRate = 3.0f;

    [Tooltip("Amount of time shake is amplified following rope impact.")]
    [SerializeField]
    private float m_fImpactShakeDuration = 0.6f;

    [Tooltip("Multiplier for the impact shake effect.")]
    [SerializeField]
    private float m_fImpactShakeMult = 5.0f;

    [SerializeField]
    private GameObject m_grappleHandEffect = null;

    [SerializeField]
    private GameObject m_impactEffect = null;

    [SerializeField]
    private ComputeShader m_lineCompute = null;

    // -------------------------------------------------------------------------------------------------
    [Header("Sound Effects")]

    [SerializeField]
    private AudioClip m_grappleFireSFX = null;

    [SerializeField]
    private AudioClip m_grappleExtendSFX = null;

    [SerializeField]
    private AudioClip m_grappleImpactSFX = null;

    [SerializeField]
    private AudioClip m_grapplePullLoopSFX = null;

    [SerializeField]
    private AudioSource m_sfxSource = null;

    [SerializeField]
    private AudioSource m_grappleLoopAudioSource = null;

    // -------------------------------------------------------------------------------------------------

    private PlayerController m_controller;
    private PlayerStats m_stats;
    private Animator m_animController;
    private CameraEffects m_cameraEffects;
    private Transform m_cameraTransform;
    private bool m_bPullHookActive;

    // Grapple function
    private PlayerBeam m_beamScript;
    private RaycastHit m_fireHit;
    private AudioSource m_impactAudioSource;
    private Vector3 m_v3GrappleNormal;
    private Vector3 m_v3GrappleBoost;
    private float m_fGrapLineLength; // Distance from casting point to destination, (linear unlike the rope itself).
    private float m_fGrappleLineProgress; // Distance along the linear rope distance currently covered by the rope while casting.
    private float m_fGrappleTime; // Elapsed time from the point of grappling.
    private bool m_bGrappleHookActive;
    private bool m_bGrappleLocked;
    private bool m_bGrappleJustImpacted;

    // Pull function
    private PullObject m_pullObj;
    private GameObject m_pullEndPoint;
    private Vector3 m_v3PullTension;
    private float m_fPullLineLength;
    private float m_fPullLineProgress;
    private bool m_bPullLocked;
    private bool m_bPullJustImpacted;

    // Both functions.
    private bool m_bWithinRange;

    // Effects
    private LineEffects m_grapLineEffects;
    private LineEffects m_pullLineEffects;

    // Misc.
    private float m_fReleaseGravity;

    void Awake()
    {
        LineEffectParameters lineEffectParams;
        lineEffectParams.m_nWaveCount = m_nWaveCount;
        lineEffectParams.m_fRopeShakeDelay = m_fRopeShakeDelay;
        lineEffectParams.m_fShakeSpeed = m_fShakeSpeed;
        lineEffectParams.m_fShakeMagnitude = m_fShakeMagnitude;
        lineEffectParams.m_fRippleWaveAmp = m_fRippleWaveAmp;
        lineEffectParams.m_fPopThickness = m_fPopThickness;
        lineEffectParams.m_fPopRate = m_fPopRate;
        lineEffectParams.m_fImpactShakeDuration = m_fImpactShakeDuration;
        lineEffectParams.m_fImpactShakeMult = m_fImpactShakeMult;
        lineEffectParams.m_fLineThickness = m_grappleLine.startWidth;

        m_grapLineEffects = new LineEffects(Instantiate(m_lineCompute), lineEffectParams, m_grappleLine.positionCount);

        lineEffectParams.m_fLineThickness = m_pullLine.startWidth;

        m_pullLineEffects = new LineEffects(Instantiate(m_lineCompute), lineEffectParams, m_pullLine.positionCount);

        // Grapple end point object.
        m_impactAudioSource = m_grapplePoint.GetComponent<AudioSource>();
        m_impactAudioSource.maxDistance = m_fGrappleRange + 5.0f;

        m_pullEndPoint = new GameObject("Pull_End_Point");

        // Component retreival.
        m_controller = GetComponent<PlayerController>();
        m_beamScript = GetComponent<PlayerBeam>();
        m_stats = GetComponent<PlayerStats>();
        m_animController = GetComponentInChildren<Animator>();

        if (!m_sfxSource)
            m_sfxSource = GetComponent<AudioSource>();

        if (!m_grappleLoopAudioSource)
            m_grappleLoopAudioSource = GetComponent<AudioSource>();

        m_grappleLoopAudioSource.clip = m_grapplePullLoopSFX;

        Camera cam = GetComponentInChildren<Camera>();

        m_cameraEffects = cam.GetComponent<CameraEffects>();
        m_cameraTransform = cam.transform;

        // Misc.
        m_fGrappleTime = 0.0f;
        m_bGrappleHookActive = false;

        if (m_bJumpGravOnRelease)
            m_fReleaseGravity = m_controller.JumpGravity();
        else
            m_fReleaseGravity = Physics.gravity.y;
    }

    private void OnDestroy()
    {
        m_grapLineEffects.DestroyBuffers();
        m_pullLineEffects.DestroyBuffers();
    }

    void Update()
    {
        if (PauseMenu.IsPaused())
            return;

        // ------------------------------------------------------------------------------------------------------------------------------
        // Shooting

        Ray grapRay = new Ray(m_cameraTransform.position, m_cameraTransform.forward);

        bool bPlayerHasEnoughMana = m_stats.EnoughMana();

        // Spherecast to find impact point.
        const int nRaymask = ~(1 << 2); // Layer bitmask includes every layer but the ignore raycast layer.
        m_bWithinRange = Physics.SphereCast(grapRay, m_fHookRadius, out m_fireHit, m_fGrappleRange, nRaymask, QueryTriggerInteraction.Ignore);

        // Grapple casting.
        if (bPlayerHasEnoughMana && !m_bGrappleHookActive && !m_beamScript.BeamEnabled() && Input.GetMouseButton(0) && m_bWithinRange && m_fireHit.collider.tag != "NoGrapple")
        {
            // Play SFX
            if(m_grappleFireSFX)
                m_sfxSource.PlayOneShot(m_grappleFireSFX);

            if(m_grappleExtendSFX)
                m_sfxSource.PlayOneShot(m_grappleExtendSFX);

            m_grapplePoint.transform.position = m_fireHit.point;
            m_grapplePoint.transform.parent = m_fireHit.collider.transform;
            m_v3GrappleNormal = m_fireHit.normal;
            m_fGrapLineLength = m_fireHit.distance;

            // Fly time.
            m_fGrappleTime = 0.0f;

            m_controller.FreeOverride();

            m_bGrappleHookActive = true;
        }

        // Pull casting.
        if (bPlayerHasEnoughMana && !m_bPullHookActive && !m_beamScript.BeamUnlocked() && Input.GetMouseButton(1) && m_bWithinRange && m_fireHit.collider.tag == "PullObj")
        {
            // Find & enable pull object script.
            m_pullObj = m_fireHit.collider.gameObject.GetComponent<PullObject>();

            // Set end point to hit point and parent it to the hit object.
            m_pullEndPoint.transform.position = m_fireHit.point;
            m_pullEndPoint.transform.parent = m_fireHit.collider.transform;

            //m_v3PullPoint = m_fireHit.point;
            m_fPullLineLength = m_fireHit.distance;

            m_bPullHookActive = true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // Effects & Animations

        // Show particle effect at the hit point while aiming.
        if(!m_bGrappleHookActive && !m_bPullHookActive && m_bWithinRange)
        {
            m_impactEffect.transform.position = m_fireHit.point;
            m_impactEffect.transform.rotation = Quaternion.LookRotation(m_fireHit.normal, Vector3.up);
        }

        // Line will disable when the pop effect finishes.
        m_grappleHandEffect.SetActive(m_bGrappleHookActive);

        bool bImpacted = m_bGrappleLocked && m_bGrappleHookActive;

        // Animations
        m_animController.SetBool("isCasting", m_bGrappleHookActive);

        m_impactEffect.SetActive(m_bWithinRange);

        m_animController.SetBool("isGrappled", bImpacted);

        // ------------------------------------------------------------------------------------------------------------------------------
        // Active behaviour

        if (m_bGrappleHookActive)
        {
            m_fGrappleLineProgress += m_fGrappleLineSpeed * Time.deltaTime;
            m_fGrappleLineProgress = Mathf.Clamp(m_fGrappleLineProgress, 0.0f, m_fGrapLineLength);

            bool bPrevLocked = m_bGrappleLocked;
            m_bGrappleLocked = m_fGrappleLineProgress >= m_fGrapLineLength;

            m_bGrappleJustImpacted = !bPrevLocked && m_bGrappleLocked;

            if (m_bGrappleLocked)
            {
                if (m_bGrappleJustImpacted)
                {
                    // Play impact SFX.
                    if(m_grappleImpactSFX)
                        m_impactAudioSource.PlayOneShot(m_grappleImpactSFX);

                    // Apply camera shake.
                    m_cameraEffects.ApplyShake(0.05f, 1.0f, true);

                    // Apply initial force if grounded.
                    if (m_controller.IsGrounded())
                    {
                        m_v3GrappleBoost = m_cameraTransform.forward * m_fForwardGroundGrappleForce;
                        m_v3GrappleBoost += m_cameraTransform.up * m_fUpGroundGrappleForce;
                    }

                    m_bGrappleJustImpacted = false;
                }
                else
                {
                    m_cameraEffects.ApplyShake(0.1f, 0.1f);
                    m_v3GrappleBoost = Vector3.zero;
                }

                // Play loop SFX
                if (!m_grappleLoopAudioSource.isPlaying)
                    m_grappleLoopAudioSource.Play();

                // Increment grapple time.
                m_fGrappleTime += Time.deltaTime;

                // Add small FOV offset.
                m_cameraEffects.AddFOVOffset(7.0f);

                // Begin grapple override.
                m_controller.OverrideMovement(GrappleFly);

                // Exit when releasing the left mouse button.
                if (!Input.GetMouseButton(0) || m_stats.GetMana() <= 0.0f)
                {
                    m_controller.FreeOverride();
                    m_controller.SetGravity(m_fReleaseGravity);

                    m_bGrappleHookActive = false;
                    m_fGrappleLineProgress = 0.0f;

                    m_grappleLoopAudioSource.Stop();

                    // Release impulse.
                    if (m_fGrappleTime >= m_fMinReleaseBoostTime)
                    {
                        m_controller.AddImpulse(m_controller.LookForward() * m_fReleaseForce);
                    }
                }
            }
        }

        if(m_bPullHookActive)
        {
            m_fPullLineProgress += m_fPullLineSpeed * Time.deltaTime;
            m_fPullLineProgress = Mathf.Clamp(m_fPullLineProgress, 0.0f, m_fPullLineLength);

            bool bPrevLocked = m_bPullLocked;
            m_bPullLocked = m_fPullLineProgress >= m_fPullLineLength;

            m_bPullJustImpacted = !bPrevLocked && m_bPullLocked;

            if (m_bPullLocked)
            {
                if (m_bPullJustImpacted)
                {
                    m_cameraEffects.ApplyShake(0.05f, 1.0f, true);

                    m_bPullJustImpacted = false;
                }
                else
                {
                    m_cameraEffects.ApplyShake(0.1f, 0.1f);
                }

                // Pulling...
                PullObject();

                // Exit when releasing the right mouse button.
                if (!Input.GetMouseButton(1) || m_stats.GetMana() <= 0.0f)
                {
                    if (m_pullObj != null)
                        m_pullObj.LetGo();

                    m_bPullHookActive = false;
                    m_fPullLineProgress = 0.0f;
                }
            }
        }
        // ------------------------------------------------------------------------------------------------------------------------------
    }

    private void LateUpdate()
    {
        if (PauseMenu.IsPaused())
            return;

        m_grapLineEffects.ProcessLine(m_grappleLine, m_controller, m_grappleNode, m_grapplePoint.transform.position, m_fGrappleLineProgress / m_fGrapLineLength, m_bGrappleHookActive);
        m_pullLineEffects.ProcessLine(m_pullLine, m_controller, m_pullNode, m_pullEndPoint.transform.position, m_fPullLineProgress / m_fPullLineLength, m_bPullHookActive);
    }


    Vector3 GrappleFly(PlayerController controller, float fDeltaTime)
    {
        Vector3 v3NetForce = m_v3GrappleBoost;

        Vector3 v3GrappleDif = m_grapplePoint.transform.position - transform.position;
        Vector3 v3GrappleDir = v3GrappleDif.normalized;
        
        // Component of velocity in the direction of the grapple.
        float fPullComponent = Vector3.Dot(controller.GetVelocity(), v3GrappleDir);
        
        // Component of velocity not in the direction of the grapple.
        Vector3 v3NonPullComponent = controller.GetVelocity() - (v3GrappleDir * fPullComponent);

        if (fPullComponent < m_fMaxFlySpeed)
           v3NetForce += v3GrappleDir * m_fPullAcceleration * fDeltaTime;

        Vector3 v3MoveDir = Vector3.zero;

        // Calculate local movement axes.
        Vector3 v3Forward;
        Vector3 v3Up;
        Vector3 v3Right;
        int nDirCount = 0;

        m_controller.CalculateSurfaceAxesUnlimited(Vector3.up, out v3Forward, out v3Up, out v3Right);

        // Get player movement direction.
        if (Input.GetKey(KeyCode.W))
        {
            v3MoveDir += v3Forward;
            ++nDirCount;
        }
        if (Input.GetKey(KeyCode.A))
        {
            v3MoveDir -= v3Right;
            ++nDirCount;
        }
        if (Input.GetKey(KeyCode.S))
        {
            v3MoveDir -= v3Forward;
            ++nDirCount;
        }
        if (Input.GetKey(KeyCode.D))
        {
            v3MoveDir += v3Right;
            ++nDirCount;
        }

        // Normalize only if necessary.
        if (nDirCount > 1)
            v3MoveDir.Normalize();

        // Controls
        v3NetForce += v3MoveDir * m_fAirAcceleration * fDeltaTime;

        // Lateral drag.
        if (v3NonPullComponent.sqrMagnitude < 1.0f)
            v3NetForce -= v3NonPullComponent * m_fDriftTolerance * fDeltaTime;
        else
            v3NetForce -= v3NonPullComponent.normalized * m_fDriftTolerance * fDeltaTime;

        // Stop when within radius of the hook.
        if (v3GrappleDif.sqrMagnitude <= m_fDestinationRadius * m_fDestinationRadius)
        {
            // Disable grapple.
            m_bGrappleHookActive = false;
            m_fGrappleLineProgress = 0.0f;

            // Stop looping SFX.
            m_grappleLoopAudioSource.Stop();

            m_controller.FreeOverride();
            m_controller.SetGravity(m_controller.JumpGravity());
        }

        // The component of the velocity + net force away from the grapple point.
        float tension = Vector3.Dot(m_controller.GetVelocity() + v3NetForce, v3GrappleDir);

        // Prevent the rope from stretching beyond the rope length established when initially grappling.
        if (m_bRestrictToRopeLength && v3GrappleDif.sqrMagnitude > m_fGrapLineLength && tension < 0.0f)
        {
            v3NetForce -= tension * v3GrappleDir;
        }

        return m_controller.GetVelocity() + v3NetForce;
    }

    Vector3 GrappleLand(PlayerController controller, float fDeltaTime)
    {
        Vector3 v3NetForce = Vector3.zero;

        // Calculate 2-dimensional movement vectors.
        Vector3 v3ForwardVec = controller.LookForward();
        Vector3 v3RightVec = Vector3.Cross(Vector3.up, v3ForwardVec);

        // Movement during grapple landing phase.
        if (Input.GetKey(KeyCode.W))
            v3NetForce += v3ForwardVec * m_fLandMoveAcceleration * fDeltaTime;
        if (Input.GetKey(KeyCode.A))
            v3NetForce -= v3RightVec * m_fLandMoveAcceleration * fDeltaTime;
        if (Input.GetKey(KeyCode.S))
            v3NetForce -= v3ForwardVec * m_fLandMoveAcceleration * fDeltaTime;
        if (Input.GetKey(KeyCode.D))
            v3NetForce += v3RightVec * m_fLandMoveAcceleration * fDeltaTime;

        // Add gravity.
        v3NetForce += Physics.gravity * Time.fixedDeltaTime;

        Vector3 v3Velocity = controller.GetVelocity();

        // Drag
        if(v3Velocity.sqrMagnitude < 1.0f)
        {
            v3Velocity.x -= v3Velocity.x * 5.0f * fDeltaTime;
            v3Velocity.z -= v3Velocity.x * 5.0f * fDeltaTime;
        }
        else 
        {
            Vector3 v3VelNor = v3Velocity.normalized;
            v3Velocity.x -= v3VelNor.x * 5.0f * fDeltaTime;
            v3Velocity.z -= v3VelNor.z * 5.0f * fDeltaTime;
        }

        // Free override on landing.
        if (controller.IsGrounded())
        {
            controller.FreeOverride();
            controller.SetGravity(controller.JumpGravity());
        }

        return v3Velocity + v3NetForce;
    }

    /*
    Description: Tug/pull an object towards the player when a tension threshold is exceeded.
    */
    void PullObject()
    {
        if(m_pullObj == null)
        {
            // Free the pull hook.
            m_bPullHookActive = false;
            m_fPullLineProgress = 0.0f;

            return;
        }

        Vector3 v3ObjDiff = transform.position - m_pullEndPoint.transform.position;
        Vector3 v3ObjDir = v3ObjDiff.normalized;
        float fRopeDistSqr = v3ObjDiff.magnitude; // Current rope length squared.

        // Distance beyond initial rope distance on impact the rope must be pulled to to decouple the pull object. (Squared)
        float fBreakDistSqr = m_fPullBreakDistance * m_fPullBreakDistance;

        float fDistBeyondThreshold = fRopeDistSqr - m_fPullLineLength; // Distance beyond inital rope length.
        float fTension = fDistBeyondThreshold / m_fPullBreakDistance; // Tension value used to sample the color gradient and detect when the pull object should decouple.

        m_pullObj.SetTension(fTension);

        if (fTension >= 1.0f)
        {
            // Object is decoupled.
            if(m_pullObj != null)
                m_pullObj.Trigger(v3ObjDir);

            m_pullObj.SetTension(0.0f);
        
            // Free the pull hook.
            m_bPullHookActive = false;
            m_fPullLineProgress = 0.0f;
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

    /*
    Description: Break the grapple teather. 
    */
    public void ReleaseGrapple()
    {
        m_bGrappleHookActive = false;
        m_fGrappleLineProgress = 0.0f;

        m_controller.FreeOverride();
    }

    /*
    Description: Get whether or not the player is within grapple range of the target.
    Return Type: bool
    */
    public bool InGrappleRange()
    {
        return m_bWithinRange || m_bGrappleHookActive || m_bPullHookActive;
    }
}
