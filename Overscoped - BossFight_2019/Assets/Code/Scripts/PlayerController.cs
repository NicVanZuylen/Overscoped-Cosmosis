using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    // Private:

    [Header("Grounded")]
    [Space(10)]
    [Tooltip("Rate of acceleration when on a ground surface.")]
    [SerializeField]
    private float m_fGroundAcceleration = 40.0f;

    [Tooltip("Maximum input-induced movement speed whilst grounded.")]
    [SerializeField]
    private float m_fMaxGroundMoveSpeed = 5.0f;

    [Tooltip("Maximum input-induced movement speed whilst grounded and sprinting.")]
    [SerializeField]
    private float m_fMaxSprintMoveSpeed = 8.0f;

    [Tooltip("Maximum angle offset from directly forwards sprinting is allowed.")]
    [SerializeField]
    private float m_fSprintAngleOffset = 30.0f;

    [Tooltip("Velocity drag when on the ground & below maximum movement speed.")]
    [SerializeField]
    private float m_fGroundDrag = 10.0f;

    [Tooltip("Movement drag when on the ground and still under momentum from flight.")]
    [SerializeField]
    private float m_fMomentumDrag = 5.0f;

    [Tooltip("Drag applied when moving in the direction of velocity and exceeding max speed.")]
    [SerializeField]
    private float m_fSlideDrag = 5.0f;

    [Header("In-air")]
    [Space(10)]
    [Tooltip("Rate of acceleration when jumping/falling.")]
    [SerializeField]
    private float m_fAirAcceleration = 0.1f;

    [Tooltip("Maximum input-induced movement speed whilst flying.")]
    [SerializeField]
    private float m_fMaxAirborneMoveSpeed = 2.5f;

    [Tooltip("Movement drag while in the air.")]
    [SerializeField]
    private float m_fAirDrag = 5.0f;

    [Header("Jumping")]
    [Tooltip("Maximum horizontal distance travelled whilst jumping before landing.")]
    [SerializeField]
    private float m_fJumpDistance = 5.0f;

    [Tooltip("Maximum height reached whilst jumping.")]
    [SerializeField]
    private float m_fJumpHeight = 3.0f;

    [Header("Misc.")]
    [Space(10)]

    [Tooltip("Height of the player above the ground when they respawn.")]
    [SerializeField]
    private float m_fRespawnHeight = 10.0f;

    [Tooltip("Whether or not the player is standing on a walkable surface.")]
    [SerializeField]
    private bool m_bOnGround = false;

    // Movement
    private CharacterController m_controller;
    private CameraEffects m_cameraEffects;
    private Animator m_animController;
    private RaycastHit m_groundHit;
    private Vector3 m_v3RespawnPosition;
    private Vector3 m_v3MoveDirection;
    private Vector3 m_v3SurfaceRight;
    private Vector3 m_v3SurfaceUp;
    private Vector3 m_v3SurfaceForward;
    private Vector3 m_v3Velocity;
    private float m_fSprintDot;
    private bool m_bShouldSprint;
    private bool m_bShouldJump;
    private bool m_bSlopeLimit;

    // Jumping
    private float m_fJumpGravity; // Gravity used when jumping.
    private float m_fJumpDuration; // Jump time duration.
    private float m_fJumpInitialVelocity; // Initial impulse applied when jumping.
    private float m_fCurrentGravity;
    private int m_nJumpFrame;

    // Looking
    Transform m_cameraTransform;
    private float m_fLookEulerX;
    private float m_fLookEulerY;
    private float m_fFOVIncrease;

    // Overrides
    public delegate Vector3 OverrideFunction(PlayerController controller);

    OverrideFunction m_overrideFunction;
    bool m_bOverridden;

    private void Awake()
    {
        m_controller = GetComponent<CharacterController>();
        m_cameraEffects = GetComponentInChildren<CameraEffects>();
        m_animController = GetComponentInChildren<Animator>();
        m_cameraTransform = GetComponentInChildren<Camera>().transform;
        m_v3RespawnPosition = transform.position;

        // Set intitial camrera look rotation.
        Vector3 v3LookEuler = m_cameraTransform.rotation.eulerAngles;
        m_fLookEulerX = v3LookEuler.x;
        m_fLookEulerY = v3LookEuler.y;

        m_v3SurfaceUp = transform.up;
        m_v3SurfaceForward = transform.forward;
        m_v3SurfaceRight = transform.right;

        // Convert the sprint angle to a dot product value.
        m_fSprintDot = 1.0f - (m_fSprintAngleOffset / 90.0f);

        m_fJumpGravity = Physics.gravity.y;
        m_fJumpDuration = m_fJumpDistance / m_fMaxGroundMoveSpeed;

        // Next two jumping variables are determined using the kinematic formula: D = 1/2at^2 + vt for projectile motion.

        // Determine gravity level used when jumping.
        m_fJumpGravity = (-2 * m_fJumpHeight) / ((m_fJumpDuration * 0.5f) * (m_fJumpDuration * 0.5f));

        m_fCurrentGravity = m_fJumpGravity;

        // Determine initial impulse added to vertical velocity when jumping.
        m_fJumpInitialVelocity = (2 * m_fJumpHeight) / (m_fJumpDuration * 0.5f);
    
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        m_controller.enabled = false;
    }

    private void OnEnable()
    {
        m_controller.enabled = true;
    }

    /*
    Description: Get a quaternion describing the look rotation of the player.
    Return Type: Quaternion
    */
    public Quaternion LookRotation()
    {
        return Quaternion.Euler(m_fLookEulerX, m_fLookEulerY, 0.0f);
    }

    /*
    Description: Get the intended movement direction of the player.
    Return Type: Vector3
    */
    private Vector3 MoveDirection()
    {
        Vector3 v3Direction = Vector3.zero;
        int nMoveDirectionCount = 0;

        // On the ground, move relative to the camera and the surface.
        if (Input.GetKey(KeyCode.W))
        {
            ++nMoveDirectionCount;
            v3Direction += m_v3SurfaceForward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            ++nMoveDirectionCount;
            v3Direction -= m_v3SurfaceRight;
        }
        if (Input.GetKey(KeyCode.S))
        {
            ++nMoveDirectionCount;
            v3Direction -= m_v3SurfaceForward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            ++nMoveDirectionCount;
            v3Direction += m_v3SurfaceRight;
        }

        // Cancel diagonal movement speed boost.
        if (nMoveDirectionCount >= 2)
            v3Direction = v3Direction.normalized;

        return v3Direction;
    }

    /*
    Description: Get the intended movement direction of the player for fixed update functions.
    Return Type: Vector3
    */
    public Vector3 MoveDirectionFixed()
    {
        return m_v3MoveDirection;
    }

    /*
    Description: Get whether or not the player is standing on a walkable surface.
    Return Type: bool
    */
    public bool IsGrounded()
    {
        return m_bOnGround;
    }

    /*
    Description: Get the current velocity from the character controller.
    Return Type: Vector3
    */
    public Vector3 GetVelocity()
    {
        return m_controller.velocity;
    }

    /*
    Description: Set a movement override delegate that will run instead of the default movement behaviour.
    */
    public void OverrideMovement(OverrideFunction function)
    {
        m_overrideFunction = function;
        m_bOverridden = true;
    }

    /*
    Description: Remove the movement override. (Re-enables default movement behaviour.)
    */
    public void FreeOverride()
    {
        m_bOverridden = false;
    }

    /*
    Description: Get whether or not the controller's default movement behaviour is currently overridden.
    Return Type: bool
    */
    public bool IsOverridden()
    {
        return m_bOverridden;
    }

    /*
    Description: Set the acceleration due to gravity of the player. 
    Param:
        float fGravity: The acceleration due to gravity to use.
    */
    public void SetGravity(float fGravity)
    {
        m_fCurrentGravity = fGravity;
    }

    /*
    Description: Get the acceleration due to gravity of the player.
    Return Type: float
    */
    public float GetGravity()
    {
        return m_fCurrentGravity;
    }

    /*
    Description: Get the default acceleration due to gravity calculated from jump height and distance.
    */
    public float JumpGravity()
    {
        return m_fJumpGravity;
    }

    /*
    Description: Get the forward look vector of the player.
    Return Type: Vector3
    */
    public Vector3 LookForward()
    {
        Vector3 v3Forward;
        Vector3 v3Up;
        Vector3 v3Right;

        // Calculate flat vectors using worldspace up as the normal.
        CalculateSurfaceAxesUnlimited(Vector3.up, out v3Forward, out v3Up, out v3Right);

        return v3Forward;
    }

    /*
    Description: Get the right look vector of the player.
    Return Type: Vector3
    */
    public Vector3 LookRight()
    {
        Vector3 v3Forward;
        Vector3 v3Up;
        Vector3 v3Right;

        // Calculate flat vectors using worldspace up as the normal.
        CalculateSurfaceAxesUnlimited(Vector3.up, out v3Forward, out v3Up, out v3Right);

        return v3Right;
    }

    /*
    Description: Get the standing surface-relative forward vector of the player.
    Return Type: Vector3
    */
    public Vector3 SurfaceForward()
    {
        return m_v3SurfaceForward;
    }

    /*
    Description: Get the standing surface-relative right vector of the player.
    Return Type: Vector3
    */
    public Vector3 SurfaceRight()
    {
        return m_v3SurfaceRight;
    }

    /*
    Description: Add an impulse to the velocity of the controller.
    Param:
        Vector3 v3Impulse: The impulse to be applied to velocity.
    */
    public void AddImpulse(Vector3 v3Impulse)
    {
        m_v3Velocity += v3Impulse;
    }

    /*
    Description: Calculate surface-parallel movement vectors, flat if beyond the slope limit.
    Param:
        Vector3 v3Normal: The surface normal used for calculating surface-parallel vectors.
    */
    private void CalculateSurfaceAxes(Vector3 v3Normal)
    {
        Vector3 v3SlopeRight = Vector3.Cross(v3Normal, Vector3.up);
        Vector3 v3SlopeForward = Vector3.Cross(v3SlopeRight, v3Normal);
        float fSurfaceAngle = Vector3.Dot(v3SlopeForward, Vector3.up);

        // If the slope is too steep the surface vectors will be flattened.
        if ((Mathf.Abs(fSurfaceAngle) * 90.0f) >= m_controller.slopeLimit)
        {
            // Find component of velocity pushing against the slope.
            float fVelCompNormal = Vector3.Dot(m_v3Velocity, -v3Normal);
            Vector3 v3VelAgainstNormal = fVelCompNormal * v3Normal;

            // Push back on the player with a slightly larger force to allow them to slide down the surface.
            m_v3Velocity += v3VelAgainstNormal * 1.1f;

            v3Normal = Vector3.up;
            m_bSlopeLimit = true;
        }
        else
            m_bSlopeLimit = false;

        m_v3SurfaceUp = v3Normal;
        m_v3SurfaceForward = Vector3.Cross(m_cameraTransform.right, m_v3SurfaceUp);
        m_v3SurfaceRight = Vector3.Cross(m_v3SurfaceUp, m_v3SurfaceForward);
    }

    /*
    Description: Calculate surface-parallel axis vectors.
    Param:
        Vector3 v3Normal: The surface normal used for calculating surface-parallel axes.
        Vector3 v3Forward: The output forward axis.
        Vector3 v3Up: The output up axis.
        Vector3 v3Right: The output right axis.
    */
    public void CalculateSurfaceAxesUnlimited(Vector3 v3Normal, out Vector3 v3Forward, out Vector3 v3Up, out Vector3 v3Right)
    {
        v3Up = v3Normal;
        v3Forward = Vector3.Cross(m_cameraTransform.right, v3Up);
        v3Right = Vector3.Cross(v3Up, v3Forward);
    }

    /*
    Description: Respawn's the player on the last-walkable checkpoint surface if their Y falls below a constant value.
    */
    private void RespawnBelowMinY()
    {
        if (transform.position.y < -80.0f)
        {
            // Get velocity and remove x and z components.
            Vector3 v3BodyVelocity = m_controller.velocity;
            v3BodyVelocity.x = 0.0f;
            v3BodyVelocity.z = 0.0f;

            // Apply new velocity.
            m_v3Velocity = v3BodyVelocity;

            // Teleport to spawn point.
            m_controller.enabled = false;
            transform.position = m_v3RespawnPosition;
            m_controller.enabled = true;
        }
    }

    private void FixedUpdate()
    {
        // ------------------------------------------------------------------------------------------------------
        // Movement override

        if (m_bOverridden)
        {
            // Run override behaviour and use it's velocity output.
            m_v3Velocity = m_overrideFunction(this);

            // Respawn they player if they fall too far.
            RespawnBelowMinY();

            return;
        }

        // ------------------------------------------------------------------------------------------------------
        // Movement

        // Movement and dragging forces are added to this vector, which is added to the final velocity.
        Vector3 v3NetForce = Vector3.zero;

        m_fFOVIncrease = 0.0f;

        if (m_bOnGround)
        {
            // ------------------------------------------------------------------------------------------------------
            // Grounded movement

            // Lateral drag

            if (m_v3MoveDirection.sqrMagnitude > 0.0f)
            {
                // ------------------------------------------------------------------------------------------------------
                // Sprinting.

                float fCurrentGroundMaxSpeed = m_fMaxGroundMoveSpeed;

                float fMoveDirDot = Vector3.Dot(m_v3MoveDirection, LookForward());

                if (m_bShouldSprint && fMoveDirDot >= m_fSprintDot)
                {
                    m_fFOVIncrease = 10.0f;
                    fCurrentGroundMaxSpeed = m_fMaxSprintMoveSpeed;
                }
                    
                // ------------------------------------------------------------------------------------------------------
                // Play animation...

                m_animController.SetBool("isRunning", true);

                // ------------------------------------------------------------------------------------------------------
                // Movement lateral tolerance.

                // Component magnitude of the current velocity in the direction of movement.
                float fMoveDirComponent = Vector3.Dot(m_v3Velocity, m_v3MoveDirection);
            
                // Component of the velocity not in the direction of movement.
                Vector3 v3NonMoveComponent = m_v3Velocity - (m_v3MoveDirection * fMoveDirComponent);

                // Add force to counter act lateral velocity.
                v3NetForce -= v3NonMoveComponent * 3.0f * Time.fixedDeltaTime;

                // ------------------------------------------------------------------------------------------------------
                // Running

                float fCompInVelocity = Mathf.Clamp(Vector3.Dot(m_v3Velocity, m_v3MoveDirection), 0.0f, fCurrentGroundMaxSpeed);
                float fMoveAmount = fCurrentGroundMaxSpeed - fCompInVelocity;

                Vector3 v3SlideDrag = Vector3.zero;

                if(m_v3Velocity.sqrMagnitude > fCurrentGroundMaxSpeed * fCurrentGroundMaxSpeed)
                {
                    v3SlideDrag -= m_v3Velocity * m_fSlideDrag * Time.fixedDeltaTime;
                }

                Vector3 v3Acceleration = m_v3MoveDirection * fMoveAmount * m_fGroundAcceleration * Time.fixedDeltaTime;

                v3NetForce += v3Acceleration + v3SlideDrag;
            }
            else
            {
                // ------------------------------------------------------------------------------------------------------
                // When not attempting to move.

                if(m_v3Velocity.sqrMagnitude <= m_fMaxGroundMoveSpeed * m_fMaxGroundMoveSpeed)
                {
                    // Foot drag.
                    v3NetForce -= m_v3Velocity * m_fGroundDrag * Time.fixedDeltaTime;
                }
                else
                {
                    // Slide drag.
                    v3NetForce -= m_v3Velocity * m_fMomentumDrag * Time.fixedDeltaTime;
                }

                m_animController.SetBool("isRunning", false);
            }
        }
        else
        {
            // ------------------------------------------------------------------------------------------------------
            // Play flying animation...
            m_animController.SetBool("isRunning", false);

            // ------------------------------------------------------------------------------------------------------
            // Reset movement vectors for airborn movement.

            // We're giving it the upward vector to represent a flat surface.
            //CalculateSurfaceAxes(m_groundHit.normal);
            CalculateSurfaceAxesUnlimited(Vector3.up, out m_v3SurfaceForward, out m_v3SurfaceUp, out m_v3SurfaceRight);

            // ------------------------------------------------------------------------------------------------------
            // Airborne movement

            Vector3 v3VelNor = m_v3Velocity.normalized;

            float fCompInVelocity = Vector3.Dot(v3VelNor, m_v3MoveDirection);
            float fMoveAmount = (1.0f - fCompInVelocity) * m_fMaxAirborneMoveSpeed;

            Vector3 v3Acceleration = m_v3MoveDirection * fMoveAmount * m_fAirAcceleration * Time.fixedDeltaTime;
            v3Acceleration.y = 0.0f;

            Vector3 v3AirDrag = m_v3Velocity * -m_fAirDrag * Time.fixedDeltaTime;
            v3AirDrag.y = 0.0f;
            
            v3NetForce += v3Acceleration + v3AirDrag;
        }

        // ------------------------------------------------------------------------------------------------------
        // Jumping

        if (m_nJumpFrame > 0)
        {
            // Remove external Y velocity factors affecting jumping.
            m_v3Velocity.y = 0.0f;

            // Add jumping impulse.
            v3NetForce.y = m_fJumpInitialVelocity;

            m_bShouldJump = false;
            m_bOnGround = false;

            --m_nJumpFrame;
        }
        

        // ------------------------------------------------------------------------------------------------------
        // Gravity.

        m_v3Velocity.y += m_fCurrentGravity * Time.fixedDeltaTime;
            
        // ------------------------------------------------------------------------------------------------------
        // Final forces.

        // Add net force.
        m_v3Velocity += v3NetForce;

        // Respawn the player if they fall too far.
        RespawnBelowMinY();
    }

    private void Update()
    {
        // ------------------------------------------------------------------------------------------------------
        // Mouse look

        m_fLookEulerX -= Input.GetAxis("Mouse Y");
        m_fLookEulerY += Input.GetAxis("Mouse X");

        m_fLookEulerX = Mathf.Clamp(m_fLookEulerX, -89.9f, 89.9f);

        m_cameraTransform.rotation = Quaternion.Euler(new Vector3(m_fLookEulerX, m_fLookEulerY, 0.0f) + m_cameraEffects.ShakeEuler());

        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceRight * 3.0f), Color.red);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceUp * 3.0f), Color.green);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceForward * 3.0f), Color.blue);

        // ------------------------------------------------------------------------------------------------------
        // Move direction

        m_v3MoveDirection = MoveDirection();

        // ------------------------------------------------------------------------------------------------------
        // Ground check

        bool bPrevGrounded = m_bOnGround;

        Ray sphereRay = new Ray(transform.position, -Vector3.up);

        // Sphere case down to the nearest surface below. Sphere cast is ignored if not falling.
        bool bSphereCastHit = Physics.SphereCast(sphereRay, m_controller.radius, out m_groundHit, Mathf.Infinity, int.MaxValue, QueryTriggerInteraction.Ignore);
        m_bOnGround = bSphereCastHit && m_groundHit.distance < (m_controller.height * 0.5f) - (m_controller.radius * 0.7f);

        // Sometimes the character controller does not detect collisions with the ground and update the surface transform...
        // So to be sure its updated we update it using the sphere case hit.
        if (m_bOnGround)
        {
            CalculateSurfaceAxes(m_groundHit.normal);

            // Reset gravity.
            m_fCurrentGravity = m_fJumpGravity;

            if (m_groundHit.collider.tag == "CheckPoint") // Set respawn checkpoint.
                m_v3RespawnPosition = m_groundHit.collider.bounds.center + new Vector3(0.0f, (m_groundHit.collider.bounds.extents.y * 0.5f) + m_fRespawnHeight, 0.0f);
        }

        Debug.DrawLine(sphereRay.origin, m_groundHit.point, Color.magenta);

        // CharacterController.isGrounded is a very unreliable method to check if the character is grounded. So this is used as a backup method instead.
        m_bOnGround |= m_controller.isGrounded;

        m_bOnGround &= !m_bSlopeLimit;

        // ---------------------------------------------------------------------------------------------------
        // FOV velocity effect.

        m_cameraEffects.SetFOVOffset(Mathf.Clamp((Vector3.Dot(m_v3Velocity, m_cameraTransform.forward) - m_fMaxGroundMoveSpeed) * 3 + m_fFOVIncrease, 0.0f, 15.0f));

        // ------------------------------------------------------------------------------------------------------
        // Sprint & jumping input

        bool bPrevShouldJump = m_bShouldJump;

        m_bShouldSprint = m_bOnGround && Input.GetKey(KeyCode.LeftShift);
        m_bShouldJump = m_bOnGround && Input.GetKey(KeyCode.Space);

        if (!bPrevShouldJump && m_bShouldJump)
        {
            m_nJumpFrame = 4; // Give multiple frames to clear the ground.
        }

        // ------------------------------------------------------------------------------------------------------
        // Movement

        // Move using current velocity delta.
        m_controller.Move(m_v3Velocity * Time.deltaTime);

        // Get modified velocity back from the controller.
        m_v3Velocity = m_controller.velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "CheckPoint")
        {
            // Set respawn collision to the centre + half its height + 10.
            m_v3RespawnPosition = collision.collider.bounds.center + new Vector3(0.0f, (collision.collider.bounds.extents.y * 0.5f) + m_fRespawnHeight, 0.0f);
        }
    }
}
