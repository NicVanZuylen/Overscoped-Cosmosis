using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/*
 * Description: Contains all data and functions related to the player's in-game state.
 * Author: Nic Van Zuylen
*/

public class PlayerStats : MonoBehaviour
{
    public enum ERegenMode
    {
        REGEN_LINEAR,
        REGEN_LERP
    }

    // Inspector:
    // -------------------------------------------------------------------------------------------------

    [Header("Health & Mana")]
    [Space(10)]

    [SerializeField]
    private float m_fMaxHealth = 100.0f;

    [SerializeField]
    private float m_fMaxMana = 100.0f;

    [Tooltip("Minimum mana to cast the grapple.")]
    [SerializeField]
    private float m_fMinManaCost = 15.0f;

    [Tooltip("Rate of player's mana loss when using the grapple.")]
    [SerializeField]
    private float m_fManaLossRate = 5.0f;

    [Tooltip("Rate of mana regeneration when not using the grapple.")]
    [SerializeField]
    private float m_fManaRegenRate = 20.0f;

    [Tooltip("Rate of mana regeneration when not using the grapple in lerp mode.")]
    [SerializeField]
    private float m_fManaRegenRateLerp = 0.1f;

    [Tooltip("Amount of time after using the grapple mana regen will begin.")]
    [SerializeField]
    private float m_fManaRegenDelay = 1.0f;

    [Tooltip("Amount of damage taken from the portal punch attack.")]
    [SerializeField]
    private float m_fPortalPunchDamage = 50.0f;

    [Tooltip("Force applied to the player when they are hit by the portal punch attack.")]
    [SerializeField]
    private float m_fPortalPunchForce = 100.0f;

    [Header("Death")]
    [Tooltip("Amount of time's worth of movement captured by the camera spline.")]
    [SerializeField]
    private float m_fDeathRecordTime = 20.0f;

    [Tooltip("Speed curve for death rewind.")]
    [SerializeField]
    private AnimationCurve m_rewindCurve = null;

    [Tooltip("Checkpoint to restart at after dying.")]
    [SerializeField]
    private Transform m_checkpoint = null;

    [Tooltip("Regen mode for mana regen.")]
    [SerializeField]
    private ERegenMode m_manaRegenMode = ERegenMode.REGEN_LINEAR;

    // -------------------------------------------------------------------------------------------------
    [Space(10)]
    [Header("GUI References")]
    [Space(10)]

    [SerializeField]
    private Image m_healthFill = null;

    [SerializeField]
    private Material m_manaFillMat = null;

    [SerializeField]
    private Material[] m_reticleMats = null;

    [SerializeField]
    private RectTransform m_reticleOuterTransform = null;

    [SerializeField]
    private Material m_armHealthMat = null;

    [SerializeField]
    private Material m_armManaMat = null;

    [SerializeField]
    private Text m_speedText = null;

    // -------------------------------------------------------------------------------------------------
    [Header("SFX")]

    [Tooltip("Speed in which the wind loop will play at maximum volume.")]
    [SerializeField]
    private float m_fWindMaxVolSpeed = 50.0f;

    [Tooltip("Minimum speed in which the player must be flying to hear the wind SFX")]
    [SerializeField]
    private float m_fWindMinVolSpeed = 8.0f;

    [Tooltip("Speed in which wind volume will drop when grounded.")]
    [SerializeField]
    private float m_fWindDecayRate = 2.0f;

    [SerializeField]
    private AudioClip m_windLoopSFX = null;

    [SerializeField]
    private AudioClip[] m_hurtSFX = null;

    [SerializeField]
    private AudioClip[] m_jumpGruntSFX = null;

    [SerializeField]
    private AudioSource m_sfxSource;

    // -------------------------------------------------------------------------------------------------

    // Private:

    private GrappleHook m_hookScript;
    private PlayerController m_controller;
    private PlayerBeam m_beamScript;
    private CameraEffects m_camEffects;
    private Transform m_camPivot;
    private ScreenFade m_fadeScript;
    private AudioLoop m_windAudioLoop;

    // Resources
    private float m_fHealth;
    private float m_fMana;
    private float m_fCurrentRegenDelay;
    private static bool m_bCheckpointReached = false;
    private bool m_bIsAlive;

    // Camera backtracking
    private float m_fSplineInterval;
    private float m_fCurrentSplineTime;
    private float m_fSplineProgress;

    void Awake()
    {
        m_hookScript = GetComponent<GrappleHook>();
        m_controller = GetComponent<PlayerController>();
        m_beamScript = GetComponent<PlayerBeam>();
        m_camEffects = GetComponentInChildren<CameraEffects>(false);
        m_camPivot = transform.Find("CameraPivot");
        m_fadeScript = FindObjectOfType<ScreenFade>();

        m_controller.AddJumpCallback(OnJump);

        if (!m_sfxSource)
            m_sfxSource = GetComponent<AudioSource>();

        m_windAudioLoop = new AudioLoop(m_windLoopSFX, gameObject, ESpacialMode.AUDIO_SPACE_NONE);

        m_fHealth = m_fMaxHealth;
        m_fMana = m_fMaxMana;
        m_fCurrentRegenDelay = 1.0f;
        m_bIsAlive = true;

        m_fSplineInterval = m_fDeathRecordTime / m_camEffects.MaxSplineCount();
        m_fCurrentSplineTime = 0.0f;

        // Spawn at checkpoint if it has been reached.
        if(m_bCheckpointReached && m_checkpoint)
        {
            transform.position = m_checkpoint.position;
            m_controller.SetLookRotation(m_checkpoint.rotation);
        }
    }

    /*
    Description: Apply velocity-based visual and audio effects.
    */
    void ApplyVelocityFX()
    {
        // Get velocity and max ground speed.
        Vector3 v3Velocity = m_controller.GetVelocity();
        float fMaxGroundSpeed = m_controller.MaxGroundSpeed();

        // FOV increment value.
        float fFOVIncrease = 0.0f;

        // Increment FOV if sprinting.
        if (m_controller.IsSprinting())
            fFOVIncrease += 10.0f;

        if (m_controller.IsGrounded())
        {
            // Set grounded FOV change rate.
            m_camEffects.AddFOVOffset(fFOVIncrease);
            m_camEffects.SetFOVChangeRate(75.0f);
        }
        else if (!m_controller.IsJumping())
        {
            // Airborne FOV calculations...
            float fFOVOffset = Mathf.Clamp(v3Velocity.magnitude - fMaxGroundSpeed + fFOVIncrease, 0.0f, 15.0f);

            m_camEffects.AddFOVOffset(fFOVOffset);
            m_camEffects.SetFOVChangeRate(20.0f);
        }
        else
        {
            // Airborne due to jumping calculations...
            Vector3 v3VelNoY = v3Velocity;
            v3VelNoY.y = 0.0f;
            float fFOVOffset = Mathf.Clamp(v3VelNoY.magnitude - fMaxGroundSpeed + fFOVIncrease, 0.0f, 15.0f);

            m_camEffects.AddFOVOffset(fFOVOffset);
            m_camEffects.SetFOVChangeRate(20.0f);
        }

        AudioSource windSource = m_windAudioLoop.GetSource();

        // Wind effect.
        if (!m_controller.IsGrounded())
        {
            // Adjust wind volume based off of velocity.
            windSource.volume = Mathf.Max((m_controller.GetVelocity().magnitude - m_fWindMinVolSpeed) / m_fWindMaxVolSpeed, 0.0f);

            if (!m_windAudioLoop.IsPlaying())
                m_windAudioLoop.Play();
        }
        else if(m_windAudioLoop.IsPlaying())
        {
            // Decay volume when landing.
            windSource.volume = Mathf.MoveTowards(windSource.volume, 0.0f, m_fWindDecayRate * Time.deltaTime);

            // Stop audio when volume reaches silence.
            if (windSource.volume <= 0.01f)
                m_windAudioLoop.Stop();
        }
    }

    /*
    Description: Run once when jumping.
    Param:
        PlayerController controller: For callback compatibility.
    */
    void OnJump(PlayerController controller)
    {
        // Get random SFX index.
        int nRandomSFXIndex = Random.Range(0, m_jumpGruntSFX.Length);

        // Perform null check and play audio.
        if (m_jumpGruntSFX.Length > 0 && m_jumpGruntSFX[nRandomSFXIndex])
            m_sfxSource.PlayOneShot(m_jumpGruntSFX[nRandomSFXIndex]);
    }

    // Update is called once per frame
    void Update()
    {
        if(m_bIsAlive)
        {
            AliveUpdate();
        }
        else
        {
            DeathUpdate();
        }
    }

    private void AliveUpdate()
    {
        // Apply velocity-based FX.
        ApplyVelocityFX();

        if (m_hookScript.IsActive())
        {
            // Reset mana regen delay.
            m_fCurrentRegenDelay = m_fManaRegenDelay;

            // Lose mana whilst the hook is active.
            m_fMana -= m_fManaLossRate * Time.deltaTime;
        }
        else
        {
            // Count down regen delay.
            m_fCurrentRegenDelay -= Time.deltaTime;

            if (m_fCurrentRegenDelay <= 0.0f)
            {
                // Regenerate mana.
                switch (m_manaRegenMode)
                {
                    case ERegenMode.REGEN_LINEAR:
                        m_fMana += m_fManaRegenRate * Time.deltaTime;
                        break;

                    case ERegenMode.REGEN_LERP:
                        m_fMana = Mathf.Lerp(m_fMana, m_fMaxMana, m_fManaRegenRateLerp);
                        break;
                }
            }
        }

        m_fCurrentSplineTime -= Time.deltaTime;

        if (m_fCurrentSplineTime <= 0.0f)
        {
            m_fCurrentSplineTime = m_fSplineInterval;

            // Record current camera state.
            m_camEffects.RecordCameraState();
        }

#if UNITY_EDITOR
        // Kill button
        if(Input.GetKeyDown(KeyCode.K))
        {
            KillPlayer();
        }
#endif

        // Clamp health and mana.
        m_fHealth = Mathf.Clamp(m_fHealth, 0.0f, m_fMaxHealth);
        m_fMana = Mathf.Clamp(m_fMana, 0.0f, m_fMaxMana);

        m_armHealthMat.SetFloat("_Mana", 1.0f - (m_fHealth / m_fMaxHealth));
        m_armManaMat.SetFloat("_Mana", 1.0f - (m_fMana / m_fMaxMana));

        if (m_hookScript.InGrappleRange())
        {
            m_reticleMats[0].SetInt("_InRange", 1);
            m_reticleMats[1].SetInt("_InRange", 1);

            m_reticleOuterTransform.sizeDelta = new Vector2(50.0f, 50.0f) * 1.2f;
        } 
        else
        {
            m_reticleMats[0].SetInt("_InRange", 0);
            m_reticleMats[1].SetInt("_InRange", 0);

            m_reticleOuterTransform.sizeDelta = new Vector2(50.0f, 50.0f);
        }

        if (m_speedText)
            m_speedText.text = m_controller.GetVelocity().magnitude.ToString("n2") + "m/s";

        m_healthFill.fillAmount = (m_fHealth / m_fMaxHealth) * 0.5f;
        m_manaFillMat.SetFloat("_Resource", m_fMana / m_fMaxMana);
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void DeathUpdate()
    {
        CameraSplineState splineState = m_camEffects.EvaluateCamSpline(Mathf.Clamp(m_fSplineProgress, 0.0f, 1.0f));

        m_fSplineProgress += m_rewindCurve.Evaluate(m_fSplineProgress) * Time.deltaTime;

        m_fadeScript.SetFade(m_fSplineProgress);

        m_camEffects.transform.localRotation = Quaternion.identity;

        m_camPivot.transform.position = splineState.m_v4Position;
        m_camPivot.transform.rotation = splineState.m_rotation;

        // Resurrect player.
        if (Input.GetKeyDown(KeyCode.K))
        {
            Resurrect();
        }

        // Reset scene when spline is complete.
        if(m_fSplineProgress >= 1.0f)
        {
            RestartScene();
        }
    }

    /*
    Description: Whether or not the player has enough mana to cast the grapple. 
    Return Type: bool
    */
    public bool EnoughMana()
    {
        return m_fMana > m_fMinManaCost;
    }

    /*
    Description: The current mana value for the player.
    Return Type: float
    */
    public float GetMana()
    {
        return m_fMana;
    }

    /*
    Description: Restore the player's health to full, and reverse any death effects.
    */
    public void Resurrect()
    {
        m_fHealth = m_fMaxHealth;

        // Reverse death effects...

        // Reset spline.
        m_camEffects.ClearCamSpline();
        m_fSplineProgress = 0.0f;

        m_camPivot.parent = transform;
        m_camPivot.localPosition = new Vector3(0.0f, 0.7f, 0.0f);

        // Enable scripts.
        m_controller.enabled = true;
        m_hookScript.enabled = true;
        m_beamScript.enabled = true;

        m_bIsAlive = true;
    }

    /*
    Description: Restore the player's health to full. 
    */
    public void RestoreHealth()
    {
        m_fHealth = m_fMaxHealth;
    }

    /*
    Description: Deal damage to the player's health, and play any related effects.
    Param:
        float fDamage: The amount of damage to deal to the player's health.
    */
    public void DealDamage(float fDamage)
    {
        m_fHealth -= fDamage;

        // Hurt SFX
        int nRandomSFXIndex = Random.Range(0, m_hurtSFX.Length);

        if (m_hurtSFX.Length > 0 && m_hurtSFX[nRandomSFXIndex])
            m_sfxSource.PlayOneShot(m_hurtSFX[nRandomSFXIndex]);

        if(m_fHealth <= 0.0f)
        {
            m_fHealth = 0.0f;

            // Kill player...

            KillPlayer();
        }
    }

    private void KillPlayer()
    {
        m_fHealth = 0.0f;

        // Set health bar value to zero.
        m_armHealthMat.SetFloat("_Mana", 0.0f);

        // Detach camera from player.
        m_camPivot.parent = null;

        // Begin spline.
        m_camEffects.StartCamSpline();

        // Begin fade effect.
        m_fadeScript.SetFadeRate(0.0f);
        m_fadeScript.SetCallback(RestartScene);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);

        // Disable control scripts.
        m_controller.enabled = false;
        m_hookScript.enabled = false;
        m_beamScript.enabled = false;

        // Assume checkpoint is reached.
        m_bCheckpointReached = true;

        // Flag as dead.
        m_bIsAlive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "PushPlayer")
        {
            Debug.Log("Portal Punch Hit!");

            // Add force in the punch direction.
            m_controller.AddImpulse(-other.transform.up * m_fPortalPunchForce);

            // Shake camera.
            m_camEffects.ApplyShake(0.5f, 2.0f, true);

            // Break hook.
            m_hookScript.ReleaseGrapple();

            // Deal damage.
            DealDamage(m_fPortalPunchDamage);
        }
    }
}
