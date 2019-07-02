using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    // Public:

    [Tooltip("Rate of acceleration when on a ground surface.")]
    public float m_groundAcceleration = 40.0f;

    [Tooltip("Rate of acceleration when jumping/falling.")]
    public float m_airAcceleration = 0.1f;

    [Tooltip("Maximum movement speed when on a ground surface.")]
    public float m_maxSpeed = 5.0f;

    [Tooltip("Force applied upwards when jumping.")]
    public float m_jumpForce = 5.0f;

    [Tooltip("Movement drag when on the ground.")]
    public float m_groundDrag = 50.0f;

    [Tooltip("Movement drag while in the air.")]
    public float m_airDrag = 5.0f;

    [Tooltip("Maximum velocity whilst flying.")]
    public float m_terminalVelocity = 53.0f;

    // Private:

    private CharacterController m_controller;
    private CapsuleCollider m_collider;

    private Vector3 m_velocity;

    [SerializeField]
    private bool m_onGround;

    // Surface variables.
    private BoxCollider m_groundDetectionBox;
    private bool m_boxDetectedGround;

    private Vector3 m_surfaceRight;
    private Vector3 m_surfaceUp;
    private Vector3 m_surfaceForward;

    Transform m_cameraTransform;
    private float m_lookEulerX;
    private float m_lookEulerY;

    public delegate Vector3 OverrideFunction(PlayerController controller);

    OverrideFunction m_overrideFunction;
    bool m_overridden;

    void Awake()
    {
        m_controller = GetComponent<CharacterController>();
        m_collider = GetComponent<CapsuleCollider>();
        m_cameraTransform = GetComponentInChildren<Camera>().transform;
        m_groundDetectionBox = GetComponent<BoxCollider>();

        Physics.IgnoreCollision(m_groundDetectionBox, m_controller);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Jump()
    {
        m_velocity += m_jumpForce * m_surfaceUp;
    }

    public Vector3 WASDAcceleration(float acceleration)
    {
        Debug.DrawLine(transform.position, transform.position + (m_surfaceRight * 3.0f), Color.red);
        Debug.DrawLine(transform.position, transform.position + (m_surfaceUp * 3.0f), Color.green);
        Debug.DrawLine(transform.position, transform.position + (m_surfaceForward * 3.0f), Color.blue);

        Vector3 netForce = Vector3.zero;
        bool bMoved = false;

        // On the ground, move relative to the camera and the surface.
        if (Input.GetKey(KeyCode.W))
        {
            bMoved = true;
            netForce += m_surfaceForward * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            bMoved = true;
            netForce -= m_surfaceRight * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            bMoved = true;
            netForce -= m_surfaceForward * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            bMoved = true;
            netForce += m_surfaceRight * acceleration * Time.deltaTime;
        }

        // Cancel diagonal movement speed boost.
        if (bMoved)
            netForce = netForce.normalized * acceleration;

        return netForce;
    }

    public bool IsGrounded()
    {
        return m_controller.isGrounded;
    }

    public Vector3 Drag(float drag)
    {
        /*
        Vector3 velNor;

        if (m_velocity.magnitude > 1.0f)
            velNor = m_velocity.normalized;
        else // Special case where speed is less than one. Normalizing the velocity here would cause an increase in speed.
            velNor = m_velocity;

        return  -velNor * drag * Time.deltaTime;
        */

        float forwardComponent = Vector3.Dot(m_velocity, m_surfaceForward);
        float lateralComponent = Vector3.Dot(m_velocity, m_surfaceRight);

        Vector3 dragVector = (forwardComponent * m_surfaceForward) + (lateralComponent * m_surfaceRight);

        return -dragVector.normalized * drag * Time.deltaTime;
    }

    public Vector3 GetVelocity()
    {
        return m_velocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        m_velocity = velocity;
    }

    public void OverrideMovement(OverrideFunction function)
    {
        m_overrideFunction = function;
        m_overridden = true;
    }

    public void FreeOverride()
    {
        m_overridden = false;
    }

    Vector3 TestOverride(PlayerController controller)
    {
        return controller.GetVelocity();
    }

    public bool IsOverridden()
    {
        return m_overridden;
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
        m_surfaceUp = hit.normal;
        m_surfaceForward = Vector3.Cross(m_cameraTransform.right, m_surfaceUp);
        m_surfaceRight = Vector3.Cross(m_surfaceUp, m_surfaceForward);
    }

    void CalculateSurfaceTransform(RaycastHit hit)
    {
        m_surfaceUp = hit.normal;
        m_surfaceForward = Vector3.Cross(m_cameraTransform.right, m_surfaceUp);
        m_surfaceRight = Vector3.Cross(m_surfaceUp, m_surfaceForward);
    }

    /*
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Collider collider = hit.collider;

        if(collider != null && hit.point.y < transform.position.y) // Object must be the surface the player is standing on.
        {
            // Get surface-relative movement vectors.
            CalculateSurfaceTransform(hit);

            m_onGround = true;
        }
    }
    */

    void Update()
    {
        // ------------------------------------------------------------------------------------------------------
        // Mouse look

        m_lookEulerX -= Input.GetAxis("Mouse Y");
        m_lookEulerY += Input.GetAxis("Mouse X");

        m_lookEulerX = Mathf.Clamp(m_lookEulerX, -89.9f, 89.9f);

        Quaternion targetCamRotation = Quaternion.Euler(m_lookEulerX, m_lookEulerY, 0.0f);
        m_cameraTransform.rotation = Quaternion.Slerp(m_cameraTransform.rotation, targetCamRotation, 0.7f);

        // ------------------------------------------------------------------------------------------------------

        if(m_overridden)
        {
            m_velocity = m_overrideFunction(this);

            m_controller.Move(m_velocity * Time.deltaTime);

            // Set velocity to controller velocity output to apply any changes made by the controller physics.
            m_velocity = m_controller.velocity;

            return;
        }

        Vector3 m_netForce = Vector3.zero;

        // ------------------------------------------------------------------------------------------------------
        // Checking if grounded

        Ray sphereRay = new Ray(transform.position, -transform.up);
        RaycastHit hit = new RaycastHit();
        float sphereRadius = m_controller.radius * 0.5f;

        // Sphere case down to the nearest surface below. Sphere cast is ignored if not falling.
        if (m_velocity.y <= 0.0f || m_onGround)
        {
            bool sphereCastHit = Physics.SphereCast(sphereRay, sphereRadius, out hit);
            m_onGround = sphereCastHit && hit.distance < m_controller.height * 0.5f;

            // Sometimes the character controller does not detect collisions with the ground and update the surface transform...
            // So to be sure its updated we update it using the sphere case hit.
            if (sphereCastHit)
                CalculateSurfaceTransform(hit);

            Debug.DrawLine(sphereRay.origin, hit.point);
        }
        else
            m_onGround = false;

        // CharacterController.isGrounded is a very unreliable method to check if the character is grounded. So this is used as a backup method instead.
        m_onGround |= m_controller.isGrounded;

        // ------------------------------------------------------------------------------------------------------
        // Jumping

        if (Input.GetKeyDown(KeyCode.Space) && m_onGround)
        {
            Jump();
            m_onGround = false;
        }

        // ------------------------------------------------------------------------------------------------------
        // Movement

        if(m_onGround)
        {
            Vector3 outAcceleration = WASDAcceleration(m_groundAcceleration);
            m_netForce += outAcceleration;

            if(outAcceleration.sqrMagnitude < 0.01f) // Drag
            {
                m_netForce += Drag(m_groundDrag);
            }
        }
        else
        {
            Vector3 outAcceleration = WASDAcceleration(m_airAcceleration);
            m_netForce += outAcceleration;

            m_surfaceRight = m_cameraTransform.right;
            m_surfaceUp = transform.up;

            Vector3 forwardVec = m_cameraTransform.forward;
            forwardVec.y = 0.0f; // The Y component needs to be removed since it can mess with drag.
            forwardVec.Normalize();

            m_surfaceForward = forwardVec;

            m_netForce += Drag(m_airDrag);
        }

        // ------------------------------------------------------------------------------------------------------
        // Gravity

        // Apply gravity
        m_netForce += Physics.gravity * Time.deltaTime;

        // Apply net force.
        m_velocity += m_netForce;

        // Clamp velocity.
        if (m_onGround && m_velocity.sqrMagnitude > m_maxSpeed * m_maxSpeed)
            m_velocity = m_velocity.normalized * m_maxSpeed;
        else if (m_velocity.sqrMagnitude > m_terminalVelocity * m_terminalVelocity)
            m_velocity = m_velocity.normalized * m_terminalVelocity;

        m_controller.Move(m_velocity * Time.deltaTime);

        // Set velocity to controller velocity output to apply any changes made by the controller physics.
        m_velocity = m_controller.velocity;
    }

    //private void OnTriggerStay(Collider other)
    //{
    //    m_boxDetectedGround = other.transform.position.y < transform.position.y;
    //}
}
