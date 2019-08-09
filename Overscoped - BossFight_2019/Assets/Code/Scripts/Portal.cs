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
    private GameObject m_arm;

    [Header("Stages")]
    [Tooltip("Amount of time the portal will be considered to be opening.")]
    [SerializeField]
    private float m_fOpenTime = 1.0f;

    [Tooltip("Amount of time for the arm to enter and leave.")]
    [SerializeField]
    private float m_fArmTime = 3.0f;

    [Tooltip("The amount of time for the portal to close.")]
    [SerializeField]
    private float m_fCloseTime = 1.0f;

    delegate void StageFunc();

    StageFunc m_stage;
    private bool m_bActive;

    private Rigidbody m_rigidBody;
    private Transform m_visualTransform; // Transform of the visible part of the portal.
    private float m_fCurrentTime;
    public float m_fCollisionFreeTime;

    private void Awake()
    {
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), m_playerCollider);
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

    private void OpenStage()
    {
        // Don't allow progression when collisions are still being resolved.
        if(m_fCollisionFreeTime >= 0.5f)
            m_fCurrentTime -= Time.deltaTime;

        float fScaleProgress = (1.0f - (m_fCurrentTime / m_fCloseTime));

        m_visualTransform.localScale = new Vector3(fScaleProgress, fScaleProgress, 1.0f); // Scale portal opening.

        if(m_fCurrentTime <= 0.0f)
        {
            // Next stage.
            m_stage = ArmStage;
            m_fCurrentTime = m_fArmTime;
        }
    }

    private void ArmStage()
    {
        m_fCurrentTime -= Time.deltaTime;

        if(m_fCurrentTime <= 0.0f)
        {
            // Next stage.
            m_stage = CloseStage;
            m_fCurrentTime = m_fCloseTime;
        }
    }

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

    public bool IsActive()
    {
        return m_bActive;
    }
}
