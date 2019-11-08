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

    [Header("Stages")]

    [Tooltip("SFX played when the portal opens.")]
    [SerializeField]
    private AudioClip m_openSFX = null;

    [Tooltip("SFX played when the portal closes.")]
    [SerializeField]
    private AudioClip m_closeSFX = null;

    [Tooltip("Impact sound of the boss fist.")]
    [SerializeField]
    private AudioSelection m_punchImpactSFX = new AudioSelection();

    delegate void StageFunc();

    StageFunc m_stage;
    private bool m_bActive;

    private Rigidbody m_rigidBody;
    private Material m_portalMat;
    private Collider m_armCollider;
    private Vector3 m_v3PunchDirection;
    private AudioSource m_audioSource;
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
    Description: Get the length of the arm exiting the portal.
    Return Type: float
    */
    public float GetArmLength()
    {
        return m_fArmLength;
    }

    /*
    Description: Force the wait between opening and the arm coming out to end.
    */
    public void SetArmEnterStage()
    {
        // Do nothing if stage is already set.
        if (m_stage == ArmEnterStage)
            return;

        // Reset SFX cooldown.
        m_punchImpactSFX.SetCooldown(0.0f);

        m_stage = ArmEnterStage;
        m_fCurrentTime = 0.0f;
    }

    /*
    Description: Set the current attack stage to arm exit.
    */
    public void SetArmExitStage()
    {
        // Do nothing if stage is already set.
        if (m_stage == ArmExitStage)
            return;

        m_fCurrentExitTime = 0.0f;
        m_stage = ArmExitStage;

        // Play SFX.
        m_audioSource.PlayOneShot(m_closeSFX, BossBehaviour.GetVolume());
    }

    /*
    Description: Set the current attack stage to the portal close stage.
    */
    public void SetPortalCloseStage()
    {
        // Do nothing if stage is already set.
        if (m_stage == CloseStage)
            return;

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

        // Play open effect.
        m_audioSource.PlayOneShot(m_openSFX, BossBehaviour.GetVolume());

        m_bossAnimator.SetBool("PortalPunchComplete", false);


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

        m_audioSource = GetComponent<AudioSource>();

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

        // Set arm material properties.
        for(int i = 0; i < m_armMaterials.Length; ++i)
        {
            m_armMaterials[i].SetVector("_PlaneOrigin", transform.position);
            m_armMaterials[i].SetVector("_PlaneNormal", -m_v3PunchDirection);
        }
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

        // Arm extension math...
        float fArmNorm = m_fCurrentTime / m_fArmEnterTime;
        float fArmOut = fArmNorm * m_fArmLength;
        fArmOut -= m_fArmLength;

        m_arm.transform.position = transform.position + (m_v3PunchDirection * fArmOut);

        // Play impact SFX once fully extended.
        if(fArmNorm >= 1.0f)
        {
            m_punchImpactSFX.PlayIndex(0, BossBehaviour.GetVolume());
        }
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
