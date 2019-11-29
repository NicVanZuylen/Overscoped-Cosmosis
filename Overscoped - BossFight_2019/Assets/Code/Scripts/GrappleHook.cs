using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Handles all input and physics for the grapple and pull core game mechanics.
 * Author: Nic Van Zuylen
*/

enum EGrappleState
{
    GRAPPLE_IDLE,
    GRAPPLE_CASTING,
    GRAPPLE_EXTENDING,
    GRAPPLE_HOOKED
};

enum EGrappleMode
{
    GRAPPLE_MODE_GRAPPLE,
    GRAPPLE_MODE_PULL
};

[RequireComponent(typeof(PlayerController))]

public class GrappleHook : MonoBehaviour
{
    // Private: 

    // -------------------------------------------------------------------------------------------------
    [Header("Object References")]

    [SerializeField]
    private LineRenderer m_grappleLine = null;

    [Tooltip("Grapple impact point object.")]
    [SerializeField]
    private GameObject m_grapplePoint = null;

    [SerializeField]
    private Transform m_grappleNode = null;

    // -------------------------------------------------------------------------------------------------
    [Header("Physics")]

    [Tooltip("Acceleration due to grapple pull.")]
    [SerializeField]
    private float m_fPullAcceleration = 30.0f;

    [Tooltip("Acceleration when moving with WASD while grappling.")]
    [SerializeField]
    private float m_fGrappleMoveAcceleration = 30.0f;

    [Tooltip("Maximum speed in the direction of the grapple line the player can travel.")]
    [SerializeField]
    private float m_fMaxFlySpeed = 40.0f;

    [Tooltip("Radius from the hook in which the player will detach.")]
    [SerializeField]
    private float m_fDestinationRadius = 2.0f;

    [Tooltip("Impulse added to the player upwards when reaching the grapple destination.")]
    [SerializeField]
    private float m_fDestinationUpPush = 80.0f;

    [Tooltip("Impulse added to the player upwards when reaching the grapple destination.")]
    [SerializeField]
    private float m_fDestinationNormalPush = 30.0f;

    [Tooltip("Allowance of lateral movement when grappling.")]
    [SerializeField]
    private float m_fDriftTolerance = 8.0f;

    [Tooltip("The distance the grapple hook will travel before cancelling.")]
    [SerializeField]
    private float m_fGrappleRange = 20.0f;

    [Tooltip("The minimum time grappling before the release boost can be applied.")]
    [SerializeField]
    private float m_fMinReleaseBoostTime = 0.3f;

    [Tooltip("The magnitude of the forward force applied to the player upon rope release.")]
    [SerializeField]
    private float m_fReleaseForce = 20.0f;

    [Tooltip("New player acceleration due to gravity after releasing the grapple.")]
    [SerializeField]
    private float m_fReleaseGravity = -9.81f;

    [Tooltip("The magnitude of the foward force applied to the player when grappling from the ground.")]
    [SerializeField]
    private float m_fForwardGroundGrappleForce = 10.0f;

    [Tooltip("The magnitude of the upward force applied to the player when grappling from the ground.")]
    [SerializeField]
    private float m_fUpGroundGrappleForce = 5.0f;

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

    [Tooltip("Particle VFX playing from the player's hands when the grapple is in use.")]
    [SerializeField]
    private ParticleObject m_grappleHandVFX = new ParticleObject();

    [Tooltip("VFX played at the point of impact of the grapple.")]
    [SerializeField]
    private ParticleObject m_grappleImpactVFX = new ParticleObject();

    [Tooltip("VFX played at the target point of the grapple when not in use.")]
    [SerializeField]
    private ParticleObject m_targetVFX = new ParticleObject();

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

    // -------------------------------------------------------------------------------------------------

    private PlayerController m_controller;
    private PlayerStats m_stats;
    private Animator m_animController;
    private CameraEffects m_cameraEffects;
    private Transform m_cameraTransform;
    private const int m_nRayMask = ~(1 << 2 | 1 << 9 | 1 << 11); // Layer bitmask includes every layer but: IgnoreRaycast, NoGrapple and Player.

    // Grapple function
    private PlayerBeam m_beamScript;
    private RaycastHit m_fireHit;
    private AudioSource m_impactAudioSource;
    private AudioLoop m_grappleLoopAudio;
    private float m_fGrapLineLength; // Distance from casting point to destination, (linear unlike the rope itself).
    private float m_fGrappleLineProgress; // Distance along the linear rope distance currently covered by the rope while casting.
    private float m_fGrappleHookedTime; // Elapsed time from the point of grappling.
    private static float m_fGrappleVolume = 1.0f;
    private EGrappleState m_grappleState;
    private EGrappleMode m_grappleMode;
    private bool m_bWaitForRelease; // Whether or not to wait for mouse button release before allowing grappling again.

    // Pull function
    private PullObject m_pullObj;

    // Both functions.
    private bool m_bWithinRange;

    // Effects
    private LineEffects m_grapLineEffects;

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

        // Grapple end point object.
        m_impactAudioSource = m_grapplePoint.GetComponent<AudioSource>();
        m_impactAudioSource.maxDistance = m_fGrappleRange + 5.0f;

        // Component retreival.
        m_controller = GetComponent<PlayerController>();
        m_beamScript = GetComponent<PlayerBeam>();
        m_stats = GetComponent<PlayerStats>();
        m_animController = GetComponentInChildren<Animator>();

        // SFX

        if (!m_sfxSource)
            m_sfxSource = GetComponent<AudioSource>();

        m_grappleLoopAudio = new AudioLoop(m_grapplePullLoopSFX, gameObject, ESpacialMode.AUDIO_SPACE_NONE);

        // Camera

        Camera cam = GetComponentInChildren<Camera>();

        m_cameraEffects = cam.GetComponent<CameraEffects>();
        m_cameraTransform = cam.transform;

        // Set initial grapple status.
        m_fGrappleHookedTime = 0.0f;
        m_grappleState = EGrappleState.GRAPPLE_IDLE;
    }

    private void OnDestroy()
    {
        m_grapLineEffects.DestroyBuffers();
    }

    void Update()
    {
        if (PauseMenu.IsPaused())
            return;

        // ------------------------------------------------------------------------------------------------------------------------------
        // Current update state.

        // Switch for choosing update function based off of state.
        switch(m_grappleState)
        {
            case EGrappleState.GRAPPLE_IDLE:

                UpdateIdle();

                break;

            case EGrappleState.GRAPPLE_CASTING:

                UpdateCasting();

                break;

            case EGrappleState.GRAPPLE_EXTENDING:

                UpdateExtension();

                break;

            case EGrappleState.GRAPPLE_HOOKED:

                UpdateHooked();

                break;
        }
    }

    private void LateUpdate()
    {
        if (PauseMenu.IsPaused())
            return;

        m_grapLineEffects.ProcessLine(m_grappleLine, m_controller, m_grappleNode, m_grapplePoint.transform.position, m_fGrappleLineProgress / m_fGrapLineLength, m_grappleState > EGrappleState.GRAPPLE_CASTING);
    }

    /*
    Description: Begin extension of the grapple rope.
    */
    public void BeginExtension()
    {
        // Reset grapple line progress.
        m_fGrappleLineProgress = 0.0f;

        // Play VFX.
        if (!m_grappleHandVFX.IsPlaying())
            m_grappleHandVFX.Play();

        // Play SFX.
        m_sfxSource.PlayOneShot(m_grappleExtendSFX, m_fGrappleVolume);

        SetState(EGrappleState.GRAPPLE_EXTENDING);
    }

    /*
    Description: Perform a raycast attempting to detect a grapple-able surface. Return whether or not one was found.
    Return Type: bool
    */
    private bool GrappleRayCast()
    {
        Ray grapRay = new Ray(m_cameraTransform.position, m_cameraTransform.forward);

        // Return positive if the player is not currently grappling, has enough mana, 
        // is looking at a surface that can be grappled and that surface is not what the player is standing on.
        return m_grappleState == EGrappleState.GRAPPLE_IDLE && m_stats.EnoughMana() && Physics.SphereCast(grapRay, m_fHookRadius, out m_fireHit, m_fGrappleRange, m_nRayMask, QueryTriggerInteraction.Ignore)
            && !(m_controller.IsGrounded() && m_controller.GroundCollider() == m_fireHit.collider);
    }

    /*
    Description: Update function while the grapple is idle.
    */
    private void UpdateIdle()
    {
        // Get raycast result...
        m_bWithinRange = GrappleRayCast();

        // Update target indicator VFX.
        if (m_bWithinRange)
        {
            m_targetVFX.SetPosition(m_fireHit.point);

            if (!m_targetVFX.IsPlaying())
                m_targetVFX.Play();
        }
        else if (m_targetVFX.IsPlaying())
            m_targetVFX.Stop(ParticleSystemStopBehavior.StopEmittingAndClear);

        // If the raycast returned positive and the player is holding left mouse...
        if (m_bWithinRange && !m_bWaitForRelease && Input.GetMouseButton(0))
        {
            // Begin grapple sequence.
            BeginCast();
        }
        else if (m_bWaitForRelease && !Input.GetMouseButton(0))
            m_bWaitForRelease = false;
    }

    /*
    Description: Begin casting animation (arm extension)
    */
    private void BeginCast()
    {
        // Determine mode...
        if (m_fireHit.collider.tag == "PullObj")
        {
            m_grappleMode = EGrappleMode.GRAPPLE_MODE_PULL;
            m_pullObj = m_fireHit.collider.GetComponent<PullObject>();
        }
        else
            m_grappleMode = EGrappleMode.GRAPPLE_MODE_GRAPPLE;

        // Set grapple destination point position, parent & distance.
        Transform grapplePointTransform = m_grapplePoint.transform;

        grapplePointTransform.position = m_fireHit.point;
        grapplePointTransform.parent = m_fireHit.collider.transform;
        grapplePointTransform.rotation = Quaternion.LookRotation(m_fireHit.normal, Vector3.up); // Rotate grapple point to face normal, to rotate the VFX.
        m_fGrapLineLength = m_fireHit.distance;

        // Play SFX.
        if (m_grappleFireSFX)
            m_sfxSource.PlayOneShot(m_grappleFireSFX, m_fGrappleVolume);

        // Stop target VFX.
        if (m_targetVFX.IsPlaying())
            m_targetVFX.Stop(ParticleSystemStopBehavior.StopEmittingAndClear);

        SetState(EGrappleState.GRAPPLE_CASTING);
    }

    /*
    Description: Update function while in the casting grapple state.
    */
    private void UpdateCasting()
    {
        // Release condition.
        if (!Input.GetMouseButton(0))
            ReleaseGrapple();
    }

    /*
    Description: Update grapple rope extension.
    */
    private void UpdateExtension()
    {
        // Release condition.
        if (!Input.GetMouseButton(0))
        {
            ReleaseGrapple();
            return;
        }

        m_fGrappleLineProgress += m_fGrappleLineSpeed * Time.deltaTime;
        m_fGrappleLineProgress = Mathf.Clamp(m_fGrappleLineProgress, 0.0f, m_fGrapLineLength);

        // Switch to hooked state once fully extended.
        if (m_fGrappleLineProgress >= m_fGrapLineLength)
        {
            OnHook();
        }
    }

    /*
    Description: Update function in the grapple hooked state.
    */
    private void UpdateHooked()
    {
        if (m_grappleMode == EGrappleMode.GRAPPLE_MODE_GRAPPLE)
        {
            // Increment time since grapple hooked.
            m_fGrappleHookedTime += Time.deltaTime;

            // Apply subtle camera shake.
            m_cameraEffects.ApplyShake(0.1f, 0.1f);

            // Add small FOV offset.
            m_cameraEffects.AddFOVOffset(7.0f);

            // Play loop SFX
            if (!m_grappleLoopAudio.IsPlaying())
                m_grappleLoopAudio.Play(m_fGrappleVolume);
        }
        else // Update pull mode.
            UpdatePull();

        // Release comdition
        if (!Input.GetMouseButton(0) || !m_stats.EnoughMana())
            ReleaseGrapple();
    }

    /*
    Description: Runs once on the frame the grapple hooks onto an object.
    */
    private void OnHook()
    {
        m_fGrappleLineProgress = m_fGrapLineLength;
        SetState(EGrappleState.GRAPPLE_HOOKED);

        // Apply camera shake.
        m_cameraEffects.ApplyShake(0.5f, 1.0f, true);

        // Play impact VFX
        if (!m_grappleImpactVFX.IsPlaying())
            m_grappleImpactVFX.Play();

        // Play impact SFX.
        if(m_grappleImpactSFX)
            m_impactAudioSource.PlayOneShot(m_grappleImpactSFX, m_fGrappleVolume);

        // Apply boost from the ground.
        if(m_controller.IsGrounded())
        {
            Vector3 v3Impulse = (m_cameraTransform.forward * m_fForwardGroundGrappleForce) + (m_cameraTransform.up * m_fUpGroundGrappleForce);
            m_controller.AddImpulse(v3Impulse);
        }

        // Begin overrides.
        if(m_grappleMode == EGrappleMode.GRAPPLE_MODE_GRAPPLE)
        {
            m_controller.OverrideMovement(GrappleFly);
            m_controller.OverrideCollision(OnGrappleCollision);
        }
    }

    /*
    Description: Release the grapple.
    Param:
        bool bWaitForRelease: Whether or not to force the player to release the mouse button before attempting to grapple again.
    */
    public void ReleaseGrapple(bool bWaitForRelease = false)
    {
        // Set new gravity value when releasing from hooked state.
        if (m_grappleState == EGrappleState.GRAPPLE_HOOKED && m_grappleMode == EGrappleMode.GRAPPLE_MODE_GRAPPLE)
            m_controller.SetGravity(m_fReleaseGravity);

        SetState(EGrappleState.GRAPPLE_IDLE);

        // Release grapple overrides.
        if(m_grappleMode == EGrappleMode.GRAPPLE_MODE_GRAPPLE)
        {
            m_controller.FreeMovementOverride();
            m_controller.FreeCollisionOverride();
        }

        // Stop VFX.
        if(m_grappleHandVFX.IsPlaying())
            m_grappleHandVFX.Stop();

        if (m_grappleImpactVFX.IsPlaying())
            m_grappleImpactVFX.Stop();

        // Stop loop SFX.
        if (m_grappleLoopAudio.IsPlaying())
            m_grappleLoopAudio.Stop();

        // Release boost
        if(m_fGrappleHookedTime >= m_fMinReleaseBoostTime)
            m_controller.AddImpulse(m_controller.LookForward() * m_fReleaseForce);

        // Flag to wait for release.
        m_bWaitForRelease = bWaitForRelease;

        // Reset stats.
        m_fGrappleHookedTime = 0.0f;
    }

    /*
    Description: Player controller override that controls player physics whilst grappling. This includes pull & swing physics.
                 Returns the new velocity of the player controller for the current frame.
    Return Type: Vector3
    */
    private Vector3 GrappleFly(PlayerController controller, float fDeltaTime)
    {
        Vector3 v3NetForce = Vector3.zero;

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
        v3NetForce += v3MoveDir * m_fGrappleMoveAcceleration * fDeltaTime;

        // Lateral drag.
        if (v3NonPullComponent.sqrMagnitude < 1.0f)
            v3NetForce -= v3NonPullComponent * m_fDriftTolerance * fDeltaTime;
        else
            v3NetForce -= v3NonPullComponent.normalized * m_fDriftTolerance * fDeltaTime;

        // Stop when within radius of the hook.
        if (v3GrappleDif.sqrMagnitude <= m_fDestinationRadius * m_fDestinationRadius)
        {
            // Disable grapple.
            ReleaseGrapple(true);

            // Run collision code.
            v3NetForce += OnGrappleCollision(controller, fDeltaTime);

            // Stop looping SFX.
            m_grappleLoopAudio.Stop();

            m_controller.FreeMovementOverride();
            m_controller.SetGravity(m_controller.JumpGravity());

            return m_controller.GetVelocity() + v3NetForce;
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

    Vector3 OnGrappleCollision(PlayerController controller, float fDeltaTime)
    {
        Vector3 v3Impulse = Vector3.zero;
        Vector3 v3GrappleNormal = m_fireHit.normal;

        // Push up force, the push will only be applied if the surface normal is not pointing upwards.
        if (Vector3.Dot(v3GrappleNormal, Vector3.up) < 0.9f)
        {
            Vector3 v3GrappleRight = Vector3.Cross(v3GrappleNormal, Vector3.up);
            Vector3 v3GrappleUp = Vector3.Cross(v3GrappleRight, v3GrappleNormal); // Vector upwards along the surface normal.

            // Multiplier for additional force added to the upwards push, the less v3GrappleUp points upwards the greater the push.
            float fUpMult = 1.0f - Vector3.Dot(v3GrappleUp, Vector3.up);

            v3Impulse += v3GrappleUp * (m_fDestinationUpPush + (m_fDestinationUpPush * fUpMult));
            v3Impulse += -controller.LookForward() * m_fDestinationNormalPush;

            Debug.Log("thing");
        }

        return v3Impulse;
    }

    /*
    Description: Tug/pull an object towards the player when a tension threshold is exceeded.
    */
    private void UpdatePull()
    {
        if(m_pullObj == null)
        {
            // Free the pull.
            ReleaseGrapple();

            return;
        }

        Vector3 v3ObjDiff = transform.position - m_fireHit.point;
        Vector3 v3ObjDir = v3ObjDiff.normalized;
        float fRopeDistSqr = v3ObjDiff.magnitude; // Current rope length squared.

        // Distance beyond initial rope distance on impact the rope must be pulled to to decouple the pull object. (Squared)
        float fBreakDistSqr = m_fPullBreakDistance * m_fPullBreakDistance;

        float fDistBeyondThreshold = fRopeDistSqr - m_fGrapLineLength; // Distance beyond inital rope length.
        float fTension = fDistBeyondThreshold / m_fPullBreakDistance; // Tension value used to sample the color gradient and detect when the pull object should decouple.

        m_pullObj.SetTension(fTension);

        if (fTension >= 1.0f)
        {
            // Object is decoupled.
            if(m_pullObj != null)
                m_pullObj.Trigger(v3ObjDir);

            m_pullObj.SetTension(0.0f);

            // Free the pull hook.
            ReleaseGrapple();
        }
    }

    /*
    Description: Set the current state of the grapple, and update the animation controller.
    Param:
        EGrappleState state: The new state to transition to.
    */
    private void SetState(EGrappleState state)
    {
        m_grappleState = state;
        m_animController.SetInteger("grappleState", (int)m_grappleState);
    }

    /*
    Description: Whether or not the grapple is active (hooked or flying).
    Return Type: bool
    */
    public bool GrappleActive()
    {
        return m_grappleState > 0;
    }

    /*
    Description: Begin extending the grapple spell from the player's hand.
    */
    public void BeginGrapple()
    {
       
    }

    /*
    Description: Get whether or not the player is within grapple range of the target.
    Return Type: bool
    */
    public bool InGrappleRange()
    {
        return m_bWithinRange || m_grappleState > 0;
    }

    public static void SetVolume(float fVolume, float fMaster)
    {
        m_fGrappleVolume = fVolume * fMaster;
    }
}
