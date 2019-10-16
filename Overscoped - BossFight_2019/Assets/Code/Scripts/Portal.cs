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

    [Tooltip("Animator component on the boss.")]
    [SerializeField]
    private Animator m_bossAnimator = null;

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
    private Material m_portalMat;
    private Collider m_armCollider;
    private Vector3 m_v3PunchDirection;
    private float m_fCurrentTime;
    private float m_fCurrentExitTime;

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

    /*
    Description: Force the wait between opening and the arm coming out to end.
    */
    public void SetArmEnterStage()
    {
        m_stage = ArmEnterStage;
        m_fCurrentTime = 0.0f;
    }

    /*
    Description: Set the current attack stage to arm exit.
    */
    public void SetArmExitStage()
    {
        m_fCurrentExitTime = 0.0f;
        m_stage = ArmExitStage;
    }

    /*
    Description: Set the current attack stage to the portal close stage.
    */
    public void SetPortalCloseStage()
    {
        m_fCurrentTime = 0.0f;
        m_stage = CloseStage;
    }

    /*
    Description: Get the amount of time the portal will take to open.
    Return Type: float
    */
    public float OpenTime()
    {
        return m_fOpenTime;
    }

    public void Activate()
    {
        m_stage = OpenStage;
        m_bActive = true;
        m_fCurrentTime = 0.0f;

        // Set arm rotation.
        m_arm.transform.rotation = Quaternion.LookRotation(m_v3PunchDirection, Vector3.up);
    }

    public void Deactivate()
    {
        // Reset stage.
        m_stage = OpenStage;

        // Deactivate portal and arm.
        gameObject.SetActive(false);
        m_arm.SetActive(false);
        m_bActive = false;

        m_fCurrentTime = 0.0f;
    }

    private void Awake()
    {
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_playerCollider);

        m_armCollider = m_arm.GetComponent<Collider>();

        Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_armCollider);
        m_rigidBody = GetComponent<Rigidbody>();

        m_portalMat = transform.GetChild(0).GetComponent<MeshRenderer>().material;

        // Set initial opacity.
        m_portalMat.SetFloat("_Opacity", 0.0f);

        m_stage = OpenStage;
        m_arm.SetActive(false);
        gameObject.SetActive(false);
        m_bActive = false;
    }

    /*
    Description: Open the portal. 
    */
    private void OpenStage()
    {
        m_fCurrentTime += Time.deltaTime;

        float fOpenProgress = Mathf.Min(m_fCurrentTime / m_fOpenTime, 1.0f);

        // Set arm rotation.
        m_arm.transform.rotation = Quaternion.LookRotation(m_v3PunchDirection, Vector3.up);

        m_portalMat.SetFloat("_Opacity", fOpenProgress);

        m_bossAnimator.SetBool("PortalPunchComplete", false);

        // Set arm material properties.
        m_armMaterials[0].SetVector("_PlaneOrigin", transform.position);
        m_armMaterials[1].SetVector("_PlaneOrigin", transform.position);

        m_armMaterials[0].SetVector("_PlaneNormal", -m_v3PunchDirection);
        m_armMaterials[1].SetVector("_PlaneNormal", -m_v3PunchDirection);
    }

    /*
    Description: Thrust arm out of the portal.
    */
    private void ArmEnterStage()
    {
        // Activate arm if disabled.
        if(!m_arm.activeInHierarchy)
        {
            m_arm.SetActive(true);
            m_armCollider.enabled = true;
        }

        m_fCurrentTime += Time.deltaTime;
        m_fCurrentTime = Mathf.Min(m_fCurrentTime, m_fArmEnterTime);

        float fArmOut = (m_fCurrentTime / m_fArmEnterTime) * m_fArmLength;
        fArmOut -= m_fArmLength;

        m_arm.transform.position = transform.position + (m_v3PunchDirection * fArmOut);
    }

    /*
    Description: Return arm into the portal.
    */
    private void ArmExitStage()
    {
        // Deactivate arm collider.
        if (m_armCollider.enabled)
            m_armCollider.enabled = false;

        m_fCurrentExitTime += Time.deltaTime;
        m_fCurrentExitTime = Mathf.Min(m_fCurrentExitTime, m_fArmExitTime);

        float fArmOut = (1.0f - (m_fCurrentExitTime / m_fArmExitTime)) * m_fArmLength;
        fArmOut -= m_fArmLength;

        m_arm.transform.position = transform.position + (m_v3PunchDirection * fArmOut);
    }

    /*
    Description: Close the portal.
    */
    private void CloseStage()
    {
        // Continue arm exit stage.
        ArmExitStage();

        m_fCurrentTime += Time.deltaTime;

        float fCloseProgress = Mathf.Min(m_fCurrentTime / m_fCloseTime, 1.0f);

        m_portalMat.SetFloat("_Opacity", 1.0f - fCloseProgress);

        if (m_fCurrentTime >= m_fCloseTime)
        {
            Deactivate();
        }
    }

    private void Update()
    {
        // Ensure the portal does not move.
        m_rigidBody.velocity = Vector3.zero;

        m_stage();
    }
}
