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

    [Tooltip("Rate in which health points will regenerate over time.")]
    [SerializeField]
    private float m_fHealthRegenRate = 1.0f;

    [Tooltip("Amount of time health regen is interrupted after taking damage.")]
    [SerializeField]
    private float m_fRegenInterruptionTime = 3.0f;

    [SerializeField]
    private float m_fMaxMana = 100.0f;

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

    // -------------------------------------------------------------------------------------------------
    [Header("Death & Respawn")]

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

    [Tooltip("Amount if time time will take to halt before the death spline is ridden.")]
    [SerializeField]
    private float m_fTimeSlowDuration = 2.0f;

    [Tooltip("Rate in which the player will re-materialize when spawning.")]
    [SerializeField]
    private float m_fRespawnMaterializeRate = 1.0f;

    // -------------------------------------------------------------------------------------------------
    [Space(10)]
    [Header("GUI")]
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
    private Text m_speedText = null;

    [Tooltip("Size multipler of the reticle outer sprite when the player can grapple.")]
    [SerializeField]
    private float m_fReticleSizeMultiplier = 1.2f;

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
    private AudioSelection m_hurtSFX = new AudioSelection();

    [SerializeField]
    private AudioSelection m_jumpGruntSFX = new AudioSelection();

    [SerializeField]
    private AudioSelection m_landSFX = new AudioSelection();

    [SerializeField]
    private AudioSource m_sfxSource;

    // -------------------------------------------------------------------------------------------------

    // Private:

    private GrappleHook m_hookScript; // Grapple hook controller script reference.
    private PlayerController m_controller; // Player controller script reference.
    private PlayerBeam m_beamScript; // Player beam controller script reference.
    private CameraEffects m_camEffects; // Camera FX script.
    private BossBehaviour m_bossScript; // Boss AI script reference.
    private Transform m_camPivot; // Pivot in which the player camera is parented to.
    private ScreenFade m_fadeScript; // Script controller screen fade effects.
    private AudioLoop m_windAudioLoop; // Wind audio loop object.
    private Material[] m_materials; // Material references on the player.
    private static float m_fPlayerVolume = 1.0f; // Player audio volume.
    private static float m_fWindVolume = 1.0f; // Wind audio volume.
    private float m_fCurrentWindVolume; // Current wind volume based off of player velocity.
    private static bool m_bCheckpointReached = false; // Whether or not the respawn (on death) checkpoint has been reached.
    private bool m_bNearPickup = false; // Whether or not the player is near a crystal pickup.

    // Resources
    private float m_fHealth; // Everyone knows what this does.
    private float m_fMana; // Resource used up while grappling.
    private float m_fCurrentHealthIntTime; // Current health interruption timer value.
    private float m_fCurrentManaRegenDelay; // Current timer value of the mana regen.
    private bool m_bIsAlive; // Whether or not the player lives.

    // Respawn
    private float m_fRespawnDissolve; // Dissolve level after respawning.

    // Camera backtracking / Death
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
        m_bossScript = GameObject.FindGameObjectWithTag("Boss").GetComponentInChildren<BossBehaviour>();
        m_fadeScript = FindObjectOfType<ScreenFade>();
        m_materials = m_camEffects.GetComponentInChildren<SkinnedMeshRenderer>().materials;

        m_controller.AddJumpCallback(OnJump);
        m_controller.AddLandCallback(OnLand);

        if (!m_sfxSource)
            m_sfxSource = GetComponent<AudioSource>();

        m_windAudioLoop = new AudioLoop(m_windLoopSFX, gameObject, ESpacialMode.AUDIO_SPACE_NONE);

        m_fHealth = m_fMaxHealth;
        m_fMana = m_fMaxMana;
        m_fCurrentManaRegenDelay = 1.0f;
        m_bIsAlive = true;

        m_fSplineInterval = m_fDeathRecordTime / m_camEffects.MaxSplineCount();
        m_fCurrentSplineTime = 0.0f;

        // Initial dissolve level when respawning.
        m_fRespawnDissolve = 1.0f;

        // Set dissolve shader values back to fully dissolved.
        for (int i = 0; i < m_materials.Length; ++i)
            m_materials[i].SetFloat("_Dissolve", m_fRespawnDissolve);

        // Spawn at checkpoint if it has been reached.
        if (m_bCheckpointReached && m_checkpoint)
        {
            transform.position = m_checkpoint.position;
            m_controller.SetLookRotation(m_checkpoint.rotation);
        }
    }

    private void Start()
    {
        // Disable scripts until dissolve-in is finished.
        m_beamScript.enabled = false;
        m_controller.SetFocus(false);
        m_controller.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
        m_hookScript.enabled = false;
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

        // Wind effects.
        if (!m_controller.IsGrounded())
        {
            // Adjust wind volume based off of velocity.
            m_fCurrentWindVolume = Mathf.Max((m_controller.GetVelocity().magnitude - m_fWindMinVolSpeed) / m_fWindMaxVolSpeed);

            if (!m_windAudioLoop.IsPlaying())
                m_windAudioLoop.Play(m_fCurrentWindVolume * m_fWindVolume);

            // Apply camera wind effects...
            m_camEffects.ApplyShake(0.1f, m_fCurrentWindVolume * 0.15f);
            m_camEffects.ApplyChromAbbShake(0.1f, m_fCurrentWindVolume * 0.1f, m_fCurrentWindVolume * 0.2f);
        }
        else if(m_windAudioLoop.IsPlaying())
        {
            // Decay volume when landing.
            m_fCurrentWindVolume = Mathf.MoveTowards(m_fCurrentWindVolume, 0.0f, m_fWindDecayRate * Time.deltaTime);

            // Stop audio when volume reaches silence.
            if (windSource.volume <= 0.01f)
                m_windAudioLoop.Stop();
        }

        windSource.volume = m_fCurrentWindVolume * m_fWindVolume;
    }

    /*
    Description: Run once when jumping.
    Param:
        PlayerController controller: For callback compatibility.
    */
    void OnJump(PlayerController controller)
    {
        // Play SFX.
        m_jumpGruntSFX.PlayRandom(m_fPlayerVolume);
    }

    /*
    Description: Run once when landing.
    Param:
        PlayerController controller: For callback compatibility.
    */
    void OnLand(PlayerController controller)
    {
        m_landSFX.PlayRandom(m_fPlayerVolume);
    }

    private void OnDestroy()
    {
        // Reset shader values.
        for (int i = 0; i < m_materials.Length; ++i)
            m_materials[i].SetFloat("_Dissolve", 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseMenu.IsPaused())
            return;

        if(m_bIsAlive)
        {
            AliveUpdate();
        }
        else
        {
            DeathUpdate();
        }
    }

    /*
    Description: Update function when the player is alive.
    */
    private void AliveUpdate()
    {
        // Apply velocity-based FX.
        ApplyVelocityFX();

        // Reduce hurt cooldown.
        m_hurtSFX.CountCooldown();

        // Reverse dissolve from death.
        if(m_fRespawnDissolve > 0.0f)
        {
            // Count down dissolve level and update shader.
            m_fRespawnDissolve = Mathf.Max(m_fRespawnDissolve - (Time.deltaTime * m_fRespawnMaterializeRate), 0.0f);

            for (int i = 0; i < m_materials.Length; ++i)
                m_materials[i].SetFloat("_Dissolve", m_fRespawnDissolve);

            // Enable scripts once dissolve in is finished.
            if(m_fRespawnDissolve <= 0.0f)
            {
                m_beamScript.enabled = true;
                m_controller.SetFocus(true);
                m_controller.enabled = true;
                m_hookScript.enabled = true;
            }
            
            return;
        }

        // Health regeneration.
        if(m_fCurrentHealthIntTime <= 0.0f)
        {
            // Regenerate health.
            m_fHealth += m_fHealthRegenRate * Time.deltaTime;

            if (m_fHealth > m_fMaxHealth)
                m_fHealth = m_fMaxHealth;

            // Adjust fill amount.
            m_healthFill.fillAmount = (m_fHealth / m_fMaxHealth) * 0.5f;
        }
        else
            m_fCurrentHealthIntTime -= Time.deltaTime;

        // Grapple mana decay.
        if (m_hookScript.GrappleActive())
        {
            // Reset mana regen delay.
            m_fCurrentManaRegenDelay = m_fManaRegenDelay;

            // Lose mana whilst the hook is active.
            m_fMana -= m_fManaLossRate * Time.deltaTime;

            // Update mana GUI fill.
            m_manaFillMat.SetFloat("_Resource", m_fMana / m_fMaxMana);
        }
        else
        {
            // Count down regen delay.
            m_fCurrentManaRegenDelay -= Time.deltaTime;

            if (m_fCurrentManaRegenDelay <= 0.0f)
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

                // Update mana GUI fill.
                m_manaFillMat.SetFloat("_Resource", m_fMana / m_fMaxMana);
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

        // Highlight reticle when the player can grapple an object.
        if (m_hookScript.InGrappleRange())
        {
            m_reticleMats[0].SetInt("_InRange", 1);
            m_reticleMats[1].SetInt("_InRange", 1);

            m_reticleOuterTransform.sizeDelta = new Vector2(50.0f, 50.0f) * m_fReticleSizeMultiplier;
        } 
        else
        {
            m_reticleMats[0].SetInt("_InRange", 0);
            m_reticleMats[1].SetInt("_InRange", 0);

            m_reticleOuterTransform.sizeDelta = new Vector2(50.0f, 50.0f);
        }

        if (m_speedText)
            m_speedText.text = m_controller.GetVelocity().magnitude.ToString("n2") + "m/s";
    }

    /*
    Description: Reset time scale and reload the active scene.
    */
    private void RestartScene()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /*
    Description: Update function during the player's death state.
    */
    private void DeathUpdate()
    {
        m_camEffects.ApplyChromAbbShake(0.1f, m_fSplineProgress * 1.5f, m_fSplineProgress * 2.0f);

        if (Time.timeScale > 0.0f && m_camPivot.parent != null)
        {
            m_fCurrentSplineTime -= Time.deltaTime;

            if(m_fCurrentSplineTime <= 0.0f)
            {
                // Record current camera state.
                m_camEffects.RecordCameraState();
            }

            Time.timeScale = Mathf.Max(Time.timeScale - ((1.0f / m_fTimeSlowDuration) * Time.unscaledDeltaTime), 0.0f);

            if(Time.timeScale == 0.0f)
            {
                // Begin camera spline.
                m_camEffects.StartCamSpline();

                // Detach camera from player.
                m_camPivot.parent = null;

                // Disable control scripts.
                m_controller.enabled = false;
                m_hookScript.enabled = false;
                m_beamScript.enabled = false;
            }

            return;
        }

        CameraSplineState splineState = m_camEffects.EvaluateCamSpline(Mathf.Clamp(m_fSplineProgress, 0.0f, 1.0f));

        m_fSplineProgress += m_rewindCurve.Evaluate(m_fSplineProgress) * Time.unscaledDeltaTime;

        // Set screen fade level as rewind progresses.
        m_fadeScript.SetFade(m_fSplineProgress);

        // Dissolve player as rewind progresses.
        for (int i = 0; i < m_materials.Length; ++i)
            m_materials[i].SetFloat("_Dissolve", m_fSplineProgress);

        m_camEffects.transform.localRotation = Quaternion.identity;
        m_camPivot.position = splineState.m_v4Position;
        m_camPivot.rotation = splineState.m_rotation;

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
    Description: Return whether or not the player is within the trigger volume of a nearby resource pickup.
    Return Type: bool
    */
    public bool NearPickup()
    {
        return m_bNearPickup;
    }

    /*
    Description: Whether or not the player has enough mana to cast the grapple. 
    Return Type: bool
    */
    public bool EnoughMana()
    {
#if UNITY_EDITOR
        return true;
#else
        return m_fMana > m_fMinManaCost;
#endif
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

        // Set GUI value.
        m_healthFill.fillAmount = (m_fHealth / m_fMaxHealth) * 0.5f;

        // Play Hurt SFX...
        m_hurtSFX.PlayRandom();

        // Play camera effects...
        m_camEffects.ApplyShake(0.1f, 0.8f, true);
        m_camEffects.ApplyChromAbbShake(0.1f, 0.6f, 0.8f);

        // Interrupt regenration.
        m_fCurrentHealthIntTime = m_fRegenInterruptionTime;

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

        // Begin fade effect.
        m_fadeScript.SetFadeRate(0.0f);
        m_fadeScript.SetCallback(RestartScene);
        m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);

        // Play boss mockery voice line.
        m_bossScript.PlayDeathMockVoiceLine();

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
            m_controller.AddImpulse(other.transform.forward * m_fPortalPunchForce);

            // Shake camera.
            m_camEffects.ApplyShake(0.5f, 2.0f, true);

            // Break hook.
            m_hookScript.ReleaseGrapple();

            // Deal damage.
            DealDamage(m_fPortalPunchDamage);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Pickup")
        {
            Debug.Log("Player near pickup.");

            m_bNearPickup = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Pickup")
        {
            m_bNearPickup = false;
        }
    }

    public static void SetVolume(float fPlayerVolume, float fWindVolume, float fMaster)
    {
        m_fPlayerVolume = fPlayerVolume * fMaster;
        m_fWindVolume = fWindVolume * fMaster;
    }

    public static float GetPlayerVolume()
    {
        return m_fPlayerVolume;
    }
}
