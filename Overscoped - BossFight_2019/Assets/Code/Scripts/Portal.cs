using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    // Private:

    [Tooltip("The player's collider.")]
    [SerializeField]
    private CharacterController m_playerCollider = null;

    [Tooltip("Arm object to move through the portal.")]
    [SerializeField]
    private GameObject m_arm = null;

    [Tooltip("Arm materials")]
    [SerializeField]
    private Material[] m_armMaterials = null;

    [Tooltip("Length/Distance in which the arm will exit the portal")]
    [SerializeField]
    private float m_fArmLength = 10.0f;

    [Header("Stages")]
    [Tooltip("Amount of time the portal will be considered to be opening.")]
    [SerializeField]
    private float m_fOpenTime = 1.0f;

    [Tooltip("Amount of time for the arm to enter and leave.")]
    [SerializeField]
    private float m_fArmEnterTime = 0.2f;

    [Tooltip("Amount of time for the arm to enter and leave.")]
    [SerializeField]
    private float m_fArmExitTime = 1.0f;

    [Tooltip("The amount of time for the portal to close.")]
    [SerializeField]
    private float m_fCloseTime = 1.0f;

    delegate void StageFunc();

    StageFunc m_stage;
    private bool m_bActive;

    private Rigidbody m_rigidBody;
    private Transform m_visualTransform; // Transform of the visible part of the portal.
    private Vector3 m_v3PunchDirection;
    private float m_fCurrentTime;
    public float m_fCollisionFreeTime;

    /*
    Description: Set the direction the fist will punch in. 
    Param:
       Vector3 v3PunchDir: The direction the fist will punch in.
    */
    public void SetPunchDirection(Vector3 v3PunchDir)
    {
        m_v3PunchDirection = v3PunchDir;
    }

    /*
    Description: Get the direction the fist will punch in. 
    Return Type: Vector3
    */
    public Vector3 GetPunchDirection()
    {
        return m_v3PunchDirection;
    }

    /*
    Description: Get whether or not the portal is active.
    Return Type: bool
    */
    public bool IsActive()
    {
        return m_bActive;
    }

    private void Awake()
    {
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_playerCollider);
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_arm.GetComponent<CapsuleCollider>());
        m_rigidBody = GetComponent<Rigidbody>();

        m_visualTransform = transform.GetChild(0);
        m_stage = OpenStage;
        m_bActive = false;
    }

    private void OnEnable()
    {
        m_stage = OpenStage;
        m_bActive = true;
        m_fCurrentTime = m_fOpenTime;
    }

    private void OnDisable()
    {
        m_bActive = false;
        m_fCurrentTime = 0.0f;
    }

    /*
    Description: Open the portal. 
    */
    private void OpenStage()
    {
        // Don't allow progression when collisions are still being resolved.
        if(m_fCollisionFreeTime >= 0.5f)
            m_fCurrentTime -= Time.deltaTime;

        float fScaleProgress = (1.0f - (m_fCurrentTime / m_fCloseTime));

        m_visualTransform.localScale = new Vector3(fScaleProgress, fScaleProgress, 1.0f); // Scale portal opening.

        if(m_fCurrentTime <= 0.0f)
        {
            m_arm.SetActive(true);
            m_arm.transform.rotation = (Quaternion.LookRotation(m_v3PunchDirection, Vector3.up) * Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            // Set arm material properties.
            m_armMaterials[0].SetVector("_PlaneOrigin", transform.position);
            m_armMaterials[1].SetVector("_PlaneOrigin", transform.position);

            m_armMaterials[0].SetVector("_PlaneNormal", -m_v3PunchDirection);
            m_armMaterials[1].SetVector("_PlaneNormal", -m_v3PunchDirection);

            // Next stage.
            m_stage = ArmEnterStage;
            m_fCurrentTime = m_fArmEnterTime;
        }
    }

    /*
    Description: Thrust arm out of the portal.
    */
    private void ArmEnterStage()
    {
        m_fCurrentTime -= Time.deltaTime;

        float fArmOut = (1.0f - (m_fCurrentTime / m_fArmEnterTime)) * m_fArmLength;
        //fArmOut -= m_fArmLength;

        m_arm.transform.position = transform.position + (m_v3PunchDirection * fArmOut);

        if(m_fCurrentTime <= 0.0f)
        {
            // Next stage.
            m_stage = ArmExitStage;
            m_fCurrentTime = m_fArmExitTime;
        }
    }

    /*
    Description: Return arm into the portal.
    */
    private void ArmExitStage()
    {
        m_fCurrentTime -= Time.deltaTime;

        float fArmOut = (m_fCurrentTime / m_fArmExitTime) * m_fArmLength;
        //fArmOut -= m_fArmLength;

        m_arm.transform.position = transform.position + (m_v3PunchDirection * fArmOut);

        if (m_fCurrentTime <= 0.0f)
        {
            m_arm.SetActive(false);

            // Next stage.
            m_stage = CloseStage;
            m_fCurrentTime = m_fCloseTime;
        }
    }

    /*
    Description: Close the portal.
    */
    private void CloseStage()
    {
        m_fCurrentTime -= Time.deltaTime;

        float fScaleProgress = (m_fCurrentTime / m_fCloseTime);

        m_visualTransform.localScale = new Vector3(fScaleProgress, fScaleProgress, 1.0f); // Scale portal closure.

        if (m_fCurrentTime <= 0.0f)
        {
            // Reset.
            m_stage = OpenStage;
            m_bActive = false;

            gameObject.SetActive(false);

            m_fCurrentTime = 0.0f;
        }
    }

    private void Update()
    {
        // Increase time between collisions.
        m_fCollisionFreeTime += Time.deltaTime;

        // Ensure the portal does not move.
        m_rigidBody.velocity = Vector3.zero;

        m_stage();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Reset open time, scale and collision time.
        m_fCurrentTime = m_fOpenTime;
        m_visualTransform.localScale = Vector3.zero;
        m_fCollisionFreeTime = 0.0f;
    }
}
