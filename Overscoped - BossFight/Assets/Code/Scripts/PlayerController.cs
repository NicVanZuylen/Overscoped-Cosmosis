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

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Jump()
    {
        m_velocity += m_jumpForce * transform.up;
    }

    public Vector3 WASDAcceleration(float acceleration)
    {
        Vector3 netForce = Vector3.zero;
        bool bMoved = false;

        if (Input.GetKey(KeyCode.W))
        {
            bMoved = true;
            netForce += m_cameraTransform.forward * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            bMoved = true;
            netForce -= m_cameraTransform.right * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            bMoved = true;
            netForce -= m_cameraTransform.forward * acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            bMoved = true;
            netForce += m_cameraTransform.right * acceleration * Time.deltaTime;
        }

        netForce.y = 0.0f;

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
        Vector3 velNor;

        if (m_velocity.magnitude > 1.0f)
            velNor = m_velocity.normalized;
        else // Special case where speed is less than one. Normalizing the velocity here would cause an increase in speed.
            velNor = m_velocity;

        velNor.y = 0.0f;

        return  -velNor * drag * Time.deltaTime;
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
        // Jumping
        bool onGround = m_controller.isGrounded;

        if (Input.GetKeyDown(KeyCode.Space) && onGround)
        {
            Jump();
            onGround = false;
        }

        // ------------------------------------------------------------------------------------------------------
        // Movement

        if(onGround)
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

            m_netForce += Drag(m_airDrag);
        }

        // ------------------------------------------------------------------------------------------------------
        // Gravity

        // Apply gravity
        m_netForce += Physics.gravity * Time.deltaTime;

        // Apply net force.
        m_velocity += m_netForce;

        // Clamp velocity.
        if (onGround && m_velocity.sqrMagnitude > m_maxSpeed * m_maxSpeed)
            m_velocity = m_velocity.normalized * m_maxSpeed;
        else if (m_velocity.sqrMagnitude > m_terminalVelocity * m_terminalVelocity)
            m_velocity = m_velocity.normalized * m_terminalVelocity;

        m_controller.Move(m_velocity * Time.deltaTime);

        // Set velocity to controller velocity output to apply any changes made by the controller physics.
        m_velocity = m_controller.velocity;
    }
}
