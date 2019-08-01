using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Tooltip("Lerp rate of FOV changes.")]
    [SerializeField]
    private float m_fFOVLerpRate = 0.1f;

    private Camera m_camera;
    private Vector3 m_v3StartPosition;
    private Vector3 m_v3StartCamEuler;
    private float m_fShakeDuration;
    private float m_fShakeMagnitude;
    private float m_fStartFOV;
    private float m_fFOVOffset;
    private bool m_bFullMag;

    void Awake()
    {
        m_camera = GetComponent<Camera>();
        m_v3StartPosition = transform.localPosition;
        m_v3StartCamEuler = transform.localRotation.eulerAngles;
        m_fStartFOV = m_camera.fieldOfView;

        m_fShakeDuration = 0.0f;
        m_fShakeMagnitude = 0.0f;
    }

    void Update()
    {
        m_v3StartCamEuler = transform.localRotation.eulerAngles;

        if (m_fShakeDuration > 0.0f)
        {
            // Apply shake for this frame.
            Shake(m_fShakeMagnitude, m_bFullMag);

            m_fShakeDuration -= Time.deltaTime;
        }

        // Apply FOV lerp offset.
        m_camera.fieldOfView = Mathf.Lerp(m_camera.fieldOfView, m_fStartFOV + m_fFOVOffset, m_fFOVLerpRate);

        m_fFOVOffset = 0.0f;
    }

    /*
    Description: Apply a shake effect to the camera for a given amount of time.
    Param:
        float fTime: The amount of time to shake.
        float fMagnitude: The magnitude of the shake vector to add.
        bool bFullMag: Whether or not to force full shake vector magnitude.
    */
    public void ApplyShakeOverTime(float fTime, float fMagnitude, bool bFullMag = false)
    {
        m_fShakeDuration = Mathf.Max(m_fShakeDuration, fTime);
        m_fShakeMagnitude = fMagnitude;
        m_bFullMag = bFullMag;
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
        Vector3 camEuler = Vector3.zero;

        if (!bFullmag)
        {
            camEuler = new Vector3
            (
                Random.Range(-fMagnitude, fMagnitude),
                Random.Range(-fMagnitude, fMagnitude),
                0.0f
            );
        }
        else
        {
            camEuler = new Vector3
            (
                Random.Range(-1, 2) * fMagnitude,
                Random.Range(-1, 2) * fMagnitude,
                0.0f
            );
        }

        transform.localRotation = Quaternion.Euler(m_v3StartCamEuler + camEuler);
    }
}
