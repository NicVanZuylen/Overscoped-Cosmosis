using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    // Public:

    [Tooltip("Rate of acceleration when on a ground surface.")]
    public float m_fGroundAcceleration = 40.0f;

    [Tooltip("Rate of acceleration when jumping/falling.")]
    public float m_fAirAcceleration = 0.1f;

    public float m_fMaxMoveSpeed = 5.0f;

    [Tooltip("Maximum movement velocity when on a ground surface.")]
    public float m_fMaxGroundVelocity = 30.0f;

    [Tooltip("Force applied upwards when jumping.")]
    public float m_fJumpForce = 5.0f;

    [Tooltip("Movement velocity drag when on the ground.")]
    public float m_fMovementDrag = 50.0f;

    [Tooltip("Velocity drag when on the ground.")]
    public float m_fGroundDrag = 10.0f;

    [Tooltip("Movement drag when on the ground and still under momentum from flight.")]
    public float m_fMomentumDrag = 5.0f;

    [Tooltip("Movement drag while in the air.")]
    public float m_fAirDrag = 5.0f;

    [Tooltip("Maximum velocity whilst flying.")]
    public float m_fTerminalVelocity = 53.0f;

    [Tooltip("Height of the player above the ground when they respawn.")]
    public float m_fRespawnHeight = 10.0f;

    // Private:

    private CharacterController m_controller;
    private CapsuleCollider m_collider;
    private Vector3 m_v3RespawnPosition;

    // Main velocity
    private Vector3 m_v3Velocity;
    private Vector3 m_v3LastFrameVelocity;

    // Movement velocity
    private Vector3 m_v3MovementVelocity;
    private Vector3 m_v3LastFrameMoveVector;

    [SerializeField]
    private bool m_bOnGround;

    private Vector3 m_v3SurfaceRight;
    private Vector3 m_v3SurfaceUp;
    private Vector3 m_v3SurfaceForward;

    Transform m_cameraTransform;
    private float m_fLookEulerX;
    private float m_fLookEulerY;

    public delegate Vector3 OverrideFunction(PlayerController controller);

    OverrideFunction m_overrideFunction;
    bool m_bOverridden;

    void Awake()
    {
        m_controller = GetComponent<CharacterController>();
        m_collider = GetComponent<CapsuleCollider>();
        m_cameraTransform = GetComponentInChildren<Camera>().transform;
        m_v3RespawnPosition = transform.position;

        m_v3SurfaceUp = transform.up;
        m_v3SurfaceForward = transform.forward;
        m_v3SurfaceRight = transform.right;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Jump()
    {
        m_v3Velocity += m_fJumpForce * m_v3SurfaceUp;
    }

    public Vector3 WASDAcceleration(float acceleration)
    {
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceRight * 3.0f), Color.red);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceUp * 3.0f), Color.green);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceForward * 3.0f), Color.blue);

        Vector3 v3NetForce = Vector3.zero;
        bool bMoved = false;

        // On the ground, move relative to the camera and the surface.
        if (Input.GetKey(KeyCode.W))
        {
            bMoved = true;
            v3NetForce += m_v3SurfaceForward * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            bMoved = true;
            v3NetForce -= m_v3SurfaceRight * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            bMoved = true;
            v3NetForce -= m_v3SurfaceForward * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            bMoved = true;
            v3NetForce += m_v3SurfaceRight * acceleration * Time.deltaTime;
        }

        // Cancel diagonal movement speed boost.
        if (bMoved)
            v3NetForce = v3NetForce.normalized * acceleration;

        return v3NetForce * Time.deltaTime;
    }

    public bool IsGrounded()
    {
        return m_controller.isGrounded;
    }

    public Vector3 Drag(float fDrag)
    {
        /*
        Vector3 velNor;

        if (m_velocity.magnitude > 1.0f)
            velNor = m_velocity.normalized;
        else // Special case where speed is less than one. Normalizing the velocity here would cause an increase in speed.
            velNor = m_velocity;

        return  -velNor * drag * Time.deltaTime;
        */

        float fForwardComponent = Vector3.Dot(m_v3Velocity, m_v3SurfaceForward);
        float fLateralComponent = Vector3.Dot(m_v3Velocity, m_v3SurfaceRight);

        Vector3 v3DragVector = (fForwardComponent * m_v3SurfaceForward) + (fLateralComponent * m_v3SurfaceRight);

        if(v3DragVector.sqrMagnitude > 1.0f)
            return -v3DragVector.normalized * fDrag * Time.deltaTime;
        else
            return -v3DragVector * fDrag * Time.deltaTime;
    }

    public Vector3 Drag(float fDrag, Vector3 v3Velocity)
    {
        float fForwardComponent = Vector3.Dot(v3Velocity, m_v3SurfaceForward);
        float fLateralComponent = Vector3.Dot(v3Velocity, m_v3SurfaceRight);

        Vector3 v3DragVector = (fForwardComponent * m_v3SurfaceForward) + (fLateralComponent * m_v3SurfaceRight);

        if (v3DragVector.sqrMagnitude > 1.0f)
            return -v3DragVector.normalized * fDrag * Time.deltaTime;
        else
            return -v3DragVector * fDrag * Time.deltaTime;
    }

    public Vector3 DragMovement(float fDrag)
    {
        /*
        Vector3 velNor;

        if (m_velocity.magnitude > 1.0f)
            velNor = m_velocity.normalized;
        else // Special case where speed is less than one. Normalizing the velocity here would cause an increase in speed.
            velNor = m_velocity;

        return  -velNor * drag * Time.deltaTime;
        */

        float fForwardComponent = Vector3.Dot(m_v3MovementVelocity, m_v3SurfaceForward);
        float fLateralComponent = Vector3.Dot(m_v3MovementVelocity, m_v3SurfaceRight);

        Vector3 v3DragVector = (fForwardComponent * m_v3SurfaceForward) + (fLateralComponent * m_v3SurfaceRight);

        //float fVelComponent = Vector3.Dot(m_v3Velocity, v3DragVector);
        //v3DragVector = m_v3Velocity.normalized * fVelComponent;

        if (v3DragVector.sqrMagnitude > 1.0f)
            return -v3DragVector.normalized * fDrag * Time.deltaTime;
        else
            return -v3DragVector * fDrag * Time.deltaTime;
    }

    public Vector3 GetVelocity()
    {
        return m_v3Velocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        m_v3Velocity = velocity;
    }

    public void OverrideMovement(OverrideFunction function)
    {
        m_overrideFunction = function;
        m_bOverridden = true;
    }

    public void FreeOverride()
    {
        m_bOverridden = false;
    }

    Vector3 TestOverride(PlayerController controller)
    {
        return controller.GetVelocity();
    }

    public bool IsOverridden()
    {
        return m_bOverridden;
    }

    public Vector3 LookForward()
    {
        Vector3 dir = m_cameraTransform.forward;
        dir.y = 0.0f;
        return dir;
    }

    public Vector3 LookLeft()
    {
        Vector3 dir = -m_cameraTransform.right;
        dir.y = 0.0f;
        return dir;
    }

    public Vector3 LookRight()
    {
        Vector3 dir = m_cameraTransform.right;
        dir.y = 0.0f;
        return dir;
    }

    public Vector3 LookBack()
    {
        Vector3 dir = -m_cameraTransform.forward;
        dir.y = 0.0f;
        return dir;
    }

    void CalculateSurfaceTransform(ControllerColliderHit hit)
    {
        m_v3SurfaceUp = hit.normal;
        m_v3SurfaceForward = Vector3.Cross(m_cameraTransform.right, m_v3SurfaceUp);
        m_v3SurfaceRight = Vector3.Cross(m_v3SurfaceUp, m_v3SurfaceForward);
    }

    void CalculateSurfaceTransform(RaycastHit hit)
    {
        m_v3SurfaceUp = hit.normal;
        m_v3SurfaceForward = Vector3.Cross(m_cameraTransform.right, m_v3SurfaceUp);
        m_v3SurfaceRight = Vector3.Cross(m_v3SurfaceUp, m_v3SurfaceForward);
    }

    void Update()
    {
        // ------------------------------------------------------------------------------------------------------
        // Mouse look

        m_fLookEulerX -= Input.GetAxis("Mouse Y");
        m_fLookEulerY += Input.GetAxis("Mouse X");

        m_fLookEulerX = Mathf.Clamp(m_fLookEulerX, -89.9f, 89.9f);

        Quaternion targetCamRotation = Quaternion.Euler(m_fLookEulerX, m_fLookEulerY, 0.0f);
        m_cameraTransform.rotation = Quaternion.Slerp(m_cameraTransform.rotation, targetCamRotation, 0.7f);

        // ------------------------------------------------------------------------------------------------------

        if(m_bOverridden)
        {
            m_v3Velocity = m_overrideFunction(this);

            m_controller.Move(m_v3Velocity * Time.deltaTime);

            // Set velocity to controller velocity output to apply any changes made by the controller physics.
            m_v3Velocity = m_controller.velocity;

            return;
        }

        Vector3 v3NetForce = Vector3.zero;

        // ------------------------------------------------------------------------------------------------------
        // Checking if grounded

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
                CalculateSurfaceTransform(hit);

                if (hit.collider.tag == "CheckPoint") // Set respawn checkpoint.
                    m_v3RespawnPosition = hit.collider.bounds.center + (Vector3.up * m_fRespawnHeight);
            }

            Debug.DrawLine(sphereRay.origin, hit.point);
        }
        else
        {
            m_bOnGround = false;

            m_v3LastFrameMoveVector = Vector3.zero;
            m_v3LastFrameVelocity = Vector3.zero;
        }

        // CharacterController.isGrounded is a very unreliable method to check if the character is grounded. So this is used as a backup method instead.
        m_bOnGround |= m_controller.isGrounded;

        // ------------------------------------------------------------------------------------------------------
        // Jumping

        if (Input.GetKeyDown(KeyCode.Space) && m_bOnGround)
        {
            Jump();
            m_bOnGround = false;

            m_v3LastFrameMoveVector = Vector3.zero;
            m_v3LastFrameVelocity = Vector3.zero;
        }

        // ------------------------------------------------------------------------------------------------------
        // Ground movement velocity cancellation

        if (Input.GetKeyDown(KeyCode.F))
            m_v3Velocity += m_v3SurfaceForward * 100;

        if (m_bOnGround && m_v3LastFrameMoveVector.sqrMagnitude > 0.0f)
        {
            Vector3 v3LastFrameVelNor = m_v3LastFrameVelocity.normalized;
            
            float fLFMoveComp = Mathf.Clamp(Vector3.Dot(m_v3LastFrameVelocity, m_v3LastFrameMoveVector), 0.0f, m_v3LastFrameVelocity.magnitude);
            Vector3 v3LFMoveApp = fLFMoveComp * v3LastFrameVelNor;

            Vector3 v3SubVec = m_v3LastFrameMoveVector - v3LFMoveApp;

            m_v3Velocity -= m_v3LastFrameMoveVector;
        }
        

        // ------------------------------------------------------------------------------------------------------
        // Movement

        if (m_bOnGround)
        {
            /*
            Vector3 v3OutAcceleration = WASDAcceleration(m_fGroundAcceleration);
            v3NetForce += v3OutAcceleration;
            */

            // Calculate movement net force.
            Vector3 v3MoveNetForce = WASDAcceleration(m_fGroundAcceleration);

            // Drag movement velocity.
            if (v3MoveNetForce.sqrMagnitude <= 0.01f)
            {
                v3MoveNetForce += Drag(m_fMovementDrag, m_v3MovementVelocity);

                // Drag main velocity alongside movement velocity.
                v3NetForce -= DragMovement(m_fMovementDrag);
            }
                

            // Apply results.
            m_v3MovementVelocity += v3MoveNetForce;

            // Clamp movement velocity.
            if (m_v3MovementVelocity.sqrMagnitude > m_fMaxMoveSpeed * m_fMaxMoveSpeed)
                m_v3MovementVelocity = m_v3MovementVelocity.normalized * m_fMaxMoveSpeed;

            // Drag main velocity.
            if (v3NetForce.sqrMagnitude < 0.01f)
            {
                v3NetForce += Drag(m_fGroundDrag, m_v3Velocity);
            }
        }
        else
        {
            Vector3 v3OutAcceleration = WASDAcceleration(m_fAirAcceleration);
            v3NetForce += v3OutAcceleration;

            m_v3SurfaceRight = m_cameraTransform.right;
            m_v3SurfaceUp = transform.up;

            Vector3 v3ForwardVec = m_cameraTransform.forward;
            v3ForwardVec.y = 0.0f; // The Y component needs to be removed since it can mess with drag.
            v3ForwardVec.Normalize();

            m_v3SurfaceForward = v3ForwardVec;

            v3NetForce += Drag(m_fAirDrag);
        }

        // ------------------------------------------------------------------------------------------------------
        // Gravity

        // Apply gravity
        v3NetForce += Physics.gravity * Time.deltaTime;

        // ------------------------------------------------------------------------------------------------------
        // Apply net force

        m_v3Velocity += v3NetForce;

        // ------------------------------------------------------------------------------------------------------
        // Apply movement velocity if grounded.

        Vector3 v3MoveVector = Vector3.zero;

        if(m_bOnGround)
        {
            m_v3LastFrameVelocity = m_v3Velocity;

            Vector3 v3MoveDir = m_v3MovementVelocity.normalized;

            float fVelocityCompInMovDir = Vector3.Dot(m_v3Velocity, v3MoveDir);
            float fMoveComponent = Mathf.Clamp(m_fMaxMoveSpeed - Mathf.Max(fVelocityCompInMovDir, 0.0f), 0.0f, m_fMaxMoveSpeed);
            v3MoveVector = fMoveComponent * v3MoveDir;

            float fMoveVelMag = m_v3MovementVelocity.sqrMagnitude;

            if (v3MoveVector.sqrMagnitude > fMoveVelMag)
                v3MoveVector = v3MoveVector.normalized * Mathf.Sqrt(fMoveVelMag);

            // Store for use in the next frame.
            m_v3LastFrameMoveVector = v3MoveVector;

            // Apply to velocity.
            m_v3Velocity += v3MoveVector;
        }

        Debug.Log(m_v3MovementVelocity);

        // ------------------------------------------------------------------------------------------------------
        // Clamp velocity

        if (m_bOnGround && m_v3Velocity.sqrMagnitude > m_fMaxGroundVelocity * m_fMaxGroundVelocity)
        {
            float fVelY = m_v3Velocity.y;
        
            m_v3Velocity = m_v3Velocity.normalized * m_fMaxGroundVelocity;
            m_v3Velocity.y = fVelY;
        }
        else if (m_v3Velocity.sqrMagnitude > m_fTerminalVelocity * m_fTerminalVelocity)
            m_v3Velocity = m_v3Velocity.normalized * m_fTerminalVelocity;

        m_controller.Move(m_v3Velocity * Time.deltaTime);

        // Set velocity to controller velocity output to apply any changes made by the controller physics.
        m_v3Velocity = m_controller.velocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Killbox" && m_v3RespawnPosition != null) // Respawn player.
        {
            m_controller.enabled = false;
            transform.position = m_v3RespawnPosition;
            m_controller.enabled = true;
        }
    }
}
