using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Tooltip("Lerp rate of FOV changes.")]
    [SerializeField]
    private float m_fFOVLerpRate = 0.1f;

    [Tooltip("Rate in which new shake offsets will be applied.")]
    [SerializeField]
    private float m_fShakeDelay = 0.03f;

    [Tooltip("Amount of time for the shake offset to fade.")]
    [SerializeField]
    private float m_fShakeReturnDuration = 0.1f;

    private Camera m_camera;
    private Vector3 m_v3StartPosition;
    private Quaternion m_currentRotOffset;
    private Quaternion m_startCamRot;
    private Quaternion m_shakeRot;
    private float m_fShakeDuration;
    private float m_fShakeReturnTime;
    private float m_fCurrentShakeDelay;
    private float m_fShakeMagnitude;
    private float m_fStartFOV;
    private float m_fFOVOffset;
    private bool m_bFullMag;

    void Awake()
    {
        m_camera = GetComponent<Camera>();
        m_v3StartPosition = transform.localPosition;
        m_startCamRot = transform.localRotation;
        m_fStartFOV = m_camera.fieldOfView;

        m_fShakeDuration = 0.0f;
        m_fShakeMagnitude = 0.0f;
    }

    void Update()
    {
        m_startCamRot = transform.localRotation;

        // Apply shake.
        Shake(m_fShakeMagnitude, m_bFullMag);

        // Apply FOV lerp offset.
        m_camera.fieldOfView = Mathf.Lerp(m_camera.fieldOfView, m_fStartFOV + m_fFOVOffset, m_fFOVLerpRate);

        m_fFOVOffset = 0.0f;
    }

    /*
    Description: Apply a shake effect to the camera for a given amount of time. Lesser shakes than the current shake magnitude will be absorbed.
    Param:
        float fTime: The amount of time to shake.
        float fMagnitude: The magnitude of the shake vector to add.
        bool bFullMag: Whether or not to force full shake vector magnitude.
    */
    public void ApplyShake(float fTime, float fMagnitude, bool bFullMag = false)
    {
        if(m_fShakeDuration <= 0.0f || m_fShakeMagnitude < fMagnitude)
        {
            m_fShakeMagnitude = fMagnitude;
            m_fShakeDuration = fTime;
            m_bFullMag = bFullMag;
        }
    }

    /*
    Description: Set the FOV offset of the camera. The current FOV will lerp to the start FOV + offset.
    Param:
        float fOffset: The FOV offset to use. This will be reset after application.
    */
    public void SetFOVOffset(float fOffset)
    {
        m_fFOVOffset = Mathf.Max(m_fFOVOffset, fOffset);
    }

    /*
    Description: Apply a shake vector to the camera for this frame.
    Param:
        float fMagnitude: The magnitude of the shake vector to add.
        bool bFullMag: Whether or not to force full shake vector magnitude.
    */
    private void Shake(float fMagnitude, bool bFullmag)
    {
        if(m_fShakeDuration > 0.0f && m_fCurrentShakeDelay <= 0.0f)
        {
            Vector3 camEuler = Vector3.zero;

            if (!bFullmag)
            {
                camEuler.x = Random.Range(-fMagnitude, fMagnitude);
                camEuler.y = Random.Range(-fMagnitude, fMagnitude);
            }
            else
            {
                camEuler.x = Random.Range(-1, 2) * fMagnitude;
                camEuler.y = Random.Range(-1, 2) * fMagnitude;
            }

            m_shakeRot = Quaternion.Euler(camEuler);

            m_fShakeReturnTime = m_fShakeReturnDuration;
            m_fCurrentShakeDelay = m_fShakeDelay;
        }

        m_currentRotOffset = Quaternion.Slerp(m_shakeRot, Quaternion.Euler(Vector3.zero), 1.0f - (m_fShakeReturnTime / m_fShakeReturnDuration));

        m_fShakeReturnTime -= Time.deltaTime;
        m_fShakeDuration -= Time.deltaTime;
        m_fCurrentShakeDelay -= Time.deltaTime;
    }

    /*
    Description: Get the shake offset euler angles. (To apply to camera rotation.) 
    Return Type: Vector3
    */
    public Vector3 ShakeEuler()
    {
        return m_currentRotOffset.eulerAngles;
    }
}
