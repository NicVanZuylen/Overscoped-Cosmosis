using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public enum ERegenMode
    {
        REGEN_LINEAR,
        REGEN_LERP
    }

    // On inspector: 

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
    private float m_fDeathCamTime = 5.0f;

    [Tooltip("Regen mode for mana regen.")]
    [SerializeField]
    private ERegenMode m_manaRegenMode = ERegenMode.REGEN_LINEAR;

    [Space(10)]
    [Header("Health & Mana")]
    [Space(10)]

    [SerializeField]
    private Image m_healthFill = null;

    [SerializeField]
    private Material m_manaFillMat = null;

    [SerializeField]
    private Material m_reticleMat = null;

    [SerializeField]
    private Material m_armHealthMat = null;

    [SerializeField]
    private Material m_armManaMat = null;

    // Private:

    private GrappleHook m_hookScript;
    private PlayerController m_controller;
    private PlayerBeam m_beamScript;
    private CameraEffects m_camEffects;
    private Transform m_camPivot;

    // Resources
    private float m_fHealth;
    private float m_fMana;
    private float m_fCurrentRegenDelay;
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

        m_fHealth = m_fMaxHealth;
        m_fMana = m_fMaxMana;
        m_fCurrentRegenDelay = 1.0f;
        m_bIsAlive = true;

        m_fSplineInterval = m_fDeathCamTime / m_camEffects.MaxSplineCount();
        m_fCurrentSplineTime = 0.0f;
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

        // Kill button
        if(Input.GetKeyDown(KeyCode.K))
        {
            KillPlayer();
        }

        // Clamp health and mana.
        m_fHealth = Mathf.Clamp(m_fHealth, 0.0f, m_fMaxHealth);
        m_fMana = Mathf.Clamp(m_fMana, 0.0f, m_fMaxMana);

        m_armHealthMat.SetFloat("_Mana", 1.0f - (m_fHealth / m_fMaxHealth));
        m_armManaMat.SetFloat("_Mana", 1.0f - (m_fMana / m_fMaxMana));

        if (m_hookScript.InGrappleRange())
            m_reticleMat.SetInt("_InRange", 1);
        else
            m_reticleMat.SetInt("_InRange", 0);

        m_healthFill.fillAmount = (m_fHealth / m_fMaxHealth) * 0.5f;
        m_manaFillMat.SetFloat("_Resource", m_fMana / m_fMaxMana);
    }

    private void DeathUpdate()
    {
        //CameraSplineState splineState = m_camEffects.EvaluateCamSpline(1.0f * Time.deltaTime);
        CameraSplineState splineState = m_camEffects.EvaluateCamSpline(Mathf.Clamp(m_fSplineProgress, 0.0f, 1.0f));

        m_fSplineProgress += 0.1f * Time.deltaTime;

        m_camEffects.transform.localRotation = Quaternion.identity;

        m_camPivot.transform.position = splineState.m_v4Position;
        m_camPivot.transform.rotation = splineState.m_rotation;
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

        // Disable control scripts.
        m_controller.enabled = false;
        m_hookScript.enabled = false;
        m_beamScript.enabled = false;

        // Flag as dead.
        m_bIsAlive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "MeteorSpawn")
        {
            BossBehaviour.SetMeteorSpawn(other.transform.GetChild(0).gameObject.GetComponent<BoxCollider>());
        }

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

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "MeteorSpawn")
        {
            // Remove meteor spawn.
            BossBehaviour.SetMeteorSpawn(null);
        }
    }
}
