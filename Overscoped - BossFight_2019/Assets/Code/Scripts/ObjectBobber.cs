using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectBobber : MonoBehaviour
{
    [Tooltip("Maximum height above & below the original position the object will bob.")]
    [SerializeField]
    private float m_fBobbingHeight = 5.0f;

    [Tooltip("Amount of complete bobs (once up, & once down) per second the object will do.")]
    [SerializeField]
    private float m_fBobFrequency = 1.0f;

    [Tooltip("Whether or not bobbing can affect the object's rotation")]
    [SerializeField]
    private bool m_bEnableAngularBobbing = true;

    [Tooltip("Maximum angle bobbing will add to the original rotation on the X axis.")]
    [SerializeField]
    private float m_fXBobbingAngle = 5.0f;

    [Tooltip("Maximum angle bobbing will add to the original rotation on the Z axis.")]
    [SerializeField]
    private float m_fZBobbingAngle = 5.0f;

    private Vector3 m_v3StartPos;
    private Vector3 m_v3StartEuler;
    private Vector3 m_v3BobbingEulerOffset;

    private void Awake()
    {
        m_v3StartPos = transform.position;
        m_v3StartEuler = transform.rotation.eulerAngles;
    }

    private void Update()
    {
        // Get sine value from game time.
        float fSinVal = Mathf.Sin(Time.timeSinceLevelLoad * m_fBobFrequency * 2.0f * Mathf.PI);
        float fAngleSinVal = Mathf.Sin(Time.timeSinceLevelLoad * m_fBobFrequency * Mathf.PI);

        // Set bobbed position & rotation.
        transform.position = m_v3StartPos + (Vector3.up * fSinVal * m_fBobbingHeight);

        if(m_bEnableAngularBobbing)
        {
            // Set bobbing euler angle offset.
            m_v3BobbingEulerOffset.x = fAngleSinVal * m_fXBobbingAngle;
            m_v3BobbingEulerOffset.y = 0.0f;
            m_v3BobbingEulerOffset.z = fAngleSinVal * m_fZBobbingAngle;

            // Modify rotation...
            transform.rotation = Quaternion.Euler(m_v3StartEuler + m_v3BobbingEulerOffset);
        }
    }
}
