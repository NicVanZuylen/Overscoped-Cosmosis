using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Controls basic behaviour of objects pullable out of place using the player's pull ability.
 * Author: Nic Van Zuylen
*/

[RequireComponent(typeof(Rigidbody))]

public class PullObject : MonoBehaviour
{
    // Private: 

    [SerializeField]
    private Transform m_decoupleVector = null;

    [Tooltip("Force added when the object is decoupled.")]
    [SerializeField]
    private float m_fDecoupleForce = 5.0f;

    [Tooltip("Amount of degrees randomly added to rotation whilst shaking.")]
    [SerializeField]
    private float m_fShakeMagnitude = 4.5f;

    [Tooltip("Amount of time before a new random shake offset is applied to rotation.")]
    [SerializeField]
    private float m_fShakeTime = 0.05f;

    [Tooltip("Rate in which rotation will be updated from the shake effect.")]
    [SerializeField]
    private float m_fShakeSpeed = 10.0f;

    private Rigidbody m_rigidbody;
    private Vector3 m_v3StartEuler;
    private Vector3 m_v3CurrentEuler;
    private Vector3 m_v3TargetEuler;
    private float m_fTension;
    private float m_fCurrentShakeTime;
    private bool m_bTriggered;

    protected void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_rigidbody.isKinematic = true;
        m_v3StartEuler = transform.localRotation.eulerAngles;
        m_bTriggered = true;
    }

    protected void Update()
    {
        if (!m_bTriggered)
        {
            return;
        }

        m_fCurrentShakeTime -= Time.deltaTime;

        if(m_fTension > 0.0f && m_fCurrentShakeTime <= 0.0f)
        {
            // Apply random euler rotation offset.
            float fRandRotationOffset = m_fShakeMagnitude * m_fTension;

            Vector3 v3RandomEulerOffset = Vector3.zero;
            v3RandomEulerOffset.x = Random.Range(-fRandRotationOffset, fRandRotationOffset);
            v3RandomEulerOffset.y = Random.Range(-fRandRotationOffset, fRandRotationOffset);
            v3RandomEulerOffset.z = Random.Range(-fRandRotationOffset, fRandRotationOffset);

            m_v3TargetEuler = m_v3StartEuler + v3RandomEulerOffset;

            // Reset shake timer.
            m_fCurrentShakeTime = m_fShakeTime;
        }

        if(m_fTension > 0.0f)
        {
            m_v3CurrentEuler = Vector3.MoveTowards(m_v3CurrentEuler, m_v3TargetEuler, m_fShakeMagnitude * m_fShakeSpeed * Time.deltaTime);

            transform.localRotation = Quaternion.Euler(m_v3CurrentEuler);
        }
    }

    public bool IsTriggered()
    {
        return m_bTriggered;
    }

    public void SetTension(float fTension)
    {
        m_fTension = fTension;

        // Reset rotation if tension is set to zero.
        if (m_fTension <= 0.0f)
            transform.localRotation = Quaternion.Euler(m_v3StartEuler);
    }

    public virtual void Trigger(Vector3 playerDirection)
    {
        m_rigidbody.isKinematic = false;
        m_bTriggered = false;
        transform.parent = null;
        m_fTension = 0.0f;

        if(m_decoupleVector != null)
            m_rigidbody.AddForce((m_decoupleVector.forward + playerDirection) * m_fDecoupleForce, ForceMode.Impulse);
        else
            m_rigidbody.AddForce(playerDirection * m_fDecoupleForce, ForceMode.Impulse);

        // Disable.
        SetTension(0.0f);
        enabled = false;
    }

    public void LetGo()
    {
        // Reset tension & disable the script.
        SetTension(0.0f);
        enabled = false;
    }
}
