using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CameraSplineState
{
    public Vector4 m_v4Position;
    public Quaternion m_rotation;
}

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
    private List<CameraSplineState> m_camSpline;
    private CameraSplineState m_currentSplineState;
    private CameraSplineState m_nextSplineState;
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
    private float m_fSplineInterp;
    private int m_nSplineIndex;
    private const int m_nSplineSampleCount = 3;
    private const int m_nMaxSplinePoints = 256;
    private bool m_bFullMag;

    void Awake()
    {
        m_camera = GetComponent<Camera>();

        m_camSpline = new List<CameraSplineState>(m_nMaxSplinePoints);

        m_v3StartPosition = transform.localPosition;
        m_startCamRot = transform.localRotation;
        m_fStartFOV = m_camera.fieldOfView;

        m_fShakeDuration = 0.0f;
        m_fShakeMagnitude = 0.0f;
    }

    void Update()
    {
        //for (int i = 0; i < m_camSpline.Count - 1; ++i)
        //{
        //    Debug.DrawLine(m_camSpline[i].m_v4Position, m_camSpline[i + 1].m_v4Position, Color.magenta);
        //}

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

    /*
    Description: Record current camera state into the spline.
    Param:
    */
    public void RecordCameraState()
    {
        if(m_camSpline.Count >= m_nMaxSplinePoints)
        {
            // Max amount of points has been reached, points will will be discarded from the spline.

            // Remove old point.
            m_camSpline.RemoveAt(0);
        }

        CameraSplineState state;
        state.m_v4Position = transform.position;
        state.m_rotation = transform.rotation;

        m_camSpline.Add(state);
    }

    /*
    Description: Get the maximum amount of points on the spline.
    Return Type: int
    */
    public int MaxSplineCount()
    {
        return m_nMaxSplinePoints;
    }

    /*
    Description: Prepare camera backtracking spline for reading.
    */
    public void StartCamSpline()
    {
        m_fSplineInterp = 0.0f;
        m_nSplineIndex = m_camSpline.Count - 2;

        m_currentSplineState = m_camSpline[Mathf.Max(m_camSpline.Count - 1, 0)];
        m_nextSplineState = m_camSpline[Mathf.Max(m_camSpline.Count - 2, 0)];
    }

    /*
    Description: Clear the camera backtracking spline.
    */
    public void ClearCamSpline()
    {
        m_camSpline.Clear();
    }

    /*
    Description: Get the current camera state from the spline, and advance using the provided rate.
    Return Type: CameraSplineState
    Param:
        float fRate: The rate in which the spline will advance.
    */
    
    public CameraSplineState EvaluateCamSpline(float fProgress)
    {
        float fIndexProgress = (1.0f - fProgress) * m_camSpline.Count;
        int nSplineIndex = Mathf.Min(Mathf.FloorToInt(fIndexProgress), m_camSpline.Count - 1);

        float fInterp = 1.0f - (fIndexProgress - nSplineIndex);

        CameraSplineState currentSplineState = m_camSpline[nSplineIndex];
        CameraSplineState nextSplineState = m_camSpline[Mathf.Max(nSplineIndex - 1, 0)];

        CameraSplineState state;
        state.m_v4Position = Vector4.Lerp(currentSplineState.m_v4Position, nextSplineState.m_v4Position, fInterp);
        state.m_rotation = Quaternion.Slerp(currentSplineState.m_rotation, nextSplineState.m_rotation, fInterp);

        return state;
    }
    
    /*
    public CameraSplineState EvaluateCamSpline(float fRate)
    {
        for (int i = 0; i < m_camSpline.Count - 1; ++i)
        {
            Debug.DrawLine(m_camSpline[i].m_v4Position, m_camSpline[i + 1].m_v4Position, Color.magenta);
        }

        CameraSplineState state;
        state.m_v4Position = Vector4.Lerp(m_currentSplineState.m_v4Position, m_nextSplineState.m_v4Position, m_fSplineInterp);
        state.m_rotation = Quaternion.Slerp(m_currentSplineState.m_rotation, m_nextSplineState.m_rotation, m_fSplineInterp);

        m_fSplineInterp += fRate;

        // Advance points if the interp reaches 1.
        if (m_fSplineInterp >= 1.0f)
        {
            m_fSplineInterp = 0.0f;

            // Decrement spline index and don't alow it to decrement below zero.
            if (--m_nSplineIndex < 0)
                m_nSplineIndex = 0;

            m_currentSplineState = m_nextSplineState;
            m_nextSplineState = m_camSpline[m_nSplineIndex];
        }

        return state;
    }
    */
}
