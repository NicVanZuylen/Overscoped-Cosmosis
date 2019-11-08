using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

/*
 * Description: Handles all camera related effects. Such as shake, head bobbing, FOV adjustments, rewind etc.
 * Author: Nic Van Zuylen
*/

public struct CameraSplineState
{
    public Vector4 m_v4Position;
    public Quaternion m_rotation;
}

public class CameraEffects : MonoBehaviour
{
    [Tooltip("HDRP post processing volume.")]
    [SerializeField]
    private Volume m_postProcessing = null;

    [Tooltip("Lerp rate of FOV changes.")]
    [SerializeField]
    private float m_fFOVChangeRate = 0.1f;

    [Tooltip("Rate in which new shake offsets will be applied.")]
    [SerializeField]
    private float m_fShakeDelay = 0.03f;

    [Tooltip("Rate in which new chromatic abberation shake values will be applied.")]
    [SerializeField]
    private float m_fChromAbbShakeDelay = 0.03f;

    [Tooltip("Amount of time for the shake offset to fade.")]
    [SerializeField]
    private float m_fShakeReturnDuration = 0.1f;

    [Tooltip("Rate of head bobbing related movement")]
    [SerializeField]
    private float m_fBobbingSpeed = 7.0f;

    [Tooltip("Magnitude of vertical angle head bobbing")]
    [SerializeField]
    private float m_fBobbingXAngleMagnitude = 0.8f;

    [Tooltip("Magnitude of horizontal angle head bobbing")]
    [SerializeField]
    private float m_fBobbingYAngleMagnitude = 0.5f;

    [Tooltip("Magnitude of vertical offset head bobbing")]
    [SerializeField]
    private float m_fBobbingPosMagnitude = 0.07f;

    [Tooltip("Screen fade effect.")]
    [SerializeField]
    private ScreenFade m_screenFade = null;

    private Camera m_camera;
    private List<CameraSplineState> m_camSpline;
    private Quaternion m_startCamRot;
    private ClampedFloatParameter m_chromAbb;

    // Head bobbing.
    private Vector3 m_v3BobbingEuler; // Head bobbing camera angle offset.
    private Vector3 m_v3BobbingOffset; // Head bobbing camera position offset.
    private float m_fVertBobbingLevel; // Range between 0 and 1.
    private float m_fSideBobbingLevel; // Range between -1 and 1.
    private float m_fLandingBobbingLevel; // Range between 0 and 1.
    private int m_nBobbingDirection; // 1 or -1.
    private int m_nBobbingSideDirection; // 1 or -1.
    private int m_nLandingBobDirection; // 1 or -1.
    private bool m_bBobbing; // Whether or not to enable head bobbing from walking or running.

    // Chromatic Abberation shake
    private float m_fChromAbbShakeDuration; // Remaining time on chrom abb shake.
    private float m_fChromAbbMinMagnitude; // Minimum chrom abb shake offset.
    private float m_fChromAbbMaxMagnitude; // Maximum chrom abb shake offset.
    private float m_fCurrentCAShakeDelay; // Delay between new chrom abb shake offsets
    private float m_fChromAbbTarget; // Chrom abb offset target.

    // Screen shake.
    private Vector3 m_v3StartPosition; // Initial camera offset.
    private Quaternion m_currentRotOffset; // Current shake rotation offset.
    private Quaternion m_shakeRot; // Camera rotation with screen shake.
    private float m_fShakeDuration; // Remaining time of camera shake.
    private float m_fShakeReturnTime;
    private float m_fCurrentShakeDelay; // Time between new shake offsets.
    private float m_fShakeMagnitude; // Maximum magnitude of shake offsets.
    private bool m_bFullMag; // Whether or not to use the full shake magnitude always when shaking.

    // FOV
    private float m_fStartFOV; // Initial camera FOV.
    private float m_fFOVOffset; // Offset on camera FOV.

    // Rewind
    private CameraSplineState m_currentSplineState; // Current rewind spline point used for interpolation.
    private CameraSplineState m_nextSplineState; // Next rewind spline point used for interpolation.
    private const int m_nSplineSampleCount = 3; 
    private const int m_nMaxSplinePoints = 512; // Maximum amount of points on the camera rewind spline.
    private int m_nSplineIndex; // Index of the current spline point in the spline array.

    void Awake()
    {
        m_camera = GetComponent<Camera>();

        // Get player controller and add landing callback.
        PlayerController controller = transform.parent.GetComponentInParent<PlayerController>();
        controller.AddLandCallback(Land);

        // Create spline data array.
        m_camSpline = new List<CameraSplineState>(m_nMaxSplinePoints);

        // Get start transform data & FOV.
        m_v3StartPosition = transform.localPosition;
        m_startCamRot = transform.localRotation;
        m_fStartFOV = m_camera.fieldOfView;

        // Get chromatic aberration intensity value reference.
        ChromaticAberration chromAbbValue = null;
        m_postProcessing.profile.TryGet<ChromaticAberration>(out chromAbbValue);
        m_chromAbb = chromAbbValue.intensity;

        // Initial shake duration and magnitude values.
        m_fShakeDuration = 0.0f;
        m_fShakeMagnitude = 0.0f;

        // Initial head bobbing direction values.
        m_nBobbingDirection = -1;
        m_nBobbingSideDirection = -1;
        m_nLandingBobDirection = -1;
    }

    void Update()
    {
        m_startCamRot = transform.localRotation;

        // Apply shake & chromatic aberration.
        Shake(m_fShakeMagnitude, m_bFullMag);
        ShakeChromAbb();

        // Apply FOV offset over time.
        m_camera.fieldOfView = Mathf.MoveTowards(m_camera.fieldOfView, m_fStartFOV + m_fFOVOffset, m_fFOVChangeRate * Time.deltaTime);
        m_fFOVOffset = 0.0f;

        // Update head bobbing effect.
        UpdateBobbing();
    }

    public void UpdateBobbing()
    {
        m_fVertBobbingLevel += Time.deltaTime * m_fBobbingSpeed * m_nBobbingDirection;
        m_fVertBobbingLevel = Mathf.Clamp(m_fVertBobbingLevel, 0.0f, 1.0f);

        m_fLandingBobbingLevel += Time.deltaTime * 10.0f * m_nLandingBobDirection;
        m_fLandingBobbingLevel = Mathf.Clamp(m_fLandingBobbingLevel, 0.0f, 1.0f);

        if (!m_bBobbing) // Bobbing not enabled.
        {
            // Lower bobbing levels towards zero.
            m_nBobbingDirection = -1;
            m_fSideBobbingLevel = Mathf.MoveTowards(m_fSideBobbingLevel, 0.0f, m_fBobbingSpeed * Time.deltaTime);
        }
        else // Bobbing enabled.
        {
            // Progress side bobbing level.
            m_fSideBobbingLevel += Time.deltaTime * m_fBobbingSpeed * m_nBobbingDirection;
            m_fSideBobbingLevel = Mathf.Clamp(m_fSideBobbingLevel, -1.0f, 1.0f);
        }

        // Change vertical bobbing direction if the downwards bob is complete.
        if (m_fVertBobbingLevel >= 1.0f)
        {
            m_fVertBobbingLevel = 1.0f;
            m_nBobbingDirection = -1;
        }

        if (m_fLandingBobbingLevel >= 1.0f)
            m_nLandingBobDirection = -1;

        // Vertical bobbing value used for position and angle offset.
        float fVertBobbingValue = Mathf.Abs(Mathf.Sin(m_fVertBobbingLevel * Mathf.PI * 0.5f) + 1.0f);

        m_v3BobbingEuler.x = fVertBobbingValue * m_fBobbingXAngleMagnitude; // X angle offset. (Vertical)
        m_v3BobbingEuler.y = Mathf.Sin(m_fSideBobbingLevel * Mathf.PI * 0.5f) * m_fBobbingYAngleMagnitude * m_nBobbingSideDirection; // Y angle offset. (Horizontal)

        m_v3BobbingOffset.y = -fVertBobbingValue * m_fBobbingPosMagnitude; // Vertical offset.
        m_v3BobbingOffset.y -= m_fLandingBobbingLevel * 0.35f;
    }

    /*
    Description: Set whether or not head bobbing from ground movement is enabled.
    Param:
        bool bEnable: Whether or not to enable ground head bobbing.
    */
    public void SetBobbingEnabled(bool bEnable)
    {
        m_bBobbing = bEnable;
    }

    /*
    Description: Add a step the the head bobbing effect.
    */
    public void Step()
    {
        if (m_nBobbingDirection < 0)
            m_nBobbingDirection = 1;

        // Alternate side bobbing direction and preserve bobbing level by removing sign.
        m_nBobbingSideDirection = -m_nBobbingSideDirection;
        m_fSideBobbingLevel = Mathf.Abs(m_fSideBobbingLevel);
    }

    /*
    Description: Add a landing the the head bobbing effect.
    Param:
        PlayerController controller: For callback compatibility.
    */
    public void Land(PlayerController controller)
    {
        m_nLandingBobDirection = 1;
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
    Description: Apply a chromatic abberation shake effect to the screen for a given amount of time.
    Param:
        float fTime: The amount of time to shake.
        float fMinMag: The minimum magnitude of the shake.
        float fMaxMag: The maximum magnitude of the shake.
    */
    public void ApplyChromAbbShake(float fTime, float fMinMag = 0.0f, float fMaxMag = 1.0f)
    {
        if (m_fChromAbbShakeDuration <= 0.0f || (m_fChromAbbMinMagnitude < fMinMag || m_fChromAbbMaxMagnitude < fMaxMag))
        {
            m_fChromAbbShakeDuration = fTime;
            m_fChromAbbMinMagnitude = fMinMag;
            m_fChromAbbMaxMagnitude = fMaxMag;
        }
    }

    /*
    Description: Set the rate in which FOV will change over time.
    Param:
        float Rate: The new rate in which FOV will change over time.
    */
    public void SetFOVChangeRate(float fRate)
    {
        m_fFOVChangeRate = fRate;
    }

    /*
    Description: Set the FOV offset of the camera. The current FOV will move to the start FOV + offset.
    Param:
        float fOffset: The FOV offset to use. This will be reset after application.
    */
    public void SetFOVOffset(float fOffset)
    {
        m_fFOVOffset = Mathf.Max(m_fFOVOffset, fOffset);
    }

    /*
    Description: Add to the FOV offset of the camera. The current FOV will move to the start FOV + offset.
    Param:
        float fOffset: The FOV offset to add. This will be reset after application.
    */
    public void AddFOVOffset(float fOffset)
    {
        m_fFOVOffset += fOffset;
    }

    /*
    Description: Apply a shake to the chromatic abberation post effect value.
    */
    public void ShakeChromAbb()
    {
        // This uses unscaled deltatime since it needs to occur during the death sequence.

        if(m_fChromAbbShakeDuration <= 0.0f)
        {
            m_chromAbb.value = Mathf.MoveTowards(m_chromAbb.value, 0.0f, 5.0f * Time.unscaledDeltaTime);
            return;
        }

        if(m_fCurrentCAShakeDelay <= 0.0f)
        {
            m_fChromAbbTarget = Random.Range(m_fChromAbbMinMagnitude, m_fChromAbbMaxMagnitude);
            m_fCurrentCAShakeDelay = m_fChromAbbShakeDelay;
        }

        m_fCurrentCAShakeDelay -= Time.unscaledDeltaTime;
        m_fChromAbbShakeDuration -= Time.unscaledDeltaTime;

        // Move towards target value.
        m_chromAbb.value = Mathf.MoveTowards(m_chromAbb.value, m_fChromAbbTarget, 10.0f * Time.unscaledDeltaTime);
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
    Description: Offset from head bobbing effect.
    Return Type: Vector3
    */
    public Vector3 HeadBobbingOffset()
    {
        return m_v3BobbingOffset;
    }

    /*
    Description: Angle offset from head bobbing effect.
    Return Type: Vector3
    */
    public Vector3 HeadBobbingEuler()
    {
        return m_v3BobbingEuler;
    }

    /*
    Description: Get the screen fade script for the HUD.
    Return Type: ScreenFade
    */
    public ScreenFade GetScreenFade()
    {
        return m_screenFade;
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
        // Find spline index using progress value.
        float fIndexProgress = (1.0f - fProgress) * m_camSpline.Count;
        int nSplineIndex = Mathf.Min(Mathf.FloorToInt(fIndexProgress), m_camSpline.Count - 1);

        // Find interpolation value between current and next spline points.
        float fInterp = 1.0f - (fIndexProgress - nSplineIndex);

        // Find current and next spline points.
        CameraSplineState currentSplineState = m_camSpline[nSplineIndex];
        CameraSplineState nextSplineState = m_camSpline[Mathf.Max(nSplineIndex - 1, 0)];

        // Interpolate and read spline states.
        CameraSplineState state;
        state.m_v4Position = Vector4.Lerp(currentSplineState.m_v4Position, nextSplineState.m_v4Position, fInterp);
        state.m_rotation = Quaternion.Slerp(currentSplineState.m_rotation, nextSplineState.m_rotation, fInterp);

        return state;
    }

    /*
    Description: Apply a shake vector to the camera for this frame.
    Param:
        float fMagnitude: The magnitude of the shake vector to add.
        bool bFullMag: Whether or not to force full shake vector magnitude.
    */
    private void Shake(float fMagnitude, bool bFullmag)
    {
        if (m_fShakeDuration > 0.0f && m_fCurrentShakeDelay <= 0.0f)
        {
            Vector3 camEuler = Vector3.zero;

            if (!bFullmag) // Shake offset is at full magnitude every new shake.
            {
                camEuler.x = Random.Range(-fMagnitude, fMagnitude);
                camEuler.y = Random.Range(-fMagnitude, fMagnitude);
            }
            else // Get random angle offset for this shake iteration.
            {
                camEuler.x = Random.Range(-1, 2) * fMagnitude;
                camEuler.y = Random.Range(-1, 2) * fMagnitude;
            }

            // Convert shake rotation to quaternion.
            m_shakeRot = Quaternion.Euler(camEuler);

            // Reset timers.
            m_fShakeReturnTime = m_fShakeReturnDuration;
            m_fCurrentShakeDelay = m_fShakeDelay;
        }

        // Interpolate rotation offset between the shake offset and zero offset.
        m_currentRotOffset = Quaternion.Slerp(m_shakeRot, Quaternion.Euler(Vector3.zero), 1.0f - (m_fShakeReturnTime / m_fShakeReturnDuration));

        // Count down timers.
        m_fShakeReturnTime -= Time.deltaTime;
        m_fShakeDuration -= Time.deltaTime;
        m_fCurrentShakeDelay -= Time.deltaTime;
    }
}
