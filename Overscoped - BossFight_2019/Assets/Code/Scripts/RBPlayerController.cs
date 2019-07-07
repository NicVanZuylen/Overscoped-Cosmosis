using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class RBPlayerController : MonoBehaviour
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

    [Tooltip("Movement velocity drag multiplier when on the ground.")]
    public float m_fMovementDrag = 1.0f;

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

    private Rigidbody m_rigidbody;
    private CapsuleCollider m_collider;
    private Vector3 m_v3RespawnPosition;

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
        m_rigidbody = GetComponent<Rigidbody>();
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
        m_rigidbody.AddForce(m_fJumpForce * m_v3SurfaceUp, ForceMode.Impulse);
    }

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

    public bool IsGrounded()
    {
        return m_bOnGround;
    }

    public Vector3 GetVelocity()
    {
        return m_rigidbody.velocity;
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

        if (Input.GetKeyDown(KeyCode.F))
        {
            m_rigidbody.AddForce(m_v3SurfaceForward * 25.0f, ForceMode.Impulse);
            m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0.0f, m_rigidbody.velocity.z);
        }

        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceRight * 3.0f), Color.red);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceUp * 3.0f), Color.green);
        Debug.DrawLine(transform.position, transform.position + (m_v3SurfaceForward * 3.0f), Color.blue);
    }

    private void FixedUpdate()
    {
        // ------------------------------------------------------------------------------------------------------
        // Ground check

        Ray sphereRay = new Ray(transform.position, -transform.up);
        RaycastHit hit = new RaycastHit();
        float fSphereRadius = m_collider.radius + 0.01f;
        float fBottomSphereHeight = m_collider.height * 0.25f;

        // Sphere case down to the nearest surface below. Sphere cast is ignored if not falling.
        //if (m_rigidbody.velocity.y <= 0.0f || m_bOnGround)
        //{
            bool bSphereCastHit = Physics.SphereCast(sphereRay, fSphereRadius, out hit);

            float fHitDist = hit.distance;
            m_bOnGround = bSphereCastHit && fHitDist <= (m_collider.height * 0.25f);

            // Sometimes the character controller does not detect collisions with the ground and update the surface transform...
            // So to be sure its updated we update it using the sphere case hit.
            if (bSphereCastHit)
            {
                CalculateSurfaceTransform(hit);

                if (hit.collider.tag == "CheckPoint") // Set respawn checkpoint.
                    m_v3RespawnPosition = hit.collider.bounds.center + (Vector3.up * m_fRespawnHeight);
            }

            Debug.DrawLine(sphereRay.origin, hit.point);
        //}
        //else
        //    m_bOnGround = false;

        // ------------------------------------------------------------------------------------------------------
        // Movement override

        if (m_bOverridden)
        {
            //m_overrideFunction(this);

            return;
        }

        // ------------------------------------------------------------------------------------------------------
        // Jumping

        if (m_bOnGround && Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
            m_bOnGround = false;
        }

        // ------------------------------------------------------------------------------------------------------
        // Movement

        if(m_bOnGround)
        {
            Vector3 v3MoveDir = MoveDirection();

            // Lateral drag

            Vector3 v3VelNoY = m_rigidbody.velocity;
            v3VelNoY.y = 0.0f;

            if (v3MoveDir.sqrMagnitude > 0.0f)
            {
                // Component magnitude of the current velocity in the direction of movement.
                float fMoveDirComponent = Vector3.Dot(v3VelNoY, v3MoveDir);
            
                // Component of the velocity not in the direction of movement.
                Vector3 v3NonMoveComponent = v3VelNoY - (v3MoveDir * fMoveDirComponent);

                // Add force to counter act lateral velocity.
                m_rigidbody.AddForce(-v3NonMoveComponent * 3.0f, ForceMode.Acceleration);

                // Movement velocity.

                float fCompInVelocity = Mathf.Clamp(Vector3.Dot(v3VelNoY, v3MoveDir), 0.0f, m_fMaxMoveSpeed);
                float fMoveAmount = m_fMaxMoveSpeed - fCompInVelocity;

                //Vector3 v3Acceleration = v3MoveDir * m_fGroundAcceleration;
                Vector3 v3Acceleration = v3MoveDir * fMoveAmount * m_fGroundAcceleration;

                //m_rigidbody.AddForce(v3Acceleration, ForceMode.Acceleration);
                m_rigidbody.velocity += v3Acceleration * Time.fixedDeltaTime;
            }
            else
            {
                if (Input.GetKeyUp(KeyCode.W) && m_rigidbody.velocity.y > 0.0f)
                    m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0.0f, m_rigidbody.velocity.z);

                // Foot drag.
                Vector3 v3FootDrag = v3VelNoY;
                m_rigidbody.velocity -= v3FootDrag * 10.0f * Time.fixedDeltaTime;
                //m_rigidbody.AddForce(-v3FootDrag * 10.0f, ForceMode.Acceleration);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "CheckPoint")
        {
            // Set respawn collision to the centre + half its height + 10.
            m_v3RespawnPosition = collision.collider.bounds.center + new Vector3(0.0f, (collision.collider.bounds.extents.y * 0.5f) + 10.0f, 0.0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Killbox" && m_v3RespawnPosition != null) // Respawn player.
        {
            // Get velocity and remove x and z components.
            Vector3 v3BodyVelocity = m_rigidbody.velocity;
            v3BodyVelocity.x = 0.0f;
            v3BodyVelocity.z = 0.0f;

            // Apply new velocity.
            m_rigidbody.velocity = v3BodyVelocity;

            // Teleport to spawn point.
            transform.position = m_v3RespawnPosition;
        }
    }
}
