using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    // Public:

    [Header("Grounded")]
    [Space(10)]
    [Tooltip("Rate of acceleration when on a ground surface.")]
    public float m_fGroundAcceleration = 40.0f;

    [Tooltip("Maximum input-induced movement speed whilst grounded.")]
    public float m_fMaxGroundMoveSpeed = 5.0f;

    [Tooltip("Maximum input-induced movement speed whilst grounded and sprinting.")]
    public float m_fMaxSprintMoveSpeed = 8.0f;

    [Tooltip("Maximum movement velocity when on a ground surface.")]
    public float m_fMaxGroundVelocity = 30.0f;

    [Tooltip("Velocity drag when on the ground & below maximum movement speed.")]
    public float m_fGroundDrag = 10.0f;

    [Tooltip("Movement velocity drag multiplier when on the ground.")]
    public float m_fMovementDrag = 1.0f;

    [Tooltip("Movement drag when on the ground and still under momentum from flight.")]
    public float m_fMomentumDrag = 5.0f;

    [Tooltip("Drag applied when moving in the direction of velocity and exceeding max speed.")]
    public float m_fSlideDrag = 5.0f;

    [Header("In-air")]
    [Space(10)]
    [Tooltip("Rate of acceleration when jumping/falling.")]
    public float m_fAirAcceleration = 0.1f;

    [Tooltip("Maximum input-induced movement speed whilst flying.")]
    public float m_fMaxAirbornMoveSpeed = 2.5f;

    [Tooltip("Movement drag while in the air.")]
    public float m_fAirDrag = 5.0f;

    [Tooltip("Maximum velocity whilst flying.")]
    public float m_fTerminalVelocity = 53.0f;

    [Header("Misc.")]
    [Space(10)]
    [Tooltip("Force applied upwards when jumping.")]
    public float m_fJumpForce = 5.0f;

    [Tooltip("Height of the player above the ground when they respawn.")]
    public float m_fRespawnHeight = 10.0f;

    // Private:

    // Movement
    private CharacterController m_controller;
    private CameraEffects m_cameraEffects;
    private Animator m_animController;
    private Vector3 m_v3RespawnPosition;
    private Vector3 m_v3SurfaceRight;
    private Vector3 m_v3SurfaceUp;
    private Vector3 m_v3SurfaceForward;
    private Vector3 m_v3Velocity;

    [Tooltip("Whether or not the player is standing on a walkable surface.")]
    [SerializeField]
    private bool m_bOnGround;

    // Looking
    Transform m_cameraTransform;
    private float m_fLookEulerX;
    private float m_fLookEulerY;

    // Overrides
    public delegate Vector3 OverrideFunction(PlayerController controller);

    OverrideFunction m_overrideFunction;
    bool m_bOverridden;

    void Awake()
    {
        m_controller = GetComponent<CharacterController>();
        m_cameraEffects = GetComponentInChildren<CameraEffects>();
        m_animController = GetComponentInChildren<Animator>();
        m_cameraTransform = GetComponentInChildren<Camera>().transform;
        m_v3RespawnPosition = transform.position;

        m_v3SurfaceUp = transform.up;
        m_v3SurfaceForward = transform.forward;
        m_v3SurfaceRight = transform.right;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /*
    Description: Get a force to be applied as a result of jumping.
    Return Type: Vector3
    */
    public Vector3 JumpForce()
    {
        return m_fJumpForce * m_v3SurfaceUp;
    }

    /*
    Description: Get the intended movement direction of the player.
    Return Type: Vector3
    */
    public Vector3 MoveDirection()
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
    Description: Get the forward look vector of the player.
    Return Type: Vector3
    */
    public Vector3 LookForward()
    {
        Vector3 dir = m_cameraTransform.forward;
        dir.y = 0.0f;
        return dir.normalized;
    }

    /*
    Description: Get the right look vector of the player.
    Return Type: Vector3
    */
    public Vector3 LookRight()
    {
        Vector3 dir = m_cameraTransform.right;
        dir.y = 0.0f;
        return dir.normalized;
    }

    /*
    Description: Calculate surface-parallel movement vectors.
    Param:
        Vector3 v3Normal: The surface normal used for calculating surface-parallel vectors.
    */
    void CalculateSurfaceTransform(Vector3 v3Normal)
    {
        m_v3SurfaceUp = v3Normal;
        m_v3SurfaceForward = Vector3.Cross(m_cameraTransform.right, m_v3SurfaceUp);
        m_v3SurfaceRight = Vector3.Cross(m_v3SurfaceUp, m_v3SurfaceForward);
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

    private void Update()
    {
        // ------------------------------------------------------------------------------------------------------
        // Mouse look

        m_fLookEulerX -= Input.GetAxis("Mouse Y");
        m_fLookEulerY += Input.GetAxis("Mouse X");

        m_fLookEulerX = Mathf.Clamp(m_fLookEulerX, -89.9f, 89.9f);

        Quaternion targetCamRotation = Quaternion.Euler(m_fLookEulerX, m_fLookEulerY, 0.0f);
        m_cameraTransform.rotation = Quaternion.Slerp(m_cameraTransform.rotation, targetCamRotation, 0.7f);

        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceRight * 3.0f), Color.red);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceUp * 3.0f), Color.green);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceForward * 3.0f), Color.blue);

        // ------------------------------------------------------------------------------------------------------
        // Ground check

        Ray sphereRay = new Ray(transform.position, -transform.up);
        RaycastHit hit = new RaycastHit();
        float fSphereRadius = m_controller.radius * 0.5f;

        // Sphere case down to the nearest surface below. Sphere cast is ignored if not falling.
        if (m_v3Velocity.y <= 0.0f || m_bOnGround)
        {
            bool bSphereCastHit = Physics.SphereCast(sphereRay, fSphereRadius, out hit);
            m_bOnGround = bSphereCastHit && hit.distance < (m_controller.height * 0.5f);

            // Sometimes the character controller does not detect collisions with the ground and update the surface transform...
            // So to be sure its updated we update it using the sphere case hit.
            if (bSphereCastHit)
            {
                CalculateSurfaceTransform(hit.normal);

                if (hit.collider.tag == "CheckPoint") // Set respawn checkpoint.
                    m_v3RespawnPosition = hit.collider.bounds.center + new Vector3(0.0f, (hit.collider.bounds.extents.y * 0.5f) + m_fRespawnHeight, 0.0f);
            }

            Debug.DrawLine(sphereRay.origin, hit.point);
        }
        else
        {
            m_bOnGround = false;
        }
            
        // CharacterController.isGrounded is a very unreliable method to check if the character is grounded. So this is used as a backup method instead.
        m_bOnGround |= m_controller.isGrounded;

        // ---------------------------------------------------------------------------------------------------
        // FOV velocity effect.

        m_cameraEffects.SetFOVOffset(Mathf.Clamp((Vector3.Dot(m_v3Velocity, m_cameraTransform.forward) - m_fMaxGroundMoveSpeed) * 3, 0.0f, 15.0f));

        // ------------------------------------------------------------------------------------------------------
        // Movement override

        if (m_bOverridden)
        {
            // Run override behaviour and use it's velocity output.
            m_v3Velocity = m_overrideFunction(this);

            // Respawn they player if they fall too far.
            RespawnBelowMinY();

            m_controller.Move(m_v3Velocity * Time.deltaTime);

            // Set velocity to controller velocity output to apply any changes made by the controller physics.
            m_v3Velocity = m_controller.velocity;

            return;
        }

        // ------------------------------------------------------------------------------------------------------
        // Jumping

        if (m_bOnGround && Input.GetKeyDown(KeyCode.Space))
        {
            m_v3Velocity += JumpForce();
            m_bOnGround = false;
        }

        // ------------------------------------------------------------------------------------------------------
        // Movement

        // Movement and dragging forces are added to this vector, which is added to the final velocity.
        Vector3 v3NetForce = Vector3.zero; 

        // Get intended movement direction of the player.
        Vector3 v3MoveDir = MoveDirection();

        if (m_bOnGround)
        {
            // ------------------------------------------------------------------------------------------------------
            // Grounded movement

            // Lateral drag

            if (v3MoveDir.sqrMagnitude > 0.0f)
            {
                // ------------------------------------------------------------------------------------------------------
                // Sprinting.
                float fCurrentGroundMaxSpeed = m_fMaxGroundMoveSpeed;

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    m_cameraEffects.SetFOVOffset(10.0f);
                    fCurrentGroundMaxSpeed = m_fMaxSprintMoveSpeed;
                }
                    
                // ------------------------------------------------------------------------------------------------------
                // Play animation...

                m_animController.SetBool("isRunning", true);

                // ------------------------------------------------------------------------------------------------------
                // Movement lateral tolerance.

                // Component magnitude of the current velocity in the direction of movement.
                float fMoveDirComponent = Vector3.Dot(m_v3Velocity, v3MoveDir);
            
                // Component of the velocity not in the direction of movement.
                Vector3 v3NonMoveComponent = m_v3Velocity - (v3MoveDir * fMoveDirComponent);

                // Add force to counter act lateral velocity.
                v3NetForce -= v3NonMoveComponent * 3.0f * Time.deltaTime;

                // ------------------------------------------------------------------------------------------------------
                // Running

                float fCompInVelocity = Mathf.Clamp(Vector3.Dot(m_v3Velocity, v3MoveDir), 0.0f, fCurrentGroundMaxSpeed);
                float fMoveAmount = fCurrentGroundMaxSpeed - fCompInVelocity;

                Vector3 v3SlideDrag = Vector3.zero;

                if(m_v3Velocity.sqrMagnitude > fCurrentGroundMaxSpeed * fCurrentGroundMaxSpeed)
                {
                    v3SlideDrag -= m_v3Velocity * m_fSlideDrag * Time.deltaTime;
                }

                Vector3 v3Acceleration = v3MoveDir * fMoveAmount * m_fGroundAcceleration;

                v3NetForce += (v3Acceleration + v3SlideDrag) * Time.deltaTime;
            }
            else
            {
                // ------------------------------------------------------------------------------------------------------
                // When not attempting to move.

                if(m_v3Velocity.sqrMagnitude <= m_fMaxGroundMoveSpeed * m_fMaxGroundMoveSpeed)
                {
                    // Foot drag.
                    v3NetForce -= m_v3Velocity * m_fGroundDrag * Time.deltaTime;
                }
                else
                {
                    // Slide drag.
                    v3NetForce -= m_v3Velocity * m_fMomentumDrag * Time.deltaTime;
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
            CalculateSurfaceTransform(Vector3.up);

            // ------------------------------------------------------------------------------------------------------
            // Airborn movement

            float fCompInVelocity = Mathf.Clamp(Vector3.Dot(m_v3Velocity, v3MoveDir), 0.0f, m_fMaxAirbornMoveSpeed);
            float fMoveAmount = m_fMaxAirbornMoveSpeed - fCompInVelocity;
            
            Vector3 v3Acceleration = v3MoveDir * fMoveAmount * m_fAirAcceleration;
            v3Acceleration.y = 0.0f;

            v3NetForce += v3Acceleration * Time.deltaTime;
        }

        // Gravity.
        m_v3Velocity += Physics.gravity * Time.deltaTime;

        // Add net force.
        m_v3Velocity += v3NetForce;

        // Move using current velocity delta.
        m_controller.Move(m_v3Velocity * Time.deltaTime);

        // Get modified velocity back from the controller.
        m_v3Velocity = m_controller.velocity;

        // Respawn the player if they fall too far.
        RespawnBelowMinY();
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
